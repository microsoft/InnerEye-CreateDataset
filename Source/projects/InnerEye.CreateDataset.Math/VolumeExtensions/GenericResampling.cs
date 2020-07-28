///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using InnerEye.CreateDataset.Volumes;
    using System;
    using System.Threading.Tasks;

    public static class GenericResampling
    {
        private static T Nearest<T>(this Volume3D<T> input, Point3D pixel, T outsideValue)
        {
            return input.Nearest(pixel.X, pixel.Y, pixel.Z, outsideValue);
        }

        // Expects pixel coordinates.
        // antonsc: using a fixed default value for outside can lead to artefacts
        // https://innereye.visualstudio.com/InnerEye/_workitems/edit/2116
        private static T Nearest<T>(this Volume3D<T> input, double pixelX, double pixelY, double pixelZ, T outsideValue)
        {
            if (pixelX < -0.5 || pixelY < -0.5 || pixelZ < -0.5
                || pixelX >= input.DimX - 0.5
                || pixelY >= input.DimY - 0.5
                || pixelZ >= input.DimZ - 0.5)
            {
                return outsideValue;
            }

            return input[(int)(pixelX + 0.5), (int)(pixelY + 0.5), (int)(pixelZ + 0.5)];
        }

        // https://innereye.visualstudio.com/InnerEye/_workitems/edit/2116
        public static Volume3D<T> ResampleNearest<T>(this Volume3D<T> input, int dimX, int dimY, int dimZ, T outsideValue = default(T))
        {
            double spacingX = input.SpacingX * (input.DimX - 1) / (dimX - 1);
            double spacingY = input.SpacingY * (input.DimY - 1) / (dimY - 1);
            double spacingZ = input.SpacingZ * (input.DimZ - 1) / (dimZ - 1);
            var output = new Volume3D<T>(dimX, dimY, dimZ, spacingX, spacingY, spacingZ, input.Origin, input.Direction);
            ResampleImage(input, output, outsideValue, null);
            return output;
        }

        // https://innereye.visualstudio.com/InnerEye/_workitems/edit/2116
        public static void ResampleImage<T>(Volume3D<T> input, Volume3D<T> output, T outsideValue, Func<double, double, double, T> interpolationFunc)
        {
            int dimX = output.DimX;
            int dimY = output.DimY;
            int dimZ = output.DimZ;
            int dimXy = output.DimXY;

            double factorX = output.SpacingX / input.SpacingX;
            double factorY = output.SpacingY / input.SpacingY;
            double factorZ = output.SpacingZ / input.SpacingZ;

            var shift = input.Transform.PhysicalToPixel(output.Origin);

            Parallel.For(0, dimZ, z =>
            {
                double inputZ = z * factorZ + shift.Z;
                int index = dimXy * z - 1;
                for (int y = 0; y < dimY; y++)
                {
                    double inputY = y * factorY + shift.Y;
                    for (int x = 0; x < dimX; x++)
                    {
                        double inputX = x * factorX + shift.X;
                        if (interpolationFunc != null)
                        {
                            output[++index] = interpolationFunc(inputX, inputY, inputZ);
                        }
                        else
                        {
                            output[++index] = input.Nearest(inputX, inputY, inputZ, outsideValue);
                        }
                    }
                }
            });
        }
    }
}
