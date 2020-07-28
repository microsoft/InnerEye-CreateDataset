///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace ImageProcessingClrTest
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using InnerEye.CreateDataset.ImageProcessing;

    [TestClass]
    public class ConvolutionTests
    {
        [TestMethod]
        public void TestKernel_byte()
        {
            const int W = 512, H = 312, D = 256;

            byte[] image = new byte[W * H * D];
            int stride = W;
            int leap = W * H;

            // Make a test image that looks like a single bright pixel against a black background
            image[(D / 2) * leap + (H / 2) * stride + (W / 2)] = byte.MaxValue;

            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            w.Start();

            float sigma_x = 1.0f, sigma_y = 2.0f, sigma_z = 3.0f;
            Convolution.Convolve(
                image, W, H, D,
                new Direction[] { Direction.DirectionX, Direction.DirectionY, Direction.DirectionZ },
                new float[] { sigma_x, sigma_y, sigma_z });

            w.Stop();

            Console.WriteLine("Convolution took {0} ms.", w.ElapsedMilliseconds);

            // Check that area under curve is 1.0f and centre pixel is the maximum of the multivariate gaussian
            // with diagonal covariance matrix sigma_x^2, sigma_y^2, sigma_z^2
            float det_Sigma = sigma_x * sigma_x * sigma_y * sigma_y * sigma_z * sigma_z;
            float max = (float)Math.Pow(Math.Pow(2.0 * Math.PI, 3.0) * det_Sigma, -0.5);

            float sum = 0.0f;
            for (int z = 0; z < D; z++)
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        sum += image[z * leap + y * stride + x];

            float centre = image[(D / 2) * leap + (H / 2) * stride + (W / 2)];

            Assert.AreEqual(centre, byte.MaxValue*max, 0.5f);

            // Alas truncation of lots of values < 0.5 to 0.0 costs too much
            // volume for this test to work without large tolerance:
            Assert.AreEqual(sum, 255.0f, 100.0f);
        }



        [TestMethod]
        public void TestKernel_float()
        {
            const int W = 512, H = 312, D = 256;

            float[] image = new float[W * H * D];
            int stride = W;
            int leap = W * H;

            // Make a test image that looks like a single bright pixel against a black background
            image[(D / 2) * leap + (H / 2) * stride + (W / 2)] = 1.0f;

            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            w.Start();

            float sigma_x = 1.0f, sigma_y = 2.0f, sigma_z = 3.0f;
            Convolution.Convolve(
                image, W, H, D,
                new Direction[] { Direction.DirectionX, Direction.DirectionY, Direction.DirectionZ },
                new float[] { sigma_x, sigma_y, sigma_z });

            w.Stop();

            Console.WriteLine("Convolution took {0} ms.", w.ElapsedMilliseconds);

            // Check that area under curve is 1.0f and centre pixel is the maximum of the multivariate gaussian
            // with diagonal covariance matrix sigma_x^2, sigma_y^2, sigma_z^2
            float det_Sigma = sigma_x * sigma_x * sigma_y * sigma_y * sigma_z * sigma_z;
            float max = (float)Math.Pow( Math.Pow(2.0*Math.PI, 3.0) * det_Sigma, -0.5);

            float sum = 0.0f;
            for (int z = 0; z < D; z++)
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        sum += image[z * leap + y * stride + x];

            float centre = image[(D / 2) * leap + (H / 2) * stride + (W / 2)];

            Assert.AreEqual(centre, max, 0.001f);
            Assert.AreEqual(sum, 1.0f, 0.01f);
        }

        [TestMethod]
        public void TestKernel_short()
        {
            const int W = 512, H = 312, D = 256;

            short[] image = new short[W * H * D];
            int stride = W;
            int leap = W * H;

            // Make a test image that looks like a single bright pixel against a black background
            image[(D / 2) * leap + (H / 2) * stride + (W / 2)] = short.MaxValue;

            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            w.Start();

            float sigma_x = 1.0f, sigma_y = 2.0f, sigma_z = 3.0f;
            InnerEye.CreateDataset.ImageProcessing.Convolution.Convolve(
                image, W, H, D,
                new Direction[] { Direction.DirectionX, Direction.DirectionY, Direction.DirectionZ },
                new float[] { sigma_x, sigma_y, sigma_z });

            w.Stop();

            Console.WriteLine("Convolution took {0} ms.", w.ElapsedMilliseconds);

            // Check that area under curve is 1.0f and centre pixel is the maximum of the multivariate gaussian
            // with diagonal covariance matrix sigma_x^2, sigma_y^2, sigma_z^2
            float det_Sigma = sigma_x * sigma_x * sigma_y * sigma_y * sigma_z * sigma_z;
            float max = (float)Math.Pow(Math.Pow(2.0 * Math.PI, 3.0) * det_Sigma, -0.5);

            float sum = 0.0f;
            for (int z = 0; z < D; z++)
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        sum += image[z * leap + y * stride + x];

            float centre = image[(D / 2) * leap + (H / 2) * stride + (W / 2)];


            //Console.WriteLine("That took {0} ms.", w.ElapsedMilliseconds);

            for (int z = D / 2 - 4; z <= D / 2 + 4; z++)
            {
                for (int y = H / 2 - 4; y <= H / 2 + 4; y++)
                {
                    for (int x = W / 2 - 4; x <= W / 2 + 4; x++)
                        Console.Write("{0} ", (image[z * leap + y * stride + x]));
                    Console.WriteLine("");
                }
                Console.WriteLine();
            }


            Assert.AreEqual(max * short.MaxValue, centre, 1.0);
            Assert.AreEqual(short.MaxValue, sum, 200.0f);
        }


        //[TestMethod]
        // antonsc: Test removed because it does not have any assertions.
        public void TestTiming()
        {
            const int W = 512, H = 512, D = 256;

            float[] image = new float[W * H * D];
            int stride = W;
            int leap = W * H;

            System.Random rng = new Random();
            for (int i = 0; i < image.Length; i++)
                image[i] = (float)(rng.NextDouble());

            // Make a test image that looks like a single bright pixel against a black background
            image[(D / 2) * leap + (H / 2) * stride + (W / 2)] = 1.0f;

            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            w.Start();

            float sigma_x = 1.0f, sigma_y = 2.0f, sigma_z = 3.0f;
            Convolution.Convolve(
                image, W, H, D,
                new Direction[] { Direction.DirectionX, Direction.DirectionY, Direction.DirectionZ },
                new float[] { sigma_x, sigma_y, sigma_z });

            w.Stop();

            Console.WriteLine("Convolution of 512x512x256 volume with 3D Gaussian kernel with diag(sqrt(Sigma))= 1,2,3 took {0} ms.", w.ElapsedMilliseconds);
        }

        [TestMethod]
        public void TestConvolutionWithSmallImage()
        {
            const int W = 1, H = 1, D = 1; // image deliberately much smaller than kernel

            float[] image = new float[W * H * D];

            float sigma_x = 1.0f, sigma_y = 2.0f, sigma_z = 3.0f;
            Convolution.Convolve(
                image, W, H, D,
                new Direction[] { Direction.DirectionX, Direction.DirectionY, Direction.DirectionZ },
                new float[] { sigma_x, sigma_y, sigma_z });
        }
    }
}
