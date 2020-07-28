///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Contains statistics about the voxels inside of a contour.
    /// </summary>
    public class ContourStatistics
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="sizeInCubicCentimeters"></param>
        /// <param name="voxelValueMean"></param>
        /// <param name="voxelValueStandardDeviation"></param>
        public ContourStatistics(double sizeInCubicCentimeters, double voxelValueMean, double voxelValueStandardDeviation)
        {
            SizeInCubicCentimeters = sizeInCubicCentimeters;
            VoxelValueMean = voxelValueMean;
            VoxelValueStandardDeviation = voxelValueStandardDeviation;
        }

        /// <summary>
        /// Gets the volume of the region enclosed by the contour, in cubic centimeters.
        /// </summary>
        public double SizeInCubicCentimeters { get; }

        /// <summary>
        /// Gets the arithmetic mean of the voxel values in the region enclosed by the contour.
        /// </summary>
        public double VoxelValueMean { get; }

        /// <summary>
        /// Gets the standard deviation of the voxel values in the region enclosed by the contour.
        /// </summary>
        public double VoxelValueStandardDeviation { get; }

        /// <summary>
        /// Computes an instance of contour statistics, <see cref="ContourStatistics"/>, from those voxels
        /// of the <paramref name="originalVolume"/> where the mask volume attains the foreground value.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="mask"></param>
        /// <param name="foreground"></param>
        /// <returns></returns>
        public static ContourStatistics FromVolumeAndMask(ReadOnlyVolume3D<short> image, Volume3D<byte> mask, byte foreground = 1)
        {
            image = image ?? throw new ArgumentNullException(nameof(image));
            mask = mask ?? throw new ArgumentNullException(nameof(mask));
            if (mask.Length != image.Length)
            {
                throw new ArgumentException("Image and mask must have the same length", nameof(image));
            }

            double mean = 0;
            double variance = 0;
            double sum = 0;
            ulong count = 0;

            // StreamingStatistics.MeanVariance
            for (int i = 0; i < image.Length; i++)
            {
                if (mask[i] == foreground)
                {
                    count++;
                    double xi = image[i];
                    sum += xi;
                    var diff = (count * xi) - sum;
                    if (count > 1)
                    {
                        variance += (diff * diff) / (count * (count - 1));
                    }

                    mean += (xi - mean) / count;
                }
            }

            var volumeSizeInCC = count * image.VoxelVolume / 1000d;
            var standardDeviation = count == 0 ? 0 : Math.Sqrt(variance / count);

            return new ContourStatistics(volumeSizeInCC, mean, standardDeviation);
        }
    }
}