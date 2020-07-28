///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using System;
    using InnerEye.CreateDataset.TestHelpers;
    using InnerEye.CreateDataset.Volumes;
    using InnerEye.CreateDataset.Math;
    using NUnit.Framework;

    [TestFixture]
    public class CommonExtensionsTests
    {
        [Test]
        public void CommonExtensionMinMaxInvalid()
        {
            Volume3D<byte> volume = null;
            short[] array = null;
            Assert.Throws<ArgumentNullException>(() => volume.GetMinMax());
            Assert.Throws<ArgumentNullException>(() => array.GetMinMax());
            Assert.Throws<ArgumentNullException>(() => array.Minimum());
            Assert.Throws<ArgumentNullException>(() => array.Maximum());
            var empty = new short[0];
            Assert.Throws<ArgumentException>(() => empty.GetMinMax());
            Assert.Throws<ArgumentException>(() => empty.Minimum());
            Assert.Throws<ArgumentException>(() => empty.Maximum());
        }

        [Test]
        [TestCase(new byte[] { 1 }, (byte)1, (byte)1)]
        [TestCase(new byte[] { 10, 2, 5, 5}, (byte)2, (byte)10)]
        [TestCase(new byte[] { 10, 2, 20, 1}, (byte)1, (byte)20)]
        public void CommonExtensionMinMax(byte[] values, byte expectedMin, byte expectedMax)
        {
            var min = values.Minimum();
            var max = values.Maximum();
            var minMax = values.GetMinMax();
            Assert.AreEqual(expectedMin, min, "Minimum");
            Assert.AreEqual(expectedMax, max, "Maximum");
            Assert.AreEqual(expectedMin, minMax.Minimum, "MinMax.Minimum");
            Assert.AreEqual(expectedMax, minMax.Maximum, "MinMax.Maximum");
        }

        [Test]
        public void CommonExtensionsEmptyRegion()
        {
            var region = RegionExtensions.EmptyIntRegion();
            Assert.IsTrue(region.IsEmpty());
            Assert.AreEqual(0, region.MinimumX);
            Assert.AreEqual(-1, region.MaximumX);
            Assert.AreEqual(0, region.Size());
            Assert.AreEqual(0, region.LengthX());
            Assert.AreEqual(0, region.LengthY());
            Assert.AreEqual(0, region.LengthY());
            Assert.IsFalse(region.ContainsPoint(0, 0, 0));
        }

        [Test]
        public void CommonExtensionsRegionLength()
        {
            var minX = 10;
            var maxX = 11;
            var minY = 20;
            var maxY = 22;
            var minZ = 30;
            var maxZ = 33;
            var region = new Region3D<int>(minX, minY, minZ, maxX, maxY, maxZ);
            Assert.IsFalse(region.IsEmpty());
            Assert.AreEqual(2, region.LengthX());
            Assert.AreEqual(3, region.LengthY());
            Assert.AreEqual(4, region.LengthZ());
            Assert.AreEqual(2 * 3 * 4, region.Size());
            Assert.IsFalse(region.ContainsPoint(minX - 1, minY, minZ));
            Assert.IsFalse(region.ContainsPoint(maxX + 1, minY, minZ));
            Assert.IsTrue(region.ContainsPoint(minX, minY, minZ));
            Assert.IsTrue(region.ContainsPoint(maxX, minY, minZ));
            Assert.IsFalse(region.ContainsPoint(minX, minY - 1, minZ));
            Assert.IsFalse(region.ContainsPoint(minX, maxY + 1, minZ));
            Assert.IsTrue(region.ContainsPoint(minX, minY, minZ));
            Assert.IsTrue(region.ContainsPoint(minX, maxY, minZ));
            Assert.IsFalse(region.ContainsPoint(minX, minY, minZ - 1));
            Assert.IsFalse(region.ContainsPoint(minX, minY, maxZ + 1));
            Assert.IsTrue(region.ContainsPoint(minX, minY, minZ));
            Assert.IsTrue(region.ContainsPoint(minX, minY, maxZ));
        }

        [Test]
        public void CommonExtensionsRegionInsideOf()
        {
            var minX = 11;
            var maxX = 12;
            var minY = 21;
            var maxY = 23;
            var minZ = 31;
            var maxZ = 34;
            var region = new Region3D<int>(minX, minY, minZ, maxX, maxY, maxZ);
            Assert.IsTrue(region.InsideOf(region), "Regions are equal");
            var outer1 = new Region3D<int>(minX - 1, minY -1, minZ - 1, maxX + 1, maxY + 1, maxZ + 1);
            Assert.IsTrue(region.InsideOf(outer1), "Outer region is larger by a margin of 1");
            var outer2 = new Region3D<int>(0, minY, minZ, 0, maxY, maxZ);
            Assert.IsFalse(region.InsideOf(outer2), "Outer region does not enclose in X dimension");
            var outer3 = new Region3D<int>(minX, 0, minZ, maxX, 0, maxZ);
            Assert.IsFalse(region.InsideOf(outer3), "Outer region does not enclose in Y dimension");
            var outer4 = new Region3D<int>(minX, minY, 0, maxX, maxY, 0);
            Assert.IsFalse(region.InsideOf(outer4), "Outer region does not enclose in Z dimension");
            var empty = RegionExtensions.EmptyIntRegion();
            Assert.Throws<ArgumentException>(() => region.InsideOf(empty));
            Assert.Throws<ArgumentException>(() => empty.InsideOf(region));
        }

        [Test]
        public void CommonExtensionsGetInterestRegion1()
        {
            var volume0 = new Volume3D<byte>(3, 3, 3);
            var region0 = volume0.GetInterestRegion();
            Assert.IsTrue(region0.IsEmpty(), "When there are no non-zero values, the region should be empty");
            // Regions define an equality operator that we can use here
            Assert.AreEqual(RegionExtensions.EmptyIntRegion(), region0, "When no foreground is present, should return the special EmptyRegion");
            Assert.Throws<ArgumentException>(() => volume0.Crop(region0), "Cropping with an empty region should throw an exception");
        }

        [Test]
        public void CommonExtensionsGetInterestRegion2()
        {
            var volume0 = new Volume3D<byte>(2, 3, 4);
            var volume1 = volume0.CreateSameSize<byte>(1);
            var fullRegion = volume1.GetFullRegion();
            var region1 = volume1.GetInterestRegion();
            Assert.AreEqual(fullRegion, region1, "When all voxels have non-zero values, the region should cover the full image.");
            // Find values that are larger or equal than 1: Again, this should be all voxels.
            var region2 = volume1.GetInterestRegion(1);
            Assert.AreEqual(fullRegion, region2, "All voxels have values >= 1, the region should cover the full image.");
            var cropped = volume1.Crop(region2);
            VolumeAssert.AssertVolumesMatch(volume1, cropped, "Cropping with the full region should return the input values.");
            // Find values that are larger or equal than 2: Should get empty region.
            var region3 = volume1.GetInterestRegion(2);
            Assert.IsTrue(region3.IsEmpty(), "No voxels have values >= 2, the region should be empty.");
        }

        [Test]
        public void CommonExtensionsCreateArray()
        {
            var value = 2;
            Assert.Throws<ArgumentException>(() => GenericExtensions.CreateArray(-1, 1));
            CollectionAssert.AreEqual(new int[0], GenericExtensions.CreateArray(0, 1));
            CollectionAssert.AreEqual(new int[1] { value }, GenericExtensions.CreateArray(1, value));
            var length = 3;
            var array = new int[length];
            array.Fill(value);
            CollectionAssert.AreEqual(new int[] { value, value, value }, array);
            var destination = new int[length];
            array.CopyTo(destination);
            CollectionAssert.AreEqual(array, destination);
            Assert.Throws<ArgumentNullException>(() => array.CopyTo(null));
            var wrongLength = new int[2 * length];
            Assert.Throws<ArgumentException>(() => array.CopyTo(wrongLength));
        }

        [Test]
        public void CommonExtensionsClipVolume()
        {
            var values = new short[] { -5, -4, 1, 2, 5 };
            var image = new Volume3D<short>(values, values.Length, 1, 1, 1, 1, 1);
            var range = MinMax.Create<short>(-4, 3);
            image.ClipToRangeInPlace(range);
            // Expected values: Everything that is -4 or lower should be set to -4, that's the first
            // two voxels. Everything larger than 3 should be set to 3 - that's only the last voxels.
            // Everything in the middle is unchanged.
            var expected = new short[] { -4, -4, 1, 2, 3 };
            CollectionAssert.AreEqual(expected, image.Array);
            var invalidRange = MinMax.Create<short>(4, 3);
            Assert.Throws<ArgumentException>(() => image.ClipToRangeInPlace(invalidRange));
        }

        /// <summary>
        /// Test the conversion from a floating point image to a byte image.
        /// The full range should be mapped to [0, 255].
        /// </summary>
        /// <param name="imageArray"></param>
        /// <param name="expected"></param>
        [Test]
        // Middle value 0 would be mapped to 127.5. Converter rounds to nearest integer, gives 128.
        [TestCase(new float[] { -10, 0, 10 }, new byte[] { 0, 128, 255 })]
        [TestCase(new float[] { 10, 10 }, new byte[] { 0, 0 })]
        public void CommonExtensionsScale(float[] imageArray, byte[] expected)
        {
            var image = imageArray.SingleLineVolume();
            var result = image.ScaleToByteRange();
            Assert.AreEqual(expected, result.Array);
        }
 
        /// <summary>
        /// Test thresholding of a volume.
        /// </summary>
        /// <param name="imageArray"></param>
        /// <param name="expected"></param>
        [Test]
        [TestCase(new short[] { -10, -1, 0, 10 }, -5, new byte[] { 0, 1, 1, 1 })]
        [TestCase(new short[] { 0, 1, 2, 3 }, 2, new byte[] { 0, 0, 1, 1 })]
        [TestCase(new short[] { 0, 1 }, -2, new byte[] { 1, 1 })]
        [TestCase(new short[] { 0, 1 }, 5, new byte[] { 0, 0 })]
        public void CommonExtensionsThreshold(short[] imageArray, short threshold, byte[] expected)
        {
            var image = imageArray.SingleLineVolume();
            var result = image.Threshold(threshold);
            Assert.AreEqual(expected, result.Array);
        }

        /// <summary>
        /// Test scaling a volume via clamping to byte range, when range is given explicitly.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="expected"></param>
        // Set scale to 0..2: All values that are outside of that should be mapped to 0/255
        [TestCase(new double[] { -10, 0, 1, 2, 100}, 0, 2, new byte[] { 0, 0, 128, 255, 255})]
        // Input is already in byte range: no change
        [TestCase(new double[] { 0, 1, 255}, 0, 255, new byte[] { 0, 1, 255})]
        // If range is invalid, return all zeros.
        [TestCase(new double[] { 0, 1, 255}, 0, -1, new byte[] { 0, 0, 0})]
        public void CommonExtensionsScaleToByteRange(double[] values, double min, double max, byte[] expected)
        {
            var minMax = MinMax.Create(min, max);
            var volume = values.SingleLineVolume();
            var actual = volume.ScaleToByteRange(minMax);
            Assert.AreEqual(expected, actual.Array);
        }

        /// <summary>
        /// Test scaling a volume via clamping to byte range, using full input range.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="expected"></param>
        [TestCase(new double[] { 0, 1, 255 }, new byte[] { 0, 1, 255 })]
        [TestCase(new double[] { 0, 1, 2 }, new byte[] { 0, 128, 255 })]
        public void CommonExtensionsScaleToByteRangeFull(double[] values, byte[] expected)
        {
            var volume = values.SingleLineVolume();
            var actual = volume.ScaleToByteRange();
            Assert.AreEqual(expected, actual.Array);
        }

        [Test]
        public void CommonExtensionsMinMaxRange()
        {
            var minMaxInvalid = MinMax.Create(10, 5);
            Assert.Throws<ArgumentException>(() => minMaxInvalid.Range());
            var minMax = MinMax.Create(5, 100);
            Assert.AreEqual(minMax.Maximum - minMax.Minimum, minMax.Range());
            Assert.AreEqual(minMax.Minimum, minMax.Clamp(minMax.Minimum - 1));
            Assert.AreEqual(50, minMax.Clamp(50));
            Assert.AreEqual(minMax.Maximum, minMax.Clamp(minMax.Maximum + 1));
        }
    }
}
