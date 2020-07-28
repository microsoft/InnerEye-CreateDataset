///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dicom;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Class representing the metadata of a Volume3D object.
    /// </summary>
    public class VolumeInformation : BaseInformation
    {
        /// <summary>
        /// The readonly slice information array.
        /// </summary>
        private readonly SliceInformation[] _sliceInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeInformation"/> class.
        /// </summary>
        /// <param name="width">The number of voxels in the X-dimension of the volume.</param>
        /// <param name="height">The number of voxels in the Y-dimension of the volume</param>
        /// <param name="depth">The number of voxels in the Z-dimension of the volume</param>
        /// <param name="voxelWidth">The width of a single voxel in millimeters.</param>
        /// <param name="voxelHeight">The height of a single voxel in millimeters.</param>
        /// <param name="voxelDepth">The depth of a single voxel in millimeters.</param>
        /// <param name="rescaleSlope">The rescale slope.</param>
        /// <param name="rescaleIntercept">The rescale intercept.</param>
        /// <param name="highBit">The high bit value for the underlying image information.</param>
        /// <param name="signedPixelRepresentation">If the pixel representation for the underlying volume is signed.</param>
        /// <param name="sopClass">The SOP class of this volume.</param>
        /// <param name="origin">The origin of the volume coordinate system.</param>
        /// <param name="direction">The directional matrix that represents the transform of the image orientation to the patient coordinate system.</param>
        /// <param name="sliceInformation">
        /// The slice information collection that constructed this volume information.
        /// Note: at this point we assume the slice information is sorted in ascending order using the slice position.</param>
        /// <exception cref="ArgumentNullException">The slice information collection or directional matrix was null.</exception>
        /// <exception cref="ArgumentException">The depth value did not match the slice information array length.</exception>
        private VolumeInformation(
            uint width,
            uint height,
            uint depth,
            double voxelWidthInMillimeters,
            double voxelHeightInMillimeters,
            double voxelDepthInMillimeters,
            double rescaleSlope,
            double rescaleIntercept,
            uint highBit,
            bool signedPixelRepresentation,
            DicomUID sopClass,
            Point3D origin,
            Matrix3 direction,
            SliceInformation[] sliceInformation)
            : base(width, height, voxelWidthInMillimeters, voxelHeightInMillimeters, rescaleSlope, rescaleIntercept, highBit, signedPixelRepresentation, sopClass, origin, direction)
        {
            _sliceInformation = sliceInformation ?? throw new ArgumentNullException(nameof(sliceInformation));

            Depth = depth;
            VoxelDepthInMillimeters = voxelDepthInMillimeters;

            if (sliceInformation.Length != depth)
            {
                throw new ArgumentException("The provided slice information array did not have an array length that matched the depth value.", nameof(sliceInformation));
            }
        }

        /// <summary>
        /// Gets the depth of the volume (DimZ; the number of voxels in the Z-dimension).
        /// </summary>
        /// <value>
        public uint Depth { get; }

        /// <summary>
        /// Gets the depth of a voxel (SpacingZ) in millimeters.
        /// This is the median value of all the slice spacings between each slice information.
        /// </summary>
        public double VoxelDepthInMillimeters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeInformation"/> class from a collection of DICOM datasets.
        /// </summary>
        /// <param name="dicomDatasets">The collection of DICOM datasets.</param>
        /// <returns>The volume information object.</returns>
        /// <exception cref="ArgumentNullException">The DICOM datasets collection is null.</exception>
        /// <exception cref="ArgumentException">The DICOM datasets collection has less than 2 slices or a slice has missing required DICOM tags.</exception>
        public static VolumeInformation Create(IEnumerable<DicomDataset> dicomDatasets)
        {
            dicomDatasets = dicomDatasets ?? throw new ArgumentNullException(nameof(dicomDatasets));
            return Create(dicomDatasets.Select(x => SliceInformation.Create(x)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeInformation"/> class.
        /// </summary>
        /// <param name="sliceInformation">The slices that make up the volume.</param>
        /// <returns>The volume information object.</returns>
        /// <exception cref="ArgumentNullException">The slice information collection is null.</exception>
        /// <exception cref="ArgumentException">The slice information collection must contain at least two slices.</exception>
        public static VolumeInformation Create(IEnumerable<SliceInformation> sliceInformation)
        {
            sliceInformation = sliceInformation ?? throw new ArgumentNullException(nameof(sliceInformation));

            // Sort the slices by slice position and use the first slice to populate fields.
            var sorted = sliceInformation.OrderBy(x => 
                            x != null ? 
                                x.SlicePosition :
                                throw new ArgumentException("The slice information should not contain null slices.", nameof(sliceInformation))).ToArray();

            if (sorted.Length < 2)
            {
                throw new ArgumentException("Must have at least two slices.", nameof(sliceInformation));
            }

            var referenceSlice = sorted[0];

            return new VolumeInformation(
                width: referenceSlice.Width,
                height: referenceSlice.Height,
                depth: (uint)sorted.Length,
                voxelWidthInMillimeters: referenceSlice.VoxelWidthInMillimeters,
                voxelHeightInMillimeters: referenceSlice.VoxelHeightInMillimeters,
                voxelDepthInMillimeters: GetMedianSliceSpacing(sorted),
                rescaleSlope: referenceSlice.RescaleSlope,
                rescaleIntercept: referenceSlice.RescaleIntercept,
                highBit: referenceSlice.HighBit,
                signedPixelRepresentation: referenceSlice.SignedPixelRepresentation,
                sopClass: referenceSlice.SopClass,
                origin: referenceSlice.Origin,
                direction: referenceSlice.Direction,
                sorted);
        }

        /// <summary>
        /// Gets the ith value from the slice information array.
        /// </summary>
        /// <param name="index">The index of the slice information value to get.</param>
        /// <returns>The slice information object.</returns>
        /// <exception cref="IndexOutOfRangeException">The index is larger than the number of slices in the slice information.</exception>
        public SliceInformation GetSliceInformation(int index) => _sliceInformation[index];

        /// <summary>
        /// Gets the median slice spacing between all the provided slice informations.
        /// Note: This method assumes the slice information has been sorted in ascending order by slice position.
        /// </summary>
        /// <param name="sliceInformation">The slice information to get the slice spacings from, sorted in ascending order by slice position.</param>
        /// <returns>The median slice spacing between all the slice informations.</returns>
        /// <exception cref="ArgumentNullException">The provided slice information was null.</exception>
        private static double GetMedianSliceSpacing(SliceInformation[] sortedSliceInformation)
        {
            sortedSliceInformation = sortedSliceInformation ?? throw new ArgumentNullException(nameof(sortedSliceInformation));

            // The result will be an array 1 minus the number of slices (as the spacing in the comparison of two slices)
            var result = new double[sortedSliceInformation.Length - 1];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = sortedSliceInformation[i + 1].SlicePosition - sortedSliceInformation[i].SlicePosition;
            }

            // Sort the result spacings, and return the median value.
            return result.OrderBy(x => x).ElementAt(result.Length / 2);
        }
    }
}