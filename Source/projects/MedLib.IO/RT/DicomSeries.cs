///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using Dicom;
using MedLib.IO.Extensions;

namespace MedLib.IO.RT
{
    /// <summary>
    /// Encodes important Type 1 and 2 tags from the DICOM General Series Module
    /// <see cref="http://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.7.3.html"/>
    /// </summary>
    public class DicomSeries
    {
        public DicomSeries(
            string modality, string seriesInstanceUID, string seriesNumber, string seriesDate, string seriesTime, string seriesDescription,
            string bodyPartExamined, string patientPosition)
        {
            Modality = modality;
            SeriesInstanceUid = seriesInstanceUID;
            SeriesNumber = seriesNumber;
            SeriesDate = seriesDate;
            SeriesTime = seriesTime;
            SeriesDescription = seriesDescription;
            BodyPartExamined = bodyPartExamined;
            PatientPosition = patientPosition; 
        }

        /// <summary>
        /// (0008,0060) Type 1 CS VR
        /// </summary>
        public string Modality { get; }

        /// <summary>
        /// (0020,000E) Type 1 Unique Identifier for the series
        /// </summary>
        public string SeriesInstanceUid { get; }

        /// <summary>
        /// (0020,0011) A number that identifies the series (within the study) generall very unreliable.
        /// Do not infer anything from this number.  Type 2, VR Integer string
        /// </summary>
        public string SeriesNumber { get;  }

        /// <summary>
        /// (0008,0021) Date the series started. Type 3. DA VR (YYYYMMDD gregorian)
        /// </summary>
        public string SeriesDate { get; }

        /// <summary>
        /// (0008,0031) Time the series started. Type 3/ TM VR (HHMMSS.FFFFFF)
        /// </summary>
        public string SeriesTime { get; }

        /// <summary>
        /// (0008,103E), Type 3 VR LO (64 chars maximum)
        /// </summary>
        public string SeriesDescription { get; }

        /// <summary>
        /// 0018,0015, Type 3, VR CS
        /// <see cref="http://dicom.nema.org/medical/dicom/current/output/chtml/part16/chapter_L.html#table_L-1"/>
        /// </summary>
        public string BodyPartExamined { get; }

        /// <summary>
        /// 0018,5100, Type 2 for CT and MR IODs, VR CS
        /// <see cref="http://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.7.3.html#sect_C.7.3.1.1.2"/>
        /// </summary>
        public string PatientPosition { get; }

        /// <summary>
        /// Read a DicomSeries from the given dataset, throwing if Type 1 parameters are not present
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DicomSeries Read(DicomDataset ds)
        {
            // Throw if not present
            var modality = ds.GetSingleValue<string>(DicomTag.Modality);
            var uid = ds.GetSingleValue<DicomUID>(DicomTag.SeriesInstanceUID);

            // No throw
            var seriesNumber = ds.GetTrimmedStringOrEmpty(DicomTag.SeriesNumber);
            var seriesDate = ds.GetTrimmedStringOrEmpty(DicomTag.SeriesDate);
            var seriesTime = ds.GetTrimmedStringOrEmpty(DicomTag.SeriesTime);
            var description = ds.GetTrimmedStringOrEmpty(DicomTag.SeriesDescription);
            var bodyPart = ds.GetTrimmedStringOrEmpty(DicomTag.BodyPartExamined);
            var patientPos = ds.GetTrimmedStringOrEmpty(DicomTag.PatientPosition);

            return new DicomSeries(modality, uid.UID, seriesNumber, seriesDate, seriesTime, description, bodyPart, patientPos);

        }

        /// <summary>
        /// Returns an empty DicomSeries instance
        /// </summary>
        /// <returns></returns>
        public static DicomSeries CreateEmpty()
        {
            return new DicomSeries(
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty             
                );
        }


    }
}
