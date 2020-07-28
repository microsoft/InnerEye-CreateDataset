///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO
{
    using System;

    using Dicom;

    using MedLib.IO.Models.DicomRt;
    using MedLib.IO.Extensions;

    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Helpers for Dicom Rt files
    /// </summary>
    public static class DicomRtHelpers
    {
        /// <summary>
        /// Converts a Dicom file to a radiotherapy structure set.
        /// </summary>
        /// <param name="dicomFile">The structure set dicom file.</param>
        /// <param name="dicomToDataTransform">The dicom to data transform.</param>
        /// <returns>The radiotherapy structure set.</returns>
        /// <exception cref="ArgumentNullException">If the Dicom file or Dicom dataset is null.</exception>
        /// <exception cref="ArgumentException">If the file is not a structure set file.</exception>
        public static RadiotherapyStruct DicomFileToRadiotherapyStruct(DicomFile dicomFile, Transform3 dicomToDataTransform)
        {
            if (dicomFile?.Dataset == null)
            {
                throw new ArgumentNullException(nameof(dicomFile));
            }

            if (!dicomFile.Dataset.IsRTStructure())
            {
                throw new ArgumentException($"This file is not a structure set file. File: {dicomFile?.File?.Name}");
            }

            return RtStructReader.LoadContours(dicomFile.Dataset, dicomToDataTransform, warningsAsErrors: false).Item1;
        }
    }
}