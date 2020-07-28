///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System.Threading.Tasks;

    using Volumes;

    public static class EuclideanDistance3D
    {
        public static Volume3D<float> EuclideanDistance(this Volume3D<byte> input, double mmMarginX, double mmMarginY, double mmMarginZ, int iterations = 1)
        {

            var region = input.GetInterestRegion();
            region = region.Dilate(input, mmMarginX, mmMarginY, mmMarginZ);

            region = new Region3D<int>(region.MinimumX, region.MinimumY, region.MinimumZ, region.MaximumX, region.MaximumY, region.MaximumZ);

            return EuclideanDistance(input, iterations, region);
        }


        private static Volume3D<float> EuclideanDistance(Volume3D<byte> input, int iterations, Region3D<int> region)
        {
            var distanceMap = input.CreateSameSize<float>();

            Parallel.For(0, distanceMap.Length, delegate (int i)
            {
                distanceMap[i] = input[i] != 0 ? 0 : float.MaxValue;
            });

            EuclideanDistance(input, distanceMap, iterations, region);

            return distanceMap;

        }

        private static void EuclideanDistance<T>(Volume3D<T> input, Volume3D<float> distanceMap, int iterations,
            Region3D<int> region)
        {
            var distanceLookUp = CreateDistanceLookUp(input);

            for (var i = 0; i < iterations; i++)
            {
                RunForwardPass(distanceMap, distanceLookUp, region);
                RunBackwardPass(distanceMap, distanceLookUp, region);
            }
        }

        private static Volume3D<float> CreateDistanceLookUp<T>(Volume3D<T> input)
        {
            var distanceLookUp = new Volume3D<float>(3, 3, 3, input.SpacingX, input.SpacingY, input.SpacingZ, input.Origin, input.Direction);

            var index = 0;
            var center = new Point3D(1, 1, 1);

            for (var z = 0; z < 3; z++)
            {
                for (var y = 0; y < 3; y++)
                {
                    for (var x = 0; x < 3; x++)
                    {
                        var point = distanceLookUp.Transform.DataToDicom.Basis * (new Point3D(x, y, z) - center);
                        distanceLookUp[index++] = (float)point.Norm();
                    }
                }
            }

            return distanceLookUp;
        }

        private static void RunForwardPass(Volume3D<float> distanceMap, Volume3D<float> distanceLookUp,
            Region3D<int> region)
        {
            var dimX = distanceMap.DimX;
            var dimY = distanceMap.DimY;
            var dimZ = distanceMap.DimZ;
            var dimXy = distanceMap.DimXY;

            for (var z = region.MinimumZ; z <= region.MaximumZ; z++)
            {
                int zDimXy = z * dimXy;
                for (var y = region.MinimumY; y <= region.MaximumY; y++)
                {
                    int yDimX = y * dimX + zDimXy;
                    for (var x = region.MinimumX; x <= region.MaximumX; x++)
                    {
                        var index = x + yDimX;

                        double minDist = distanceMap[index];

                        for (var dy = -1; dy <= 1; dy++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                if (x + dx >= 0 && x + dx < dimX && y + dy >= 0 && y + dy < dimY && z - 1 >= 0 &&
                                    z - 1 < dimZ)
                                {
                                    var dist = distanceMap[x + dx + (y + dy) * dimX + (z - 1) * dimXy] +
                                                  distanceLookUp[distanceLookUp.GetIndex(dx + 1, dy + 1, 0)];

                                    if (dist < minDist)
                                    {
                                        minDist = dist;
                                    }
                                }
                            }
                        }

                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (x + dx >= 0 && x + dx < dimX && y - 1 >= 0 && y - 1 < dimY)
                            {
                                var dist = distanceMap[x + dx + (y - 1) * dimX + z * dimXy] +
                                              distanceLookUp[distanceLookUp.GetIndex(dx + 1, 0, 1)];

                                if (dist < minDist)
                                {
                                    minDist = dist;
                                }
                            }
                        }

                        if (x - 1 >= 0 && x - 1 < dimX)
                        {
                            var dist = distanceMap[(x - 1) + y * dimX + z * dimXy] +
                                       distanceLookUp[distanceLookUp.GetIndex(0, 1, 1)];

                            if (dist < minDist)
                            {
                                minDist = dist;
                            }
                        }

                        distanceMap[index] = (float)minDist;
                    }
                }
            }
        }

        private static void RunBackwardPass(Volume3D<float> distmap, Volume3D<float> distanceLookUp,
           Region3D<int> region)
        {
            var dimX = distmap.DimX;
            var dimY = distmap.DimY;
            var dimZ = distmap.DimZ;
            var dimXy = distmap.DimXY;

            for (var z = region.MaximumZ - 1; z >= region.MinimumZ; z--)
            {
                for (var y = region.MaximumY - 1; y >= region.MinimumY; y--)
                {
                    for (var x = region.MaximumX - 1; x >= region.MinimumX; x--)
                    {
                        var index = x + y * dimX + z * dimXy;

                        var minDist = distmap[index];

                        for (var dy = 1; dy >= -1; dy--)
                        {
                            for (var dx = 1; dx >= -1; dx--)
                            {
                                if (x + dx >= 0 && x + dx < dimX && y + dy >= 0 && y + dy < dimY && z + 1 >= 0 &&
                                    z + 1 < dimZ)
                                {
                                    var dist = distmap[x + dx + (y + dy) * dimX + (z + 1) * dimXy] +
                                                  distanceLookUp[distanceLookUp.GetIndex(dx + 1, dy + 1, 2)];

                                    if (dist < minDist)
                                    {
                                        minDist = dist;
                                    }
                                }
                            }
                        }

                        for (var dx = 1; dx >= -1; dx--)
                        {
                            if (x + dx >= 0 && x + dx < dimX && y + 1 >= 0 && y + 1 < dimY)
                            {
                                var dist = distmap[x + dx + (y + 1) * dimX + z * dimXy] +
                                              distanceLookUp[distanceLookUp.GetIndex(dx + 1, 2, 1)];

                                if (dist < minDist)
                                {
                                    minDist = dist;
                                }
                            }
                        }

                        if (x + 1 >= 0 && x + 1 < dimX)
                        {
                            var dist = distmap[(x + 1) + y * dimX + z * dimXy] +
                                       distanceLookUp[distanceLookUp.GetIndex(2, 1, 1)];

                            if (dist < minDist)
                            {
                                minDist = dist;
                            }
                        }

                        distmap[index] = minDist;
                    }
                }
            }
        }
    }
}