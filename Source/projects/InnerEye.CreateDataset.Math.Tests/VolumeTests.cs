///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using System;
    using System.Linq;
    using InnerEye.CreateDataset.Volumes;

    using NUnit.Framework;

    [TestFixture]
    public class VolumeTests
    {
        [Test]
        public void VoxelVolume()
        {
            var spacingX = 2.0;
            var spacingY = 3.0;
            var spacingZ = 4.0;
            var volume = new Volume3D<byte>(2, 3, 4, spacingX, spacingY, spacingZ);
            Assert.AreEqual(24.0, volume.VoxelVolume, 1.0e-10);
        }

        
        [TestCase(1, 1)]
        [TestCase(1, 10)]
        [TestCase(10, 1)]
        [TestCase(7, 8)]
        public void Volume2DGetCoordinates(int dimX, int dimY)
        {
            var volume = new Volume2D<byte>(dimX, dimY, 1, 1, new Point2D(), new Matrix2());
            foreach (var x in Enumerable.Range(0, dimX))
            {
                foreach (var y in Enumerable.Range(0, dimY))
                {
                    var index = volume.GetIndex(x, y);
                    var (x2, y2) = volume.GetCoordinates(index);
                    Assert.AreEqual(x, x2, "coordinates-index-coordindates roundtrip failed");
                    Assert.AreEqual(y, y2, "coordinates-index-coordindates roundtrip failed");
                }
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => volume.GetCoordinates(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => volume.GetCoordinates(volume.Length));
        }

        [Test]
        public void VoxelIndex()
        {
            var volume = new Volume3D<byte>(3, 3, 3);
            volume.ParallelIterateSlices(p =>
            {
                var expected = p.x + p.y * volume.DimX + p.z * volume.DimXY;
                Assert.AreEqual(expected, volume.GetIndex(p.x, p.y, p.z));
            });
        }
    }
}
