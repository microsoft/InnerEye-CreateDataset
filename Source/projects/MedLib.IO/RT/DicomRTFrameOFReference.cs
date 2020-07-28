///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using System.Collections.Generic;

    using Dicom;
    
    /// <summary>
    /// The Referenced Frame of Reference Sequence (3006,0010) describes a set of frames of reference in
    /// which some or all of the ROIs are expressed.Since the Referenced Frame of Reference UID 
    /// (3006,0024) is required for each ROI, each frame of reference used to express the coordinates of an ROI 
    /// shall be listed in the Referenced Frame of Reference Sequence(3006,0010) once and only once.
    /// </summary>
    public class DicomRTFrameOFReference
    {
        public string FrameOfRefUID { get; }

        public IReadOnlyList<DicomRTReferencedStudy> ReferencedStudies { get; }

        public DicomRTFrameOFReference(
            string frameOfRefUid,
            IReadOnlyList<DicomRTReferencedStudy> referencedStudies)
        {
            FrameOfRefUID = frameOfRefUid;
            ReferencedStudies = referencedStudies;
        }

        public static DicomRTFrameOFReference Read(DicomDataset ds)
        {
            var frameReferencedUID = ds.GetSingleValueOrDefault(DicomTag.FrameOfReferenceUID, string.Empty);

            var referencedStudies = new List<DicomRTReferencedStudy>();
            if (ds.Contains(DicomTag.RTReferencedStudySequence))
            {
                var seq = ds.GetSequence(DicomTag.RTReferencedStudySequence);
                foreach (var item in seq)
                {
                    referencedStudies.Add(DicomRTReferencedStudy.Read(item));
                }
            }
            return new DicomRTFrameOFReference(
                frameReferencedUID,
                referencedStudies);
        }

        public static DicomDataset Write(DicomRTFrameOFReference dicomRtFrameOfReference)
        {
            var ds = new DicomDataset();
            ds.Add(DicomTag.FrameOfReferenceUID, dicomRtFrameOfReference.FrameOfRefUID);
            var lisOfStudies = new List<DicomDataset>();
            foreach (var refStudy in dicomRtFrameOfReference.ReferencedStudies)
            {
                var newDS = DicomRTReferencedStudy.Write(refStudy);
                lisOfStudies.Add(newDS);
            }
            if (lisOfStudies.Count > 0)
            {
                ds.Add(new DicomSequence(DicomTag.RTReferencedStudySequence, lisOfStudies.ToArray()));
            }
            return ds;
        }
    }
}