///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Readers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dicom;
    using MedLib.IO.Extensions;
    using MedLib.IO.Models;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// DICOM series validator for validating slice and volume level information.
    /// </summary>
    public static class DicomSeriesInformationValidator
    {
        /// <summary>
        /// The expected number of bits allocated per voxel.
        /// </summary>
        public const int ExpectedBitsAllocated = 16;

        /// <summary>
        /// The expected samples per voxel.
        /// </summary>
        public const int ExpectedSamplesPerPixel = 1;

        /// <summary>
        /// The expected photometric interpretation of each DICOM slice.
        /// </summary>
        public const string ExpectedPhotometricInterpretation = "MONOCHROME2";

        /// <summary>
        /// Validates the provided volume information in accordance with the provided volume geometric acceptance test and
        /// that every slice in the volume is valid.
        /// This will check:
        ///     1. Validates each slice using the validate slice information method (and will use the supported transfer syntaxes if provided).
        ///     2. Grid conformance of the volume.
        ///     3. Slice spacing conformance for each slice.
        ///     4. Executes the propose method of the acceptance test.
        /// </summary>
        /// <param name="volumeInformation">The volume information.</param>
        /// <param name="volumeGeometricAcceptanceTest">The volume geometric acceptance test.</param>
        /// <param name="supportedTransferSyntaxes">The supported transfer syntaxes or null if we do not want to check against this.</param>
        /// <exception cref="ArgumentNullException">The volume information or acceptance test is null.</exception>
        /// <exception cref="ArgumentException">An acceptance test did not pass.</exception>
        public static void ValidateVolumeInformation(
            VolumeInformation volumeInformation,
            IVolumeGeometricAcceptanceTest volumeGeometricAcceptanceTest,
            IReadOnlyCollection<DicomTransferSyntax> supportedTransferSyntaxes = null)
        {
            volumeInformation = volumeInformation ?? throw new ArgumentNullException(nameof(volumeInformation));
            volumeGeometricAcceptanceTest = volumeGeometricAcceptanceTest ?? throw new ArgumentNullException(nameof(volumeGeometricAcceptanceTest));
            
            // 1. Validate each slice.
            for (var i = 0; i < volumeInformation.Depth; i++)
            {
                // Validate the DICOM tags of each slice.
                ValidateSliceInformation(volumeInformation.GetSliceInformation(i), supportedTransferSyntaxes);

                if (i > 0)
                {
                    // Validate the slice information is consistent across slices using the first slice as a reference.
                    ValidateSliceInformation(volumeInformation.GetSliceInformation(i), volumeInformation.GetSliceInformation(0));
                }
            }

            // 2. + 3. Check the slice and grid conformance of the volume information.
            CheckGridConformance(volumeInformation, volumeGeometricAcceptanceTest);
            CheckSliceSpacingConformance(volumeInformation, volumeGeometricAcceptanceTest);

            // 4. Run acceptance testing.
            var acceptanceErrorMessage = string.Empty;
            if (!volumeGeometricAcceptanceTest.Propose(
                    volumeInformation.SopClass,
                    volumeInformation.Origin,
                    volumeInformation.Direction,
                    new Point3D(volumeInformation.VoxelWidthInMillimeters, volumeInformation.VoxelHeightInMillimeters, volumeInformation.Depth),
                    out acceptanceErrorMessage))
            {
                throw new ArgumentException(acceptanceErrorMessage, nameof(volumeInformation));
            }
        }

        /// <summary>
        /// Validates the slice information. This method will check:
        ///     1. The dataset has a supported transfer syntax.
        ///     2. Does not have a 'ModalityLUTSequence' attribute in the dataset.
        ///     3. Has 16 bits allocated.
        ///     4. Has 'MONOCHROME2' as the photometric interpretation.
        ///     5. Only has 1 sample (channel) per pixel.
        ///     6. Validates the SOP class of the dataset is CT or MR and has the correct matching 'Modality' attribute.
        ///     7. Validates the 'BitsStored' attribute is 1 + the 'HighBit' attribute.
        /// </summary>
        /// <param name="dataset">The dataset to build the slice information from.</param>
        /// <param name="supportedTransferSyntaxes">The list of supported transfer syntaxes or empty if you do not wish to validate against this.</param>
        /// <exception cref="ArgumentNullException">The provided slice information was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not have a required attribute or was not of the correct value.</exception>
        public static void ValidateSliceInformation(
            SliceInformation sliceInformation,
            IReadOnlyCollection<DicomTransferSyntax> supportedTransferSyntaxes = null)
        {
            sliceInformation = sliceInformation ?? throw new ArgumentNullException(nameof(sliceInformation));

            var dataset = sliceInformation.DicomDataset;

            // 1. If supported transfer syntaxes have been provided, validate the dataset has one of these.
            if (supportedTransferSyntaxes != null && !supportedTransferSyntaxes.Contains(dataset.InternalTransferSyntax))
            {
                throw new ArgumentException($"The DICOM dataset has an unsupported storage transfer syntax type {dataset.InternalTransferSyntax}. Expected: {string.Join("/ ", supportedTransferSyntaxes.Select(x => x.ToString()))}");
            }

            // 2. A dataset should not have the 'ModalityLUTSequence' tag.
            if (dataset.Contains(DicomTag.ModalityLUTSequence))
            {
                throw new ArgumentException("The DICOM dataset should not have the 'ModalityLUTSequence' attribute.", nameof(dataset));
            }

            // 3. Check if the dataset has the 'BitsAllocated' tag and is set to 16.
            ThrowArgumentExeceptionIfNotEquals(ExpectedBitsAllocated, dataset.GetRequiredDicomAttribute<int>(DicomTag.BitsAllocated), "The DICOM dataset has an unsupported value for the 'BitsAllocated' attribute.");

            // 4. A dataset should have 'MONOCHROME2' for the photometric interpretation (i.e. is gray-scale).
            ThrowArgumentExeceptionIfNotEquals(ExpectedPhotometricInterpretation, DicomExtensions.DicomTrim(dataset.GetRequiredDicomAttribute<string>(DicomTag.PhotometricInterpretation)), "The DICOM dataset has an unsupported value for the 'PhotometricInterpretation' attribute.");

            // 5. A slice should only have one sample per pixel (1 channel at each pixel).
            ThrowArgumentExeceptionIfNotEquals(ExpectedSamplesPerPixel, dataset.GetRequiredDicomAttribute<int>(DicomTag.SamplesPerPixel), "The DICOM dataset has an unsupported value for the 'SamplesPerPixel' attribute.");

            // 6. Validate the SOP class of the slice is either MR/ CT and has the correct matching 'Modality' attribute.
            var modality = dataset.GetRequiredDicomAttribute<string>(DicomTag.Modality);

            if (sliceInformation.SopClass == DicomUID.CTImageStorage)
            {
                ThrowArgumentExeceptionIfNotEquals(DicomConstants.CTModality, modality, "The DICOM dataset has an 'CTImageStorage' SOP class but does not have CT for the 'Modality' attribute.");
            }

            if (sliceInformation.SopClass == DicomUID.MRImageStorage)
            {
                ThrowArgumentExeceptionIfNotEquals(DicomConstants.MRModality, modality, "The DICOM dataset has an 'MRImageStorage' SOP class but does not have MR for the 'Modality' attribute.");
            }

            // 7. Check the high bit against the bit stored.
            ThrowArgumentExeceptionIfNotEquals((int)sliceInformation.HighBit + 1, dataset.GetRequiredDicomAttribute<int>(DicomTag.BitsStored), $"The DICOM dataset has an unsupported value for the 'BitsStored' attribute. High bit value: {sliceInformation.HighBit}.");
        }

        /// <summary>
        /// Validates that all slices in the provided array matches the reference slice on the following properties:
        ///     1. SOP Class
        ///     2. Width
        ///     3. Height
        ///     4. Voxel Width
        ///     5. Voxel Height
        ///     6. Rescale slope
        ///     7. Rescale intercept.
        /// </summary>
        /// <param name="sliceInformation">The slice information to validate against the reference slice.</param>
        /// <param name="referenceSlice">The reference slice to match the properties against.</param>
        /// <exception cref="ArgumentException">A conformance check did not pass between slices.</exception>
        private static void ValidateSliceInformation(SliceInformation sliceInformation, SliceInformation referenceSlice)
        {
            ThrowArgumentExeceptionIfNotEquals(referenceSlice.SopClass, sliceInformation.SopClass, $"Slice at position '{sliceInformation.SlicePosition}' has an inconsistent SOP class.");
            ThrowArgumentExeceptionIfNotEquals(referenceSlice.Width, sliceInformation.Width, $"Slice at position '{sliceInformation.SlicePosition}' has an inconsistent width.");
            ThrowArgumentExeceptionIfNotEquals(referenceSlice.Height, sliceInformation.Height, $"Slice at position '{sliceInformation.SlicePosition}' has an inconsistent height.");
            ThrowArgumentExeceptionIfNotEquals(referenceSlice.VoxelWidthInMillimeters, sliceInformation.VoxelWidthInMillimeters, $"Slice at position '{sliceInformation.SlicePosition}' has an inconsistent voxel width (spacing X).");
            ThrowArgumentExeceptionIfNotEquals(referenceSlice.VoxelHeightInMillimeters, sliceInformation.VoxelHeightInMillimeters, $"Slice at position '{sliceInformation.SlicePosition}' has an inconsistent voxel height (spacing Y).");
            ThrowArgumentExeceptionIfNotEquals(referenceSlice.RescaleSlope, sliceInformation.RescaleSlope, $"Slice at position '{sliceInformation.RescaleSlope}' has an inconsistent rescale slope.");
            ThrowArgumentExeceptionIfNotEquals(referenceSlice.RescaleIntercept, sliceInformation.RescaleIntercept, $"Slice at position '{sliceInformation.RescaleIntercept}' has an inconsistent rescale intercept.");
        }

        /// <summary>
        /// Throws a common argument exception if the expected value does not equal the actual value.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="expectedValue">The expected value.</param>
        /// <param name="actualValue">The actual value.</param>
        /// <param name="messageContent">Any additional message contant to raise in the argument exception other than expected did not equal actual.</param>
        /// <exception cref="ArgumentException">If the expected value does not equal the actual value.</exception>
        private static void ThrowArgumentExeceptionIfNotEquals<T>(T expectedValue, T actualValue, string messageContent)
        {
            if (!expectedValue.Equals(actualValue))
            {
                throw new ArgumentException($"{messageContent} Expected: '{expectedValue}', Actual: '{actualValue}'.", "DicomSeriesContent");
            }
        }

        /// <summary>
        /// Checks the slice spacing conformance based on the acceptance test.
        /// </summary>
        /// <param name="volumeInformation">The volume information.</param>
        /// <param name="acceptanceTest">The acceptance test.</param>
        /// <exception cref="ArgumentNullException">The volume information or acceptance test was null.</exception>
        /// <exception cref="ArgumentException">The acceptance test did not pass for a slice.</exception>
        private static void CheckSliceSpacingConformance(VolumeInformation volumeInformation, IVolumeGeometricAcceptanceTest acceptanceTest)
        {
            for (var i = 1; i < volumeInformation.Depth; i++)
            {
                var spacing = volumeInformation.GetSliceInformation(i).SlicePosition - volumeInformation.GetSliceInformation(i - 1).SlicePosition;

                if (!acceptanceTest.AcceptSliceSpacingError(volumeInformation.SopClass, spacing, volumeInformation.VoxelDepthInMillimeters))
                {
                    throw new ArgumentException($"The spacing between slice {i - 1} and {i} was inconsistent.");
                }
            }
        }

        /// <summary>
        /// Checks the grid conformance of the provided volume information based on the provided geometric acceptance test.
        /// </summary>
        /// <param name="volumeInformation">The volume information.</param>
        /// <param name="acceptanceTest">The acceptance test.</param>
        /// <exception cref="ArgumentNullException">The volume information or acceptance test was null.</exception>
        /// <exception cref="ArgumentException">The series did not conform to a regular grid.</exception>
        private static void CheckGridConformance(VolumeInformation volumeInformation, IVolumeGeometricAcceptanceTest acceptanceTest)
        {
            volumeInformation = volumeInformation ?? throw new ArgumentNullException(nameof(volumeInformation));
            acceptanceTest = acceptanceTest ?? throw new ArgumentNullException(nameof(acceptanceTest));

            var scales = Matrix3.Diag(
                                volumeInformation.VoxelWidthInMillimeters,
                                volumeInformation.VoxelHeightInMillimeters,
                                volumeInformation.VoxelDepthInMillimeters);

            if (volumeInformation.Depth != volumeInformation.Depth)
            {
                throw new ArgumentException("Mismatch between depth and number of slices.", nameof(volumeInformation));
            }

            for (int z = 0; z < volumeInformation.Depth; z++)
            {
                var sliceInformation = volumeInformation.GetSliceInformation(z);
                var sliceScales = Matrix3.Diag(sliceInformation.VoxelWidthInMillimeters, sliceInformation.VoxelHeightInMillimeters, 0);
                var sliceOrientation = Matrix3.FromColumns(sliceInformation.Direction.Column(0), sliceInformation.Direction.Column(1), new Point3D(0, 0, 0));

                // Check corners of each slice only
                for (uint y = 0; y < sliceInformation.Height; y += sliceInformation.Height - 1)
                {
                    for (uint x = 0; x < sliceInformation.Width; x += sliceInformation.Width - 1)
                    {
                        var point = new Point3D(x, y, z);
                        var patientCoordViaSliceFrame = sliceOrientation * sliceScales * point + sliceInformation.Origin;
                        var patientCoordViaGridFrame = volumeInformation.Direction * scales * point + volumeInformation.Origin;

                        if (!acceptanceTest.AcceptPositionError(volumeInformation.SopClass, patientCoordViaSliceFrame, patientCoordViaGridFrame))
                        {
                            throw new ArgumentException("The series did not conform to a regular grid.", nameof(volumeInformation));
                        }
                    }
                }
            }
        }
    }
}