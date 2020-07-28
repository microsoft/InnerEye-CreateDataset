///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.RT
{
    using Dicom;

    using Extensions;
    using System;
    using System.Globalization;

    /// <summary>
    /// Encodes  parts of the RTSeries DICOM module we use.
    /// see http://dicom.nema.org/medical/Dicom/current/output/chtml/part03/sect_C.8.8.html
    /// </summary>
    public class DicomRTSeries
    {
        public const string RtModality = "RTSTRUCT";

        public string Modality { get; }

        public string SeriesInstanceUID
        {
            get;
            set;
        }

        /// <summary>
        /// Get/Set the Series Description of this RT Series
        /// </summary>
        public string SeriesDescription { get; set;  }

        public DicomRTSeries(string modality, string seriesInstanceUID, string description)
        {
            Modality = modality;
            SeriesInstanceUID = seriesInstanceUID;
            SeriesDescription = description;
        }

        public static DicomRTSeries Read(DicomDataset ds)
        {
            var modality = ds.GetStringOrEmpty(DicomTag.Modality);
            var seriesInstanceUID = ds.GetStringOrEmpty(DicomTag.SeriesInstanceUID);
            var description = ds.GetStringOrEmpty(DicomTag.SeriesDescription);
            return new DicomRTSeries(modality, seriesInstanceUID, description);
        }

        public static void Write(DicomDataset ds, DicomRTSeries series)
        {
            ds.Add(DicomTag.Modality, series.Modality);
            ds.Add(DicomTag.SeriesInstanceUID, series.SeriesInstanceUID);
            ds.Add(DicomTag.SeriesDescription, series.SeriesDescription);
            // Type 2 attributes - must be present but empty is fine. 
            ds.Add(DicomTag.OperatorsName, string.Empty);
            ds.Add(DicomTag.SeriesNumber, string.Empty);

            // Type 3 tags - optional but useful
            var now = DateTime.UtcNow;
            var date = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var time = now.ToString("HHmmss", CultureInfo.InvariantCulture);
            ds.Add(DicomTag.SeriesDate, date);
            ds.Add(DicomTag.SeriesTime, time);
        }
    }

}