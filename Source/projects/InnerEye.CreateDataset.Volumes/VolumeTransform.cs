///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Volumes
{
    using System;

    /// <summary>
    /// Constructs a Transform3 representing the composite transform Origin + Spacing*Direction
    /// </summary>
    public class VolumeTransform
    {
        /// <summary>
        /// Returns a Transform3 instance that maps a point in integer volume coordinates into the reference coordinate system (aka Dicom Patient Coordinates)
        /// </summary>
        /// <value>
        /// The pixel to physical.
        /// </value>
        public Transform3 DataToDicom { get; }

        /// <summary>
        /// Returns the inverse of PixelToPysical i.e. the transform from the reference coordinate system to voxel corodinates 
        /// </summary>
        /// <value>
        /// The physical to pixel.
        /// </value>
        public Transform3 DicomToData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeTransform"/> class.
        /// </summary>
        /// <param name="spacingX">The spacing x.</param>
        /// <param name="spacingY">The spacing y.</param>
        /// <param name="spacingZ">The spacing z.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="direction">The direction.</param>
        public VolumeTransform(double spacingX, double spacingY, double spacingZ, Point3D origin, Matrix3 direction)
        {
            SpacingX = spacingX;
            SpacingY = spacingY;
            SpacingZ = spacingZ;

            Origin = origin;
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));

            var scaleMatrix = Matrix3.Diag(spacingX, spacingY, spacingZ);

            DataToDicom = new Transform3(direction * scaleMatrix, origin);
            DicomToData = DataToDicom.Inverse(); 
        }

        /// <summary>
        /// Gets the X dimension pixel spacing.
        /// </summary>
        /// <value>
        /// The X dimension pixel spacing.
        /// </value>
        public double SpacingX { get; }

        /// <summary>
        /// Gets the Y dimension pixel spacing.
        /// </summary>
        /// <value>
        /// The Y dimension pixel spacing.
        /// </value>
        public double SpacingY { get; }

        /// <summary>
        /// Gets the Z dimension pixel spacing.
        /// </summary>
        /// <value>
        /// The Z dimension pixel spacing.
        /// </value>
        public double SpacingZ { get; }

        /// <summary>
        /// Gets the patient origin.
        /// </summary>
        /// <value>
        /// The patient origin.
        /// </value>
        public Point3D Origin { get; }

        /// <summary>
        /// Gets the directional 3x3 matrix.
        /// </summary>
        /// <value>
        /// The directional 3x3 matrix.
        /// </value>
        public Matrix3 Direction { get; }

        /// <summary>
        /// Transforms a 3D point in physical coordinates (e.g. DICOM patient coordinate system) to pixels.
        /// </summary>
        /// <param name="physicalPoint">The 3D point to convert.</param>
        /// <returns>
        /// The transformed point.
        /// </returns>
        public Point3D PhysicalToPixel(Point3D physicalPoint) => DicomToData * physicalPoint;

        /// <summary>
        /// Transforms a 3D point in pixel coordinates (e.g. Image space) to DICOM patient coordinate system.
        /// </summary>
        /// <param name="pixelPoint">The 3D point to convert.</param>
        /// <returns>
        /// The transformed point.
        /// </returns>
        public Point3D PixelToPhysical(Point3D pixelPoint) => DataToDicom * pixelPoint;
    }
}