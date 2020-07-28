///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Models
{
    using System;
    using Dicom;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Class representing the metadata of a slice from a Volume3D object.
    /// </summary>
    public class SliceInformation : BaseInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SliceInformation"/> class.
        /// </summary>
        /// <param name="width">The width of this slice in number of voxels.</param>
        /// <param name="height">The height of this slice in number of voxels.</param>
        /// <param name="voxelWidthInMillimeters">The voxel width of each voxel in this volume in millimeters.</param>
        /// <param name="voxelHeightInMillimeters">The voxel height of each voxel in this volume in millimeters.</param>
        /// <param name="slicePosition">The position of this slice in the 3-dimensional volume.</param>
        /// <param name="rescaleSlope">The rescale slope.</param>
        /// <param name="rescaleIntercept">The rescale intercept.</param>
        /// <param name="highBit">The high bit value for the underlying image information.</param>
        /// <param name="signedPixelRepresentation">If the pixel representation for the underlying volume is signed.</param>
        /// <param name="sopClass">The SOP class of this slice.</param>
        /// <param name="origin">The origin coordinate for this slice.</param>
        /// <param name="direction">The image orientation patient directional matrix.</param>
        /// <param name="dicomDataset">The reference DICOM dataset this slice information was built from.</param>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset or directional matrix was null.</exception>
        private SliceInformation(
            uint width,
            uint height,
            double voxelWidthInMillimeters,
            double voxelHeightInMillimeters,
            double slicePosition,
            double rescaleSlope,
            double rescaleIntercept,
            uint highBit,
            bool signedPixelRepresentation,
            DicomUID sopClass,
            Point3D origin,
            Matrix3 direction,
            DicomDataset dicomDataset)
            : base (width, height, voxelWidthInMillimeters, voxelHeightInMillimeters, rescaleSlope, rescaleIntercept, highBit, signedPixelRepresentation, sopClass, origin, direction)
        {
            SlicePosition = slicePosition;
            DicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));
        }

        /// <summary>
        /// Gets the position of the slice (the dot product of the Z direction and the origin).
        /// </summary>
        public double SlicePosition { get; }

        /// <summary>
        /// Gets the reference DICOM dataset for this slice.
        /// </summary>
        public DicomDataset DicomDataset { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SliceInformation"/> class.
        /// </summary>
        /// <param name="dicomDataset">The DICOM dataset to abstract the metadata from.</param>
        /// <exception cref="ArgumentException">
        /// The DICOM dataset has a negative width or height.
        /// The DICOM dataset did not contain the required DICOM attributes ('SOPClassUID', 'ImagePositionPatient', 'Columns', 'Rows', 'PixelSpacing', 'HighBit')
        /// </exception>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset is null.</exception>
        public static SliceInformation Create(DicomDataset dicomDataset)
        {
            dicomDataset = dicomDataset ?? throw new ArgumentNullException(nameof(dicomDataset));

            var (width, height) = dicomDataset.GetSliceSize();
            var origin = dicomDataset.GetOrigin();
            var direction = dicomDataset.GetDirectionalMatrix();
            var sopClass = dicomDataset.GetSopClass();
            var rescaleIntercept = 0.0;
            var rescaleSlope = 1.0;

            // Only fetch the rescale and intercept from CT images.
            if (sopClass == DicomUID.CTImageStorage)
            {
                rescaleIntercept = dicomDataset.GetRescaleIntercept();
                rescaleSlope = dicomDataset.GetRescaleSlope();
            }

            return new SliceInformation(
                width: width >= 0 ? (uint)width : throw new ArgumentException("The width of a slice cannot be less than 0", nameof(dicomDataset)),
                height: height >= 0 ? (uint)height : throw new ArgumentException("The height of a slice cannot be less than 0", nameof(dicomDataset)),
                voxelWidthInMillimeters: dicomDataset.GetPixelSpacingX(),
                voxelHeightInMillimeters: dicomDataset.GetPixelSpacingY(),
                slicePosition: GetSlicePosition(direction, origin),
                rescaleSlope: rescaleSlope,
                rescaleIntercept: rescaleIntercept,
                highBit: dicomDataset.GetHighBit(),
                signedPixelRepresentation: dicomDataset.IsSignedPixelRepresentation(),
                origin: origin,
                direction: direction,
                sopClass: sopClass,
                dicomDataset: dicomDataset);
        }

        /// <summary>
        /// Gets the slice position from the image orientation to patient matrix direction and origin.
        /// </summary>
        /// <param name="direction">The image orientation to patient matrix.</param>
        /// <param name="origin">The origin 3-dimensional point.</param>
        /// <returns>The position of the slice.</returns>
        private static double GetSlicePosition(Matrix3 direction, Point3D origin)
            => Point3D.DotProd(direction.Column(2), origin);
    }
}