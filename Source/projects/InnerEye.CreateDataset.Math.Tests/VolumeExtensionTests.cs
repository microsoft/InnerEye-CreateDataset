///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using InnerEye.CreateDataset.Common;
    using InnerEye.CreateDataset.TestHelpers;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;
    using NUnit.Framework;

    [TestFixture]
    public class VolumeExtensionTests
    {
        [Test]
        public void GetFullRegion()
        {
            var volume = new Volume3D<byte>(2, 3, 4);
            var region = volume.GetFullRegion();
            Assert.AreEqual(0, region.MinimumX);
            Assert.AreEqual(0, region.MinimumY);
            Assert.AreEqual(0, region.MinimumZ);
            Assert.AreEqual(1, region.MaximumX);
            Assert.AreEqual(2, region.MaximumY);
            Assert.AreEqual(3, region.MaximumZ);
        }

        [TestCase(-1.0f, 0)]
        [TestCase(0f, 0)]
        [TestCase(1.0f, 255)]
        // Conversion to byte uses Math.Round. Anything that is closer than 1/512 to 1.0
        // will be rounded up to 255, otherwise down to 254
        [TestCase(1.0f - 1.1f / 512f, 254)]
        [TestCase(1.0f - 0.9f / 512f, 255)]
        [TestCase(1.1f, 255)]
        [TestCase(0.499f, 127)]
        [TestCase(0.501f, 128)]
        public void PosteriorToByte(float value, byte expected)
        {
            Assert.AreEqual(expected, Converters.PosteriorToByte(value));
        }

        [TestCase(1.000001f, 1)]
        [TestCase(12345.0001f, 12345)]
        public void TryConvertToInt16Success(float value, short expected)
        {
            var converted = Converters.TryConvertToInt16(value);
            Assert.AreEqual(expected, converted);
        }

        [TestCase(1.00001f)]
        [TestCase(12345.01f)]
        public void TryConvertToInt16Fails(float value)
        {
            var ex = Assert.Throws<ArgumentException>(() => Converters.TryConvertToInt16(value), $"Value {value} should not be converted");
            Assert.IsFalse(ex.Message.Contains("at index -1"), "Index should not be in error message when not provided");
        }

        [TestCase(short.MinValue - 0.1f, short.MinValue)]
        [TestCase(short.MaxValue + 0.1f, short.MaxValue)]
        [TestCase(1.0f, 1)]
        [TestCase(1.5f, 2)]
        [TestCase(14.5f, 15)]
        [TestCase(-0.49f, 0)]
        [TestCase(-0.5f, -1)]
        [TestCase(-100f, -100)]
        public void ConvertAndClampToInt16(float value, short expected)
        {
            var converted = Converters.ClampToInt16(value);
            Assert.AreEqual(expected, converted);
        }

        [Test]
        public void VolumeMap1()
        {
            var volume = new Volume3D<byte>(2, 3, 4);
            volume[0] = 1;
            volume[23] = 23;
            var mapped = volume.Map(value => value);
            VolumeAssert.AssertVolumesMatch(volume, mapped, "");
        }

        [Test]
        public void VolumeMap2()
        {
            var volume = new Volume3D<byte>(2, 3, 4);
            var expected = new Volume3D<byte>(2, 3, 4);
            foreach (var index in volume.Array.Indices())
            {
                volume[index] = (byte)(index + 1);
                expected[index] = (byte)(index + 2);
            }
            var mapped = volume.MapIndexed(null, (value, index) =>
            {
                Assert.AreEqual(index + 1, value);
                return (byte)(index + 2);
            });
            VolumeAssert.AssertVolumesMatch(expected, mapped, "");
        }

        ///<summary>
        /// Test for median smoothing.
        /// We consider a 4x4 image with fixed byte values and
        /// perform median smoothing with radius equal to 1 (neighborhoods of size 27 voxels)
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        [Test]
        public void MedianSmoother4by4Test(int radius)
        {
            // Fill data.
            var data = new byte[]
            {
                201, 233, 149, 120, 119, 144, 243, 41, 128, 201, 144, 70, 164,
                8, 14, 56, 133, 160, 73, 216, 24, 83, 63, 191, 195, 26, 38, 45,
                196, 19, 183, 159, 133, 205, 10, 153, 34, 153, 193, 73, 119, 82,
                90, 108, 99, 159, 192, 92, 171, 103, 12, 124, 63, 99, 158, 41,
                150, 87, 4, 51, 9, 45, 221, 83
            };

            // Target values.
            var target = new byte[]
            {
                138, 138, 146, 134, 138, 138, 132, 96, 123, 123, 66, 66, 146,
                136, 50, 63, 138, 138, 151, 134, 133, 133, 120, 99, 119, 119,
                90, 91, 123, 123, 86, 91, 118, 101, 113, 98, 111, 90, 87, 73,
                85, 90, 87, 91, 93, 94, 85, 91, 118, 118, 113, 98, 111, 101,
                94, 81, 93, 99, 91, 91, 93, 94, 88, 91
            };

            // Produce median smoothing output with radius 1.
            var input = new Volume3D<byte>(data, 4, 4, 4, 2, 2, 2);
            var output = input.MedianSmooth(radius);
            // expected based on radius ie: identify if 0 radius else target if 1
            var expected = radius == 0 ? input : new Volume3D<byte>(target, 4, 4, 4, 2, 2, 2);
            VolumeAssert.AssertVolumesMatch(expected, output);
        }

        [Test]
        public void SagitallMirrorDiffOddSize()
        {
            var volume = VolumeExtensions.FromSlices<byte>(3, 3,
                new List<byte[]>
                {
                    new byte[] { 1, 2, 3, 4, 5, 4, 3, 2, 1 },
                    new byte[] { 10, 40, 50, 80, 50, 10, 70, 80, 90 }
                });
            var diff = volume.CreateSagittalSymmetricAbsoluteDifference();
            // Expected values: Take a block of 3 number in one of the input slices.
            // This is what would be mirrored. Middle element turns 0 (mirrored onto itself),
            // feature value is the absolute diff between the right and left element.
            var expected = VolumeExtensions.FromSlices(3, 3,
                new List<byte[]>
                {
                    new byte[] { 2, 0, 2, 0, 0, 0, 2, 0, 2 },
                    new byte[] { 40, 0, 40, 70, 0, 70, 20, 0, 20 }
                });
            VolumeAssert.AssertVolumesMatch(expected, diff);
        }

        [Test]
        public void SagitallMirrorDiffEvenSize()
        {
            var volume = VolumeExtensions.FromSlices(2, 2,
                new List<byte[]>
                {
                    new byte[] { 1, 2, 3, 8 },
                    new byte[] { 10, 40, 50, 10 }
                });
            var diff = volume.CreateSagittalSymmetricAbsoluteDifference();
            var expected = VolumeExtensions.FromSlices(2, 2,
                new List<byte[]>
                {
                    new byte[] { 1, 1, 5, 5 },
                    new byte[] { 30, 30, 40, 40 }
                });
            VolumeAssert.AssertVolumesMatch(expected, diff);
        }

        [Test]
        public void SagitallMirrorOddSize()
        {
            var volume = VolumeExtensions.FromSlices<byte>(3, 3,
                new List<byte[]>
                {
                    new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                    new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 }
                });
            volume.SagittalMirroringInPlace();
            // Expected values: Take a block of 3 number in one of the input slices, mirror that.
            var expected = VolumeExtensions.FromSlices(3, 3,
                new List<byte[]>
                {
                    new byte[] { 3, 2, 1, 6, 5, 4, 9, 8, 7 },
                    new byte[] { 30, 20, 10, 60, 50, 40, 90, 80, 70}
                });
            VolumeAssert.AssertVolumesMatch(expected, volume);
        }

        [Test]
        public void SagitallMirrorEvenSize()
        {
            var volume = VolumeExtensions.FromSlices(2, 2,
                new List<byte[]>
                {
                    new byte[] { 1, 2, 3, 4 },
                    new byte[] { 10, 20, 30, 40 }
                });
            volume.SagittalMirroringInPlace();
            var expected = VolumeExtensions.FromSlices(2, 2,
                new List<byte[]>
                {
                    new byte[] { 2, 1, 4, 3 },
                    new byte[] { 20, 10, 40, 30 }
                });
            VolumeAssert.AssertVolumesMatch(expected, volume);
        }

        [TestCase(256)]
        [TestCase(10)]
        [TestCase(1)]
        public void IntegralImageTest(int dimXandY)
        {
            int dimX = dimXandY;
            int dimY = dimXandY;
            int dimZ = 100;
            var volume = new Volume3D<byte>(dimXandY, dimXandY, dimZ);
            volume.Fill(255);
            var stopWatch = Stopwatch.StartNew();
            var integral = volume.IntegralImage();
            stopWatch.Stop();
            Console.WriteLine($"Time integral {stopWatch.Elapsed}");
            var sum = volume.Array.Select(x => (ulong)x).Aggregate((x, acc) => x + acc);
            Console.Write(sum);
            Assert.AreEqual(sum, integral[integral.Length - 1]);
            Assert.AreEqual(volume[0], integral[0]);
        }

        [TestCase(10, 20, 30, 11, 21, 31)]
        [TestCase(12, 22, 32, 15, 25, 35)]
        [TestCase(10, 20, 30, 10, 20, 30)]
        [TestCase(15, 25, 35, 15, 25, 35)]
        public void RegionInsideOf(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            var bigger = new Region3D<int>(10, 20, 30, 15, 25, 35);
            var test = new Region3D<int>(minX, minY, minZ, maxX, maxY, maxZ);
            Assert.IsTrue(test.InsideOf(bigger));
        }

        [TestCase(9, 20, 30, 11, 21, 31)]
        [TestCase(10, 19, 30, 11, 21, 31)]
        [TestCase(10, 20, 29, 11, 21, 31)]
        [TestCase(10, 20, 30, 16, 21, 31)]
        [TestCase(10, 20, 30, 11, 26, 31)]
        [TestCase(10, 20, 30, 11, 21, 36)]
        public void RegionNotInsideOf(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            var bigger = new Region3D<int>(10, 20, 30, 15, 25, 35);
            var test = new Region3D<int>(minX, minY, minZ, maxX, maxY, maxZ);
            Assert.IsFalse(test.InsideOf(bigger));
        }

        /// <summary>
        /// Tests the functionality of <see cref="VolumeExtensions.VoxelsInsideMask"/>
        /// </summary>
        [Test]
        public void VoxelsInsideMask()
        {
            Volume3D<T> Create<T>(T[] values)
            {
                return new Volume3D<T>(values, values.Length, 1, 1, 1, 1, 1);
            }
            var image = Create(new short[] { 4, 3, 2, 1 });
            var mask = Create(new byte[] { 0, 1, 0, 1 });
            // The two voxel values that have a corresponding 1 in the maks
            var withMaskExpected = new short[] { 3, 1 };
            Assert.AreEqual(withMaskExpected, image.VoxelsInsideMask(mask).ToArray(), "Voxels when mask is used");
            // Without the mask, all voxels values should be returned.
            var withoutMaskExpected = image.Array;
            Assert.AreEqual(withoutMaskExpected, image.VoxelsInsideMask(null).ToArray(), "Voxels when mask is empty");
            Volume3D<byte> nullImage = null;
            Assert.Throws<ArgumentNullException>(() => VolumeExtensions.VoxelsInsideMask(nullImage, mask).ToArray(), "Null image is not allowed");
        }

        [Test]
        public void VoxelCoordinates()
        {
            var volume = new Volume3D<byte>(3, 3, 3);
            volume.ParallelIterateSlices(expected =>
            {
                var actual = volume.GetCoordinates(volume.GetIndex(expected.x, expected.y, expected.z));
                Assert.AreEqual(expected, (actual.X, actual.Y, actual.Z));
            });
        }

        /// <summary>
        /// Tests the functionality of <see cref="VolumeExtensions.VoxelsInsideMaskWithCoordinates"/>
        /// </summary>
        [Test]
        public void VoxelsInsideMaskWithCoordinates()
        {
            var image = Create(new short[] { 4, 3, 2, 1 });
            var mask = Create(new byte[] { 0, 1, 0, 1 });

            IEnumerable<(Index3D, int)> CreateExpected(bool withMask)
                => image.Array
                .Select((x, i) => (image.GetCoordinates(i), x * (withMask == true ? mask[i] : 1)))
                .Where(x => x.Item2 != 0);

            Volume3D<T> Create<T>(T[] values)
            {
                return new Volume3D<T>(values, values.Length, 1, 1, 1, 1, 1);
            }

            // The two voxel values that have a corresponding 1 in the maks
            var withMaskExpected = CreateExpected(true);

            Assert.AreEqual(withMaskExpected, image.VoxelsInsideMaskWithCoordinates(mask).ToArray(), 
                "Voxels when mask is used");

            // Without the mask, all voxels values should be returned.
            Assert.AreEqual(CreateExpected(false), image.VoxelsInsideMaskWithCoordinates(null).ToArray(), 
                "Voxels when mask is empty");
            Volume3D<byte> nullImage = null;
            Assert.Throws<ArgumentNullException>(() => VolumeExtensions.VoxelsInsideMaskWithCoordinates(nullImage, mask).ToArray(), 
                "Null image is not allowed");
        }

        /// <summary>
        /// Tests the functionality of <see cref="VolumeExtensions.GetCombinedForegroundRegion"/>
        /// </summary>
        [Test]
        public void GetCombinedForegroundRegion()
        {
            var volumeA = new Volume3D<byte>(3, 3, 3);

            Assert.Throws<ArgumentNullException>(() => VolumeExtensions.GetCombinedForegroundRegion(volumeA, null), "Null image is not allowed");
            Assert.Throws<ArgumentNullException>(() => VolumeExtensions.GetCombinedForegroundRegion(null, volumeA), "Null image is not allowed");
            Assert.Throws<ArgumentNullException>(() => VolumeExtensions.GetCombinedForegroundRegion(null, null), "Null image is not allowed");

            Assert.AreEqual(new Region3D<int>(0, 0, 0, -1, -1, -1), VolumeExtensions.GetCombinedForegroundRegion(volumeA, volumeA),
               "Negative bounds expected if no foreground region found in both volumes");

            volumeA[0, 0, 0] = 1;

            Assert.AreEqual(new Region3D<int>(0, 0, 0, 0, 0, 0), VolumeExtensions.GetCombinedForegroundRegion(volumeA, volumeA), 
                "Combined region should be the foreground region of volume if other volume is also same");

            var volumeB = volumeA.CreateSameSize<byte>();
            volumeB[2, 2, 2] = 1;

            Assert.AreEqual(volumeA.GetFullRegion(), VolumeExtensions.GetCombinedForegroundRegion(volumeA, volumeB),
               "Region when combining disjoint regions");
        }

        [TestCase(new int[] {1, 2, 3, 3, 1 }, 2, 3)]
        [TestCase(new int[] {1}, 0, 1)]
        public void ArgMax(int[] values, int expectedIndex, int expectedValue)
        {
            var (Index, Maximum) = values.ArgMax();
            Assert.AreEqual(expectedIndex, Index);
            Assert.AreEqual(expectedValue, Maximum);
        }

        [Test]
        public void ArgMaxInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => VolumeExtensions.ArgMax<int>(null));
            Assert.Throws<ArgumentException>(() => VolumeExtensions.ArgMax(new int[0]));
        }

        [Test]
        public void SliceWithMostForeground()
        {
            var foreground0 = new byte[4];
            var foreground1 = new byte[] {1, 0, 0, 0 };
            var foreground4 = new byte[] { 1, 1, 1, 1 };
            // A volume that has 1 foreground pixel in slice 0, 0 in slice 1, 4 in slice 2, 1 in slice 3
            var volume = VolumeExtensions.FromSlices(2, 2, new byte[][] { foreground1, foreground0, foreground4, foreground1 });
            var largestSlice = volume.SliceWithMostForeground(1);
            Assert.AreEqual((2,4), largestSlice);
            // When search for a foreground value that is not present at all, the first
            // slice should be returned.
            Assert.AreEqual((0,0), volume.SliceWithMostForeground(42));
        }
    }
}
