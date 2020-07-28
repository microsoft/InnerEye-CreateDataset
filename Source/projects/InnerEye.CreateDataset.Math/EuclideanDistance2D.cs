///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System.Threading.Tasks;
    
    using Volumes;
    
    public static class EuclideanDistance2D
    {
        public static Volume2D<float> EuclideanDistance(this Volume2D<byte> input, int iterations = 1)
        {
            return EuclideanDistance(input, iterations, new Region2D<int>(0, 0, input.DimX, input.DimY));
        }

        private static Volume2D<float> EuclideanDistance(Volume2D<byte> input, int iterations, Region2D<int> region)
        {
            var distanceMap = input.AllocateStorage<byte, float>();

            Parallel.For(0, input.Length, delegate (int i)
            {
                distanceMap[i] = input[i] != 0 ? 0 : float.MaxValue;
            });

            EuclideanDistance(input, distanceMap, iterations, region);

            return distanceMap;
        }

        private static void EuclideanDistance<T>(Volume2D<T> input, Volume2D<float> distanceMap, int iterations, Region2D<int> region)
        {
            var distanceLookUp = CreateDistanceLookUp(input);

            for (var i = 0; i < iterations; i++)
            {
                RunForwardPass(distanceMap, distanceLookUp, region);
                RunBackwardPass(distanceMap, distanceLookUp, region);
            }
        }

        private static Volume2D<double> CreateDistanceLookUp<T>(Volume2D<T> input)
        {
            var distanceLookUp = new Volume2D<double>(3, 3, input.SpacingX, input.SpacingY, input.Origin, input.Direction);

            var index = 0;
            var center = new Point2D(1, 1);

            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var point = distanceLookUp.PixelToPhysical(new Point2D(x, y)) - distanceLookUp.PixelToPhysical(center);
                    distanceLookUp[index++] = point.Norm();
                }
            }

            return distanceLookUp;
        }

        private static void RunForwardPass(this Volume2D<float> distanceMap, Volume2D<double> distanceLookUp, Region2D<int> region)
        {
            var dimX = distanceMap.DimX;
            var dimY = distanceMap.DimY;

            for (var y = region.MinimumY; y < region.MaximumY; y++)
            {
                for (var x = region.MinimumX; x < region.MaximumX; x++)
                {
                    var index = distanceMap.GetIndex(x, y);

                    double currentDistance;
                    double minDist = distanceMap[index];

                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (x + dx >= 0 && x + dx < dimX && y - 1 >= 0 && y - 1 < dimY)
                        {
                            currentDistance = distanceMap[x + dx + (y - 1) * dimX] + distanceLookUp[dx + 1, 0];

                            if (currentDistance < minDist)
                            {
                                minDist = currentDistance;
                            }
                        }
                    }

                    if (x - 1 >= 0 && x - 1 < dimX)
                    {
                        currentDistance = distanceMap[(x - 1) + y * dimX] + distanceLookUp[0, 1];

                        if (currentDistance < minDist)
                        {
                            minDist = currentDistance;
                        }
                    }

                    distanceMap[index] = (float)minDist;
                }
            }
        }
        
        private static void RunBackwardPass(this Volume2D<float> distanceMap, Volume2D<double> distanceLookUp, Region2D<int> region)
        {
            var dimX = distanceMap.DimX;
            var dimY = distanceMap.DimY;

            for (var y = region.MaximumY - 1; y >= region.MinimumY; y--)
            {
                for (var x = region.MaximumX - 1; x >= region.MinimumX; x--)
                {
                    var index = x + y * dimX;

                    double currentDistance;
                    double minDist = distanceMap[index];

                    for (var dx = 1; dx >= -1; dx--)
                    {
                        if (x + dx >= 0 && x + dx < dimX && y + 1 >= 0 && y + 1 < dimY)
                        {
                            currentDistance = distanceMap[x + dx + (y + 1) * dimX] + distanceLookUp[dx + 1, 2];

                            if (currentDistance < minDist)
                            {
                                minDist = currentDistance;
                            }
                        }
                    }

                    if (x + 1 >= 0 && x + 1 < dimX)
                    {
                        currentDistance = distanceMap[(x + 1) + y * dimX] + distanceLookUp[2, 1];

                        if (currentDistance < minDist)
                        {
                            minDist = currentDistance;
                        }
                    }

                    distanceMap[index] = (float)minDist;
                }
            }
        }
    }
}