///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    /// <summary>
    /// Contains statistics about the set of voxel values found in a region.
    /// </summary>
    public class VoxelCounts
    {
        /// <summary>
        /// Creates a new instance of the class, with all counters set to 0.
        /// </summary>
        public VoxelCounts()
        {
            Foreground = 0;
            Other = 0;
        }

        /// <summary>
        /// Creates a new instance of the class, with all counters set to the given values.
        /// </summary>
        public VoxelCounts(uint foreground, uint other)
        {
            Foreground = foreground;
            Other = other;
        }

        /// <summary>
        /// Gets the number of voxels that have the foreground value.
        /// </summary>
        public uint Foreground { get; set; }

        /// <summary>
        /// Gets the number of voxels that have a value that is different from the foreground
        /// (background and any other, possibly unexpected values).
        /// </summary>
        public uint Other { get; set; }

        /// <summary>
        /// Gets the total number of voxels that the present object is tracking.
        /// </summary>
        public uint Total => Foreground + Other;

        /// <summary>
        /// Add up the corresponding fields of the two arguments.
        /// </summary>
        /// <param name="count1"></param>
        /// <param name="count2"></param>
        /// <returns></returns>
        public static VoxelCounts operator +(VoxelCounts left, VoxelCounts right)
        {
            return new VoxelCounts(
                left.Foreground + right.Foreground,
                left.Other + right.Other);
        }

        public static VoxelCounts Add(VoxelCounts left, VoxelCounts right)
        {
            return left + right;
        }
    }
}
