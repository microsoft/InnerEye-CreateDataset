///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace Dicom
{
    using System;
    using MedLib.IO.Extensions;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// DICOM dataset extension methods for extracting attribute information.
    /// </summary>
    public static class DicomDatasetExtensions
    {
        /// <summary>
        /// Gets the value of the 'RescaleIntercept' attribute as a double.
        /// Note: This should only be used on CT datasets.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>If the pixel representation is signed.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'RescaleIntercept' tag or was not a CT image.</exception>
        public static double GetRescaleIntercept(this DicomDataset dicomDataset)
        {
            CheckSopClass(dicomDataset, DicomUID.CTImageStorage);
            return dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.RescaleIntercept);
        }

        /// <summary>
        /// Gets the value of the 'RescaleSlope' attribute as a double.
        /// Note: This should only be used on CT datasets.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>If the pixel representation is signed.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'RescaleSlope' tag or was not a CT image.</exception>
        public static double GetRescaleSlope(this DicomDataset dicomDataset)
        {
            CheckSopClass(dicomDataset, DicomUID.CTImageStorage);
            return dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.RescaleSlope);
        }

        /// <summary>
        /// Checks the SOP class of the provided DICOM dataset matches the expected DICOM UID.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to get the SOP class from.</param>
        /// <param name="dicomUID">The expected SOP class of the DICOM dataset.</param>
        /// <exception cref="ArgumentNullException">The DICOM dataset or DICOM UID is null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not match the expected SOP class.</exception>
        public static void CheckSopClass(this DicomDataset dicomDataset, DicomUID dicomUID)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            dicomUID = dicomUID ?? throw new ArgumentNullException(nameof(dicomUID));

            if (dicomDataset.GetSopClass() != dicomUID)
            {
                throw new ArgumentException("The provided DICOM dataset is not a CT image.", nameof(dicomDataset));
            }
        }

        /// <summary>
        /// Gets the value from the 'PixelRepresentation' attribute and checks if it equals 1.
        /// If 1, the underlying voxel information is signed.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>If the pixel representation is signed.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'PixelRepresentation' tag.</exception>
        public static bool IsSignedPixelRepresentation(this DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return dicomDataset.GetRequiredDicomAttribute<int>(DicomTag.PixelRepresentation) == 1;
        }

        /// <summary>
        /// Gets the high bit value from the DICOM dataset.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>The high bit value.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'HighBit' tag.</exception>
        public static uint GetHighBit(this DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return (uint)dicomDataset.GetRequiredDicomAttribute<int>(DicomTag.HighBit);
        }

        /// <summary>
        /// Gets the SOP class from a DICOM dataset.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to extract the SOP class from.</param>
        /// <returns>The DICOM UID SOP class.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'SOPClassUID' tag.</exception>
        public static DicomUID GetSopClass(this DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return dicomDataset.GetRequiredDicomAttribute<DicomUID>(DicomTag.SOPClassUID);
        }

        /// <summary>
        /// Gets the origin (Image position patient) from the Dicom dataset.
        /// </summary>
        /// <param name="dicomDataset">The Dicom dataset.</param>
        /// <returns>The origin.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'ImagePositionPatient' tag or did not have 3 parts to the attribute.</exception>
        public static Point3D GetOrigin(this DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return new Point3D(
                dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImagePositionPatient, 0),
                dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImagePositionPatient, 1),
                dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImagePositionPatient, 2));
        }

        /// <summary>
        /// Gets the width and height of the slice in voxels.
        /// </summary>
        /// <param name="dicomDataset">The dicom dataset.</param>
        /// <returns>The width and height in voxels of the slice.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'Columns' or 'Rows' tag.</exception>
        public static (int Width, int Height) GetSliceSize(this DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return (dicomDataset.GetRequiredDicomAttribute<int>(DicomTag.Columns),
                dicomDataset.GetRequiredDicomAttribute<int>(DicomTag.Rows));
        }

        /// <summary>
        /// Gets the pixel spacings from the DICOM dataset.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>The pixel spacings.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'PixelSpacing' tag or the tag did not have 2 parts.</exception>
        public static (double SpacingX, double SpacingY) GetPixelSpacings(this DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return (dicomDataset.GetPixelSpacingX(), dicomDataset.GetPixelSpacingY());
        }

        /// <summary>
        /// Gets the width of a voxel from the DICOM dataset.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>The voxel width.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'PixelSpacing' tag or the tag did not have 2 parts.</exception>
        public static double GetPixelSpacingX(this DicomDataset dicomDataset)
        {
            // Note: Pixel spacing in DICOM is back to front, so we take the second item (1) from this tag for X spacing.
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.PixelSpacing, 1);
        }

        /// <summary>
        /// Gets the height of a voxel from the DICOM dataset.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>The voxel height.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'PixelSpacing' tag.</exception>
        public static double GetPixelSpacingY(this DicomDataset dicomDataset)
        {
            // Note: Pixel spacing in DICOM is back to front, so we take the first item (0) from this tag for Y spacing.
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
            return dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.PixelSpacing, 0);
        }

        /// <summary>
        /// Gets the directional matrix (image orientation to the patient) from the DICOM dataset.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset.</param>
        /// <returns>The directional matrix (image orientation to the patient).</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        /// <exception cref="ArgumentException">The provided DICOM dataset did not contain the 'ImageOrientationPatient' tag or the tag did not have 6 parts.</exception>
        public static Matrix3 GetDirectionalMatrix(this DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));

            // Extract the 2 3-dimensional points from the 'ImageOrientationPatient' DICOM attribute.
            var imageOrientationPatientX = new Point3D(
                    dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImageOrientationPatient, 0),
                    dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImageOrientationPatient, 1),
                    dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImageOrientationPatient, 2));

            var imageOrientationPatientY = new Point3D(
                    dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImageOrientationPatient, 3),
                    dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImageOrientationPatient, 4),
                    dicomDataset.GetRequiredDicomAttribute<double>(DicomTag.ImageOrientationPatient, 5));

            // Insist that the image orientations are of unit length, this is defined in the standard but
            // rounding in serialization can cause them to be slightly off with respect to the double representation.
            imageOrientationPatientX /= imageOrientationPatientX.Norm();
            imageOrientationPatientY /= imageOrientationPatientY.Norm();

            // The standard also insists iop[0] and iop[1] are orthogonal.
            return Matrix3.FromColumns(
                imageOrientationPatientX,
                imageOrientationPatientY,
                Point3D.CrossProd(imageOrientationPatientX, imageOrientationPatientY));
        }
    }
}