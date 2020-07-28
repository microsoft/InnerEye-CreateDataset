///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Readers
{
    using System.Collections.Generic;
    using Dicom;

    /// <summary>
    /// Readonly collection of CT/MR images associated with a given series. 
    /// </summary>
    public sealed class DicomSeriesContent
    {
        public DicomSeriesContent(DicomUID seriesUID, IReadOnlyList<DicomFileAndPath> content)
        {
            SeriesUID = seriesUID;
            Content = content;
        }

        /// <summary>
        /// The unique DICOM Series UID
        /// </summary>
        public DicomUID SeriesUID { get; private set; }

        /// <summary>
        /// The list of recognised Sop Class instances in this series.
        /// </summary>
        public IReadOnlyList<DicomFileAndPath> Content { get; private set; }
    }
}