///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using InnerEye.CreateDataset.Contours;
    using InnerEye.CreateDataset.Volumes;

    using NUnit.Framework;

    using System.Linq;

    [TestFixture]
    public class ContourStatisticsTests
    {
        private Volume3D<T> CreateVolume<T>(T[] array, int dimX, int dimY, int dimZ) =>
            new Volume3D<T>(array, dimX, dimY, dimZ, 1, 1, 1, new Point3D(), Matrix3.CreateIdentity());

        [Test()]
        public void CheckBasicVolumeStats()
        {
            var volumeArray = new short[]
            {
                10, 10, 10,
                10, 20, 0,
                10, 0, 20
            };

            var volume = CreateVolume(volumeArray, 3, 3, 1);

            var contourVolumeArray = new byte[]
           {
                0, 0, 0,
                0, 1, 1,
                0, 1, 1
           };

            var contourVolume = CreateVolume(contourVolumeArray, 3, 3, 1);

            var stats = ContourStatistics.FromVolumeAndMask(new ReadOnlyVolume3D<short>(volume), contourVolume);

            Assert.AreEqual(10, stats.VoxelValueMean);
            Assert.AreEqual(10, stats.VoxelValueStandardDeviation);
            Assert.AreEqual(0.004, stats.SizeInCubicCentimeters);
        }

        [Test()]
        public void EmptyContour()
        {
            var volumeArray = new short[]
            {
                10, 10, 10,
                10, 20, 0,
                10, 0, 20
            };

            var volume = CreateVolume(volumeArray, 3, 3, 1);

            var contourVolumeArray = new byte[]
           {
                0, 0, 0,
                0, 0, 0,
                0, 0, 0
           };

            var contourVolume = CreateVolume(contourVolumeArray, 3, 3, 1);

            var stats = ContourStatistics.FromVolumeAndMask(new ReadOnlyVolume3D<short>(volume), contourVolume);

            Assert.AreEqual(0, stats.VoxelValueMean);
            Assert.AreEqual(0, stats.VoxelValueStandardDeviation);
            Assert.AreEqual(0, stats.SizeInCubicCentimeters);
        }

        [Test()]
        public void HugeContour()
        {
            var dimX = 256;
            var dimY = 256;
            var dimZ = 512;
            var totalSize = dimX * dimY * dimZ;
            var volumeArray = Enumerable.Range(0, totalSize).Select(x => (short)(x % 2 == 0 ? 0 : 20)).ToArray();

            var volume = CreateVolume(volumeArray, dimX, dimY, 512);

            var contourVolumeArray = Enumerable.Range(0, totalSize).Select(x => (byte)1).ToArray();

            var contourVolume = CreateVolume(contourVolumeArray, dimX, dimY, 512);

            var stats = ContourStatistics.FromVolumeAndMask(new ReadOnlyVolume3D<short>(volume), contourVolume);

            Assert.AreEqual(10, stats.VoxelValueMean);
            Assert.AreEqual(10, stats.VoxelValueStandardDeviation);
            Assert.AreEqual(totalSize / 1000d, stats.SizeInCubicCentimeters);
        }

    }
}
