///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset
{
    using System;
    using System.Diagnostics;

    using InnerEye.CreateDataset.Common.Models;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;

    public class GeometricNormalization
    {

        /// <summary>
        /// Runs image standardization to get voxels of the given size. 
        /// The steps are:
        /// - Re-sampling 
        /// - median smoothing.
        /// </summary>
        /// <param name="input">The raw CT image.</param>
        /// <param name="param">The set of processing parameters to use.</param>
        /// <param name="convolveUsingMedianFilter">If true, run median smoothing after resampling.</param>
        /// <returns></returns>
        public static Volume3D<byte> StandardiseNearest(Volume3D<byte> input, 
            GeometricNormalizationParameters param, 
            bool convolveUsingMedianFilter = false)
        {
            var (dimX, dimY, dimZ) = CalculateStandardisedDimension(input, param.StandardiseSpacings);
            var output = input.ResampleNearest(dimX, dimY, dimZ);
            return 
                convolveUsingMedianFilter && param.MedianFilterRadius > 0
                ? output.MedianSmooth(param.MedianFilterRadius) 
                : output;
        }

        /// <summary>
        /// Runs image standardization to get voxels of the given size. 
        /// The steps are:
        /// - Re-sampling 
        /// - median smoothing.
        /// </summary>
        /// <param name="input">The raw CT image.</param>
        /// <param name="param">The set of processing parameters to use.</param>
        /// <param name="convolveUsingMedianFilter">If true, run median smoothing after resampling.</param>
        /// <returns></returns>
        public static Volume3D<short> StandardiseLinear(Volume3D<short> input,
            GeometricNormalizationParameters param, 
            bool convolveUsingMedianFilter = false)
        {
            var (dimX, dimY, dimZ) = CalculateStandardisedDimension(input, param.StandardiseSpacings);
            var output = input.ResampleLinear(dimX, dimY, dimZ);
            return 
                convolveUsingMedianFilter && param.MedianFilterRadius > 0 
                ? output.MedianSmooth(param.MedianFilterRadius) 
                : output;
        }

        /// <summary>
        /// Computes a rounded value for a floating point number that represents the spacing of a medical volume.
        /// Pre-processing can lead to values like 0.9999994 being read from Nifti, those can cause off-by-1 errors
        /// when computing resampled image dimensions.
        /// </summary>
        /// <param name="spacing"></param>
        /// <returns></returns>
        public static double RoundSpacing(double spacing)
        {
            return System.Math.Round(spacing, 5);
        }

        /// <summary>
        /// Computes the size of a resampled image, to achieve a given voxel spacing.
        /// Returns a 3-tuple of sizes of the resampled image.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input">The image that should be resampled.</param>
        /// <param name="desiredSpacing">The voxel spacing as a 3-dim array that the resampled
        /// image should have.</param>
        /// <returns></returns>
        public static (int NewDimX, int NewDimY, int NewDimZ) CalculateStandardisedDimension<T>(
            Volume3D<T> input, 
            double[] desiredSpacing)
        {
            desiredSpacing = desiredSpacing ?? throw new ArgumentNullException(nameof(desiredSpacing));
            if (desiredSpacing.Length != 3)
            {
                throw new ArgumentException("Spacing must be given as a 3-element array.", nameof(desiredSpacing));
            }

            var roundedX = RoundSpacing(input.SpacingX);
            var roundedY = RoundSpacing(input.SpacingY);
            var roundedZ = RoundSpacing(input.SpacingZ);
            if (roundedX != input.SpacingX ||
                roundedY != input.SpacingY ||
                roundedZ != input.SpacingZ)
            {
                Trace.TraceInformation($"Rounding the spacing from ({input.SpacingX}, {input.SpacingY}, {input.SpacingZ}) to ({roundedX}, {roundedY}, {roundedZ})");
            }

            var dimX = desiredSpacing[0] <= 0 ? input.DimX : 1 + (int)(roundedX / desiredSpacing[0] * (input.DimX - 1));
            var dimY = desiredSpacing[1] <= 0 ? input.DimY : 1 + (int)(roundedY / desiredSpacing[1] * (input.DimY - 1));
            var dimZ = desiredSpacing[2] <= 0 ? input.DimZ : 1 + (int)(roundedZ / desiredSpacing[2] * (input.DimZ - 1));
            return (dimX, dimY, dimZ);
        }
    }
}
