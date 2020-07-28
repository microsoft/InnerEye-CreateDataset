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
    /// The base information for either a volume or volume slice.
    /// </summary>
    public abstract class BaseInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseInformation"/> class.
        /// </summary>
        /// <param name="width">The width of this slice in number of voxels.</param>
        /// <param name="height">The height of this slice in number of voxels.</param>
        /// <param name="voxelWidthInMillimeters">The voxel width of each voxel in this volume in millimeters.</param>
        /// <param name="voxelHeightInMillimeters">The voxel height of each voxel in this volume in millimeters.</param>
        /// <param name="rescaleSlope">The rescale slope.</param>
        /// <param name="rescaleIntercept">The rescale intercept.</param>
        /// <param name="highBit">The high bit value for the underlying image information.</param>
        /// <param name="signedPixelRepresentation">If the pixel representation for the underlying volume is signed.</param>
        /// <param name="sopClass">The SOP class of this slice.</param>
        /// <param name="origin">The origin coordinate for this slice.</param>
        /// <param name="direction">The image orientation patient directional matrix.</param>
        /// <exception cref="ArgumentNullException">The directional matrix was null.</exception>
        protected BaseInformation(
            uint width,
            uint height,
            double voxelWidthInMillimeters,
            double voxelHeightInMillimeters,
            double rescaleSlope,
            double rescaleIntercept,
            uint highBit,
            bool signedPixelRepresentation,
            DicomUID sopClass,
            Point3D origin,
            Matrix3 direction)
        {
            Width = width;
            Height = height;
            VoxelWidthInMillimeters = voxelWidthInMillimeters;
            VoxelHeightInMillimeters = voxelHeightInMillimeters;
            RescaleSlope = rescaleSlope;
            RescaleIntercept = rescaleIntercept;
            HighBit = highBit;
            SignedPixelRepresentation = signedPixelRepresentation;
            SopClass = sopClass;
            Origin = origin;
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
        }

        /// <summary>
        /// Gets the width of the volume (the number of voxels in the X-dimension of this slice).
        /// </summary>
        public uint Width { get; }

        /// <summary>
        /// Gets the height of the volume (the number of voxels in the Y-dimension of this slice).
        /// </summary>
        public uint Height { get; }

        /// <summary>
        /// Gets the width of a voxel (SpacingX) in millimeters.
        /// </summary>
        public double VoxelWidthInMillimeters { get; }

        /// <summary>
        /// Gets the height of a voxel (SpacingY) in millimeters.
        /// </summary>
        public double VoxelHeightInMillimeters { get; }

        /// <summary>
        /// Gets the rescale slope.
        /// If this is NOT a CT image, the slope will be 1.0d; otherwise the value from the DICOM dataset.
        /// </summary>
        public double RescaleSlope { get; }

        /// <summary>
        /// Gets the rescale intercept.
        /// If this is NOT a CT image, the intercept will be 0.0d; otherwise the value from the DICOM dataset.
        /// </summary>
        public double RescaleIntercept { get; }

        /// <summary>
        /// Gets the high bit of the DICOM slice.
        /// </summary>
        public uint HighBit { get; }

        /// <summary>
        /// Gets a value indicating whether the pixel representation is signed.
        /// </summary>
        public bool SignedPixelRepresentation { get; }

        /// <summary>
        /// Gets the SOP class of this DICOM slice.
        /// </summary>
        public DicomUID SopClass { get; }

        /// <summary>
        /// Gets the origin of the volume.
        /// </summary>
        public Point3D Origin { get; }

        /// <summary>
        /// Gets the directional matrix of the volume (image orientation to the patient).
        /// </summary>
        public Matrix3 Direction { get; }
    }
}