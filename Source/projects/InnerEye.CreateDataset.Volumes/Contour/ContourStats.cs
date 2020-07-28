///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public struct ContourStats
    {
        public ContourStats(double sizeIncc, double mean, double standardDeviation)
        {
            SizeIncc = sizeIncc;
            Mean = mean;
            StandardDeviation = standardDeviation;
        }

        // cm^3 or cc
        public double SizeIncc { get; }

        public double Mean { get; }

        public double StandardDeviation { get; }
    }

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public static class ContourStatsExtensions
    {
        public static ContourStats CalculateContourStats(ReadOnlyVolume3D<short> originalVolume, Volume3D<byte> contourVolume, byte foreground = 1)
        {
            double numberOfContourPoints = 0;
            long sum = 0;
            ulong sumSqMinusMean = 0;

            for (int i = 0; i < originalVolume.Length; i++)
            {
                if (contourVolume[i] == foreground)
                {
                    numberOfContourPoints++;
                    sum += originalVolume[i];
                }
            }

            var mean = numberOfContourPoints == 0 ? 0d : sum / numberOfContourPoints;

            for (int i = 0; i < originalVolume.Length; i++)
            {
                if (contourVolume[i] == foreground)
                {
                    var d = originalVolume[i] - mean;
                    sumSqMinusMean += (ulong)(d * d);
                }
            }

            var volumeSizeInmm = numberOfContourPoints * originalVolume.VoxelVolume / 1000d;
            var standardDeviation = mean == 0 ? 0 : Math.Sqrt(sumSqMinusMean / numberOfContourPoints);

            return new ContourStats(volumeSizeInmm, mean, standardDeviation);
        }
    }
}