///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using InnerEye.CreateDataset.ImageProcessing;

namespace ImageProcessingClrTest
{
    [TestClass]
    public class ConnectedComponentsTests
    {
        [TestMethod]
        public void TestConnectedComponents()
        {
            {
                const int W = 8, H = 8, D = 3;

                // Image containing three identical 8x8 slices
                byte[] image = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,
                    1, 1, 1, 0, 0, 0, 1, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,

                    0, 1, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,
                    1, 1, 1, 0, 0, 0, 1, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,

                    0, 1, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,
                    1, 1, 1, 0, 0, 0, 1, 0,
                    0, 1, 0, 0, 0, 1, 0, 1
                };

                byte[] desired = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 5, 0, 0, 0, 6, 0, 7,
                    5, 5, 5, 0, 0, 0, 8, 0,
                    0, 5, 0, 0, 0, 9, 0,10
                };

                ushort[] result = new ushort[image.Length];
                var count = ConnectedComponents.Find3d(image, W, H, D, 0, result);

                Assert.AreEqual(11, count);

                for (var i = 0; i < image.Length; i++)
                    Assert.AreEqual(result[i], desired[i % (W*H)]);
            }

            {
                const int W = 8, H = 8, D = 3;
                byte[] image = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,
                    1, 1, 1, 0, 0, 0, 1, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,

                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,

                    0, 1, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,
                    1, 1, 1, 0, 0, 0, 1, 0,
                    0, 1, 0, 0, 0, 1, 0, 1
                };

                byte[] desired = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 5, 0, 0, 0, 6, 0, 7,
                    5, 5, 5, 0, 0, 0, 8, 0,
                    0, 5, 0, 0, 0, 9, 0,10
                };


                ushort[] result = new ushort[image.Length];
                var count = ConnectedComponents.Find3d(image, W, H, D, 0, result);

                Assert.AreEqual(21, count);

                for (var i = 0; i < W * H; i++)
                    Assert.AreEqual(result[i], desired[i]);

                for (var i = 0; i < W * H; i++)
                    Assert.AreEqual(result[W*H + i], 0);

                for (var i = 0; i < W * H; i++)
                    Assert.AreEqual(result[2 * W * H + i], desired[i] == 0? 0 : desired[i] + 10);
            }

            {
                const int W = 8, H = 8, D = 3;
                byte[] image = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,
                    1, 1, 1, 0, 0, 0, 1, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,

                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0, // note the single non-zero entry
	                0, 0, 0, 0, 0, 0, 0, 0,

                    0, 1, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 0, 0, 0, 1,
                    1, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 1, 0, 0, 0, 1, 0, 1,
                    1, 1, 1, 0, 0, 0, 1, 0,
                    0, 1, 0, 0, 0, 1, 0, 1
                };

                byte[] desired = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 5, 0, 0, 0, 6, 0, 7,
                    5, 5, 5, 0, 0, 0, 8, 0,
                    0, 5, 0, 0, 0, 9, 0,10,

                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0,11, 0, 0, 0, // note the single non-zero entry
	                0, 0, 0, 0, 0, 0, 0, 0,

                    0,12, 0, 0, 0, 0, 0,13,
                    14, 0, 0, 0, 0, 0, 0,13,
                    14, 0, 0, 0,15, 0, 0, 0,
                    0, 0, 0, 0,15, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0,16, 0, 0, 0,17, 0,18,
                    16,16,16, 0, 0, 0,19, 0,
                    0,16, 0, 0, 0,20, 0,21
                };

                ushort[] result = new ushort[image.Length];
                var count = ConnectedComponents.Find3d(image, W, H, D, 0, result);

                Assert.AreEqual(22, count);

                for (var i = 0; i < D * W * H; i++)
                    Assert.AreEqual(result[i], desired[i]);
            }
        }

        [TestMethod]
        public void TestConnectedComponentsStatistics()
        {
            {
                const int W = 8, H = 8, D = 3;

                // Image containing three identical 8x8 slices
                byte[] image = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 5, 0, 0, 0, 6, 0, 7,
                    5, 5, 5, 0, 0, 0, 8, 0,
                    0, 5, 0, 0, 0, 9, 0,10,

                    0, 1, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 5, 0, 0, 0, 6, 0, 7,
                    5, 5, 5, 0, 0, 0, 8, 0,
                    0, 5, 0, 0, 0, 9, 0,10,

                    0, 1, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 5, 0, 0, 0, 6, 0, 7,
                    5, 5, 5, 0, 0, 0, 8, 0,
                    0, 5, 0, 0, 0, 9, 0,10
                };

                byte[] desired = new byte[]{
                    0, 1, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 0, 0, 0, 2,
                    3, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 4, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0,
                    0, 5, 0, 0, 0, 6, 0, 7,
                    5, 5, 5, 0, 0, 0, 8, 0,
                    0, 5, 0, 0, 0, 9, 0,10
                };

                ulong[] expectedPixelCounts = new ulong[] { 8*8*3 - 17*3, 3, 6, 6, 6, 15, 3, 3, 3, 3, 3 };

                ushort[] result = new ushort[image.Length];
                var statistics = ConnectedComponents.Find3dWithStatistics(image, W, H, D, 0, result);

                Assert.AreEqual(11, statistics.Length);
                for(var i=0; i<statistics.Length; i++)
                {
                    Assert.AreEqual(statistics[i].PixelCount, expectedPixelCounts[i]);
                    Assert.AreEqual(statistics[i].InputLabel, i);
                }

                for (var i = 0; i < image.Length; i++)
                    Assert.AreEqual(result[i], desired[i % (W * H)]);
            }
           
        }
    }
}