///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Readers
{
    using System;
    using System.Collections.Generic;
    using Dicom;
    using MedLib.IO.Models;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Reads medical image volumes represented as DICOM series.
    /// Supports only CT and MR images at present with 2 bytes per pixel.
    /// </summary>
    public static class DicomSeriesReader
    {
        /// <summary>
        /// The collection of supported transfer syntaxes for CT & MR images.
        /// </summary>
        public static readonly DicomTransferSyntax[] SupportedTransferSyntaxes = {
            DicomTransferSyntax.ExplicitVRLittleEndian, // Does not crash app if corrupted - loads corrupted data
            DicomTransferSyntax.ImplicitVRLittleEndian, // Does not crash app if corrupted - loads corrupted data
            DicomTransferSyntax.ExplicitVRBigEndian, // Does not crash app if corrupted - loads corrupted data
            DicomTransferSyntax.JPEGProcess14SV1, // Does not crash app if corrupted - loads corrupted data
            DicomTransferSyntax.JPEGProcess14, // Does not crash app if corrupted. - loads corrupted data
            DicomTransferSyntax.JPEGLSLossless, // Codec exception managed code if corrupted - does not load
            DicomTransferSyntax.RLELossless // Does not crash app or load data if corrupted
        };

        /// <summary>
        /// Attempt to construct a 3-dimensional volume instance from the provided set of DICOM series files. 
        /// </summary>
        /// <param name="dicomDatasets">The collection of DICOM datasets.</param>
        /// <param name="acceptanceTest">An implmentation of IVolumeGeometricAcceptanceTest expressing the geometric tollerances required by your application</param>
        /// <param name="supportLossyCodecs">true if it is appropriate for your application to support lossy pixel encodings</param>
        /// <returns>The created 3-dimensional volume.</returns>
        /// <exception cref="ArgumentNullException">The DICOM datasets or acceptance test is null.</exception>
        /// <exception cref="ArgumentException">A volume could not be formed from the provided DICOM series datasets.</exception>
        public static Volume3D<short> BuildVolume(
            IEnumerable<DicomDataset> dicomDatasets,
            IVolumeGeometricAcceptanceTest acceptanceTest,
            bool supportLossyCodecs)
        {
            dicomDatasets = dicomDatasets ?? throw new ArgumentNullException(nameof(dicomDatasets));
            acceptanceTest = acceptanceTest ?? throw new ArgumentNullException(nameof(acceptanceTest));

            // 1. Construct the volume information: this requires a minimum set of DICOM tags in each dataset.
            var volumeInformation = VolumeInformation.Create(dicomDatasets);

            // 2. Now validate the volume based on the acceptance tests (will throw argument exception on failure).
            DicomSeriesInformationValidator.ValidateVolumeInformation(volumeInformation, acceptanceTest, supportLossyCodecs ? null : SupportedTransferSyntaxes);

            // 3. Now validated, lets extract the pixels as a short array.
            return DicomSeriesImageReader.BuildVolume(volumeInformation);
        }
    }
}