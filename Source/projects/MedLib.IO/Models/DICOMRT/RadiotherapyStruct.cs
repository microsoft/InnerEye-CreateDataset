///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Models.DicomRt
{
    using System.Collections.Generic;

    using Readers;
    using RT;
    using System.Linq;
    using Dicom;

    /// <summary>
    /// Encodes an instance of the DICOM RT-STRUCT IOD. With facilities to serialize and restore from a DICOM dataset.
    /// </summary>
    public class RadiotherapyStruct
    {
        /// <summary>
        /// DICOM patient module
        /// </summary>
        public DicomPatient Patient { get; }

        /// <summary>
        /// DICOM study module
        /// </summary>
        public DicomStudy Study { get; }

        /// <summary>
        /// DICOM RT Series module
        /// </summary>
        public DicomRTSeries RTSeries { get; }

        /// <summary>
        /// DICOM Equipment module
        /// </summary>
        public DicomEquipment Equipment { get; }

        /// <summary>
        /// DICOM Structure Set Module.
        /// </summary>
        public DicomRTStructureSet StructureSet { get; }

        /// <summary>
        /// Collated {Contour, Structure and Observation} module information for each structure in this structure set.
        /// </summary>
        public IList<RadiotherapyContour> Contours { get; }


        /// <summary>
        /// Called when we open an existing structure set. Note we preserve the SeriesUID and the Series Description. (this is arguable if it
        /// did not originate from us)
        /// </summary>
        /// <param name="structureSet"></param>
        /// <param name="patient"></param>
        /// <param name="equipment"></param>
        /// <param name="study"></param>
        /// <param name="series"></param>
        /// <param name="contours"></param>
        public RadiotherapyStruct(
            DicomRTStructureSet structureSet, DicomPatient patient, DicomEquipment equipment, 
            DicomStudy study, DicomRTSeries series, IList<RadiotherapyContour> contours)
        {
            RTSeries = series;
            Patient = patient;
            Equipment = equipment;
            Study = study;
            StructureSet = structureSet;
            Contours = contours;
        }

        /// <summary>
        /// Called when we are creating a new structure set from an existing 3D scan. 
        /// </summary>
        /// <param name="identifiers">The identifiers from 1 of the images of the 3D scan</param>
        /// <returns>A new structure set with a new SeriesUID</returns>
        public static RadiotherapyStruct CreateDefault(IReadOnlyList<DicomIdentifiers> identifiers)
        {
            var firstIdentifier = identifiers.First();
            var newSeriesUID = DicomUID.Generate().UID;

            return new RadiotherapyStruct(
                DicomRTStructureSet.CreateDefault(identifiers),
                firstIdentifier.Patient,
                DicomEquipment.CreateEmpty(),
                firstIdentifier.Study,
                new DicomRTSeries(DicomRTSeries.RtModality, newSeriesUID, string.Empty),
                new List<RadiotherapyContour>());
        }

        /// <summary>
        /// Read the structure set from a DICOM dataset. 
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static RadiotherapyStruct Read(DicomDataset ds)
        {
            var patient = DicomPatient.Read(ds);
            var study = DicomStudy.Read(ds);
            var series = DicomRTSeries.Read(ds);
            var equipment = DicomEquipment.Read(ds);
            var structureSet = DicomRTStructureSet.Read(ds);

            // DicomRTStructureSetROI.RoiNumber is the primary key. We try and locate the first observation and roiContour data for eaach 
            // RoiNumber encountered. 
            var structures = DicomRTStructureSetROI.Read(ds);
            var contoursData = DicomRTContour.Read(ds);
            var observations = DicomRTObservation.Read(ds);

            var allContours = new List<RadiotherapyContour>(); 

            foreach (var s in structures)
            {
                var roiContours = contoursData.FirstOrDefault(c => c.ReferencedRoiNumber == s.Key);
                var roiObservation = observations.FirstOrDefault(o => o.ReferencedRoiNumber == s.Key);

                // we insist both must exist
                if (roiContours != null && roiObservation != null)
                {
                    allContours.Add(new RadiotherapyContour(roiContours, s.Value, roiObservation));
                }
            }

            return new RadiotherapyStruct(structureSet, patient, equipment, study, series, allContours);
        }

        /// <summary>
        /// Serialize the structure set to a DICOM dataset. 
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="s"></param>
        public static void Write(DicomDataset ds, RadiotherapyStruct s)
        {
            DicomPatient.Write(ds, s.Patient);
            DicomStudy.Write(ds, s.Study);
            DicomRTSeries.Write(ds, s.RTSeries);
            DicomEquipment.Write(ds, s.Equipment);
            DicomRTStructureSet.Write(ds, s.StructureSet);

            // For each ROI, gather the contours, observations and roi details into seperate arrays and serialize
            var rois = s.Contours.Select(c => c.StructureSetRoi);
            var contours = s.Contours.Select(c => c.DicomRtContour);
            var observations = s.Contours.Select(c => c.DicomRtObservation);

            DicomRTStructureSetROI.Write(ds, rois);
            DicomRTContour.Write(ds, contours);
            DicomRTObservation.Write(ds, observations);
        }

    }
}