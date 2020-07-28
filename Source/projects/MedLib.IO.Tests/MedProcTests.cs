///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedILib.Tests
{
    using System;
    using InnerEye.CreateDataset.Volumes;
    using InnerEye.CreateDataset.Math;
    using NUnit.Framework;

    /// <summary>
    /// These are legacy tests for re-sampling from MedLib. Ported over without really checking whether
    /// they test the right thing. To be removed when restructuring image resampling.
    /// </summary>
    [TestFixture]
    public class MedProcTests
    {
        private static int Clamp(int val, int min, int max)
        {
            return Math.Max(min, Math.Min(max, val));
        }

        /// <seealso>https://en.wikipedia.org/wiki/Linear_interpolation</seealso>
        private static double Linear(double x, double v1, double v2)
        {
            return (1.0 - x) * v1 + x * v2;
        }

        /// <seealso>https://en.wikipedia.org/wiki/Bilinear_interpolation</seealso>
        private static double Bilinear(double x, double y, double v1, double v2, double v3, double v4)
        {
            var s = Linear(x, v1, v2);
            var t = Linear(x, v3, v4);
            return Linear(y, s, t);
        }

        /// <seealso>https://en.wikipedia.org/wiki/Trilinear_interpolation</seealso>
        private static double Trilinear(double x, double y, double z,
            double v1, double v2, double v3, double v4,
            double v5, double v6, double v7, double v8)
        {
            var s = Bilinear(x, y, v1, v2, v3, v4);
            var t = Bilinear(x, y, v5, v6, v7, v8);
            return Linear(z, s, t);
        }
        /// <summary>
        /// Tests whether the output of trilinear downsampling is identical to a reference implementation.
        /// antonsc: This is a legacy test that was ported over from the MedImage library.
        /// </summary>
        [Test]
        public void TrilinearResampleDownsample()
        {
            var factor = 1.0/3;
            CheckTrilinearResample(factor, inDimX: 6, inDimY: 6, inDimZ: 6);
            CheckTrilinearResample(factor, inDimX: 6, inDimY: 10, inDimZ: 13);
            // starting at an origin outside the input image bounds leads to "outside values" being used
            CheckTrilinearResample(factor, inDimX: 6, inDimY: 10, inDimZ: 6, outOriginOverride: new Point3D(-1, -1, -1));
        }

        /// <summary>
        /// Tests whether the output of trilinear upsampling is identical to a reference implementation.
        /// </summary>
        public void TrilinearResampleUpsample()
        {
            var factor = 3;
            CheckTrilinearResample(factor, inDimX: 6, inDimY: 6, inDimZ: 6);
            CheckTrilinearResample(factor, inDimX: 6, inDimY: 10, inDimZ: 13);
            // starting at an origin outside the input image bounds leads to "outside values" being used
            CheckTrilinearResample(factor, inDimX: 6, inDimY: 10, inDimZ: 6, outOriginOverride: new Point3D(-1, -1, -1));
        }

        private static void CheckTrilinearResample(double factor, int inDimX, int inDimY, int inDimZ, Point3D? outOriginOverride = null)
        {
            // Assumptions:
            // For interpolation purposes, a pixel is represented as a point.
            // The center of the top left pixel has (x,y,z)=(0,0,0) point coordinates.
            // The image bounds are -0.5 <= x,y,z <= dimX,Y,Z - 0.5.
            // Everything outside the image bounds is interpolated with a special value.
            // Points within [-0.5,0] or [dim-1,dim-0.5] will behave as if there is an
            // outer 1px border of the same colour as the actual border.

            // create 3D input image
            var direction = Matrix3.CreateIdentity();
            double inSpacing = 1;
            var inOrigin = new Point3D(0, 0, 0);
            var input = new Volume3D<double>(inDimX, inDimY, inDimZ, 
                inSpacing, inSpacing, inSpacing, inOrigin, direction);

            var i = 0;
            for (int z = 0; z < inDimZ; z++)
            {
                for (int y = 0; y < inDimY; y++)
                {
                    for (int x = 0; x < inDimX; x++)
                    {
                        input[x, y, z] = i++;
                    }
                }
            }

            // resample using MedProc
            int outDimX = (int)Math.Round(inDimX * factor);
            int outDimY = (int)Math.Round(inDimY * factor);
            int outDimZ = (int)Math.Round(inDimZ * factor);
            var outSpacing = (int)(inSpacing / factor);
            Point3D outOrigin;
            if (outOriginOverride != null)
            {
                outOrigin = outOriginOverride.Value;
            }
            else
            {
                outOrigin = input.Transform.PixelToPhysical(new Point3D(inDimX/4.0, inDimY/4.0, inDimZ/4.0));
            }

            double outsideValue = 42;
            var output = input.ResampleLinear(outDimX, outDimY, outDimZ);

            // resample with reference implementation
            for (var x=0; x < outDimX; x++)
            {
                for (var y=0; y < outDimY; y++)
                {
                    for (var z=0; z < outDimZ; z++)
                    {
                        var outputPixel = new Point3D(x, y, z);
                        var physical = output.Transform.PixelToPhysical(outputPixel);
                        var inputPixel = input.Transform.PhysicalToPixel(physical);

                        double valInterpolated;
                        // if outside
                        if (inputPixel.X < -0.5 || inputPixel.Y < -0.5 || inputPixel.Z < -0.5 || 
                            inputPixel.X >= (double)inDimX - 0.5 || 
                            inputPixel.Y >= (double)inDimY - 0.5 ||
                            inputPixel.Z >= (double)inDimZ - 0.5)
                        {
                            valInterpolated = outsideValue;
                        }
                        else
                        {
                            var xx = (int)Math.Floor(inputPixel.X);
                            var yy = (int)Math.Floor(inputPixel.Y);
                            var zz = (int)Math.Floor(inputPixel.Z);
                            var x2 = inputPixel.X - xx; // [0,1]
                            var y2 = inputPixel.Y - yy;
                            var z2 = inputPixel.Z - zz;

                            Func<int,int> ClampX = val => Clamp(val, 0, inDimX - 1);
                            Func<int,int> ClampY = val => Clamp(val, 0, inDimY - 1);
                            Func<int,int> ClampZ = val => Clamp(val, 0, inDimZ - 1);

                            var v1 = input[ClampX(xx),     ClampY(yy),     ClampZ(zz)];
                            var v2 = input[ClampX(xx + 1), ClampY(yy),     ClampZ(zz)];
                            var v3 = input[ClampX(xx),     ClampY(yy + 1), ClampZ(zz)];
                            var v4 = input[ClampX(xx + 1), ClampY(yy + 1), ClampZ(zz)];
                            var v5 = input[ClampX(xx),     ClampY(yy),     ClampZ(zz + 1)];
                            var v6 = input[ClampX(xx + 1), ClampY(yy),     ClampZ(zz + 1)];
                            var v7 = input[ClampX(xx),     ClampY(yy + 1), ClampZ(zz + 1)];
                            var v8 = input[ClampX(xx + 1), ClampY(yy + 1), ClampZ(zz + 1)];
                            valInterpolated = Trilinear(x2, y2, z2, v1, v2, v3, v4, v5, v6, v7, v8);
                        }

                        Assert.AreEqual(valInterpolated, output[x,y,z], 1e-4, $"Mismatch at x={x} y={y} z={z}");
                    }
                }
            }
        }
        
    }
}