///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using System.Collections.Generic;

    using Dicom;

    using Extensions;

    /// <summary>
    /// Enum encoding the 3 expected ROIGenerationAlgorithm terms defined in the standard
    /// plus an empty/missing term that is exported as an empty string. See 
    /// http://dicom.nema.org/medical/Dicom/current/output/chtml/part03/sect_C.8.8.5.html 3006,0036
    /// </summary>
    public enum ERoiGenerationAlgorithm
    {
        UnknownOrEmpty = 0,
        Automatic,
        Semiautomatic,
        Manual
    }


    public class DicomRTStructureSetROI
    {
        public string RoiNumber { get; }

        public string RoiName { get; }

        public string ReferencedFrameOfRefUID { get; }

        public ERoiGenerationAlgorithm ROIGenerationAlgorithm { get; }

        public DicomRTStructureSetROI(
            string roiNumber,
            string roiName,
            string referencedFrameOfRefUid,
            ERoiGenerationAlgorithm roiGenerationAlgorithm)
        {
            RoiNumber = roiNumber;
            RoiName = roiName;
            ReferencedFrameOfRefUID = referencedFrameOfRefUid;
            ROIGenerationAlgorithm = roiGenerationAlgorithm;
        }

        public static IReadOnlyDictionary<string, DicomRTStructureSetROI> Read(DicomDataset ds)
        {
            var rois = new Dictionary<string, DicomRTStructureSetROI>();
            if (ds.Contains(DicomTag.StructureSetROISequence))
            {
                var seq = ds.GetSequence(DicomTag.StructureSetROISequence);
                foreach (var item in seq)
                {
                    var roiNumber = item.GetStringOrEmpty(DicomTag.ROINumber);
                    var referencedFrameUID = item.GetStringOrEmpty(DicomTag.ReferencedFrameOfReferenceUID);
                    var roiName = item.GetStringOrEmpty(DicomTag.ROIName);
                    var roiGenerationAlgorithm = ParseRoiAlgorithm(item.GetStringOrEmpty(DicomTag.ROIGenerationAlgorithm));
                    rois.Add(
                        roiNumber, new DicomRTStructureSetROI(roiNumber, roiName, referencedFrameUID, roiGenerationAlgorithm));
                }
            }
            return rois;
        }

        public static void Write(DicomDataset ds, IEnumerable<DicomRTStructureSetROI> structureSetRois)
        {

            var roisDataSets = new List<DicomDataset>();
            foreach (var roi in structureSetRois)
            {
                var newDS = new DicomDataset();
                newDS.Add(DicomTag.ROINumber, roi.RoiNumber);
                newDS.Add(DicomTag.ROIName, roi.RoiName);
                newDS.Add(DicomTag.ReferencedFrameOfReferenceUID, roi.ReferencedFrameOfRefUID);
                newDS.Add(DicomTag.ROIGenerationAlgorithm, ToString(roi.ROIGenerationAlgorithm));
                roisDataSets.Add(newDS);
            }
            if (roisDataSets.Count > 0)
            {
                ds.Add(new DicomSequence(DicomTag.StructureSetROISequence, roisDataSets.ToArray()));
            }
        }

        /// <summary>
        /// DICOM code strings for the 3 generating algorithm types
        /// </summary>
        private const string _algorithmAutomatic = "AUTOMATIC";
        private const string _algorithmSemiAutomatic = "SEMIAUTOMATIC";
        private const string _algorithmManual = "MANUAL";


        private static ERoiGenerationAlgorithm ParseRoiAlgorithm(string dString)
        {
            ERoiGenerationAlgorithm output = ERoiGenerationAlgorithm.UnknownOrEmpty;
            if (dString == _algorithmAutomatic)
            {
                output = ERoiGenerationAlgorithm.Automatic;
            }
            else
            if (dString == _algorithmSemiAutomatic)
            {
                output = ERoiGenerationAlgorithm.Semiautomatic;
            }
            else
            if (dString == _algorithmManual)
            {
                output = ERoiGenerationAlgorithm.Manual; 
            }
            return output; 
        }

        private static string ToString(ERoiGenerationAlgorithm e)
        {
            switch (e)
            {
                default:
                case ERoiGenerationAlgorithm.UnknownOrEmpty:
                    {
                        return string.Empty; 
                    }
                case ERoiGenerationAlgorithm.Automatic:
                    {
                        return _algorithmAutomatic;
                    }
                case ERoiGenerationAlgorithm.Semiautomatic:
                    {
                        return _algorithmSemiAutomatic;
                    }
                case ERoiGenerationAlgorithm.Manual:
                    {
                        return _algorithmManual;
                    }
            }
        }
    }
}