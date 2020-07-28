///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using InnerEye.CreateDataset.Volumes;
    using InnerEye.CreateDataset.Contours;
    using System.Drawing;
    using NUnit.Framework;

    [TestFixture]
    public class ContourExtensionsTests
    {
        [Description("Tests getting the region from a collection of contours on a slice returns the correct result.")]
        [Test]
        public void GetRegionContoursTest()
        {
            var contours = new List<ContourPolygon>()
            {
                new ContourPolygon(new PointF[]
                {
                    new PointF(5, 10),
                    new PointF(12, 45),
                    new PointF(87, 2),
                    new PointF(234, 5)
                },
                0),
                new ContourPolygon(new PointF[]
                {
                    new PointF(5, 10),
                    new PointF(12, 45),
                    new PointF(1, 23),
                    new PointF(12, 44),
                    new PointF(15, 48),
                },
                0),
                new ContourPolygon(new PointF[]
                {
                    new PointF(5, 10),
                    new PointF(12, 45),
                },
                0)
            };

            var region = contours.GetRegion();

            Assert.AreEqual(1, region.MinimumX);
            Assert.AreEqual(2, region.MinimumY);
            Assert.AreEqual(234, region.MaximumX);
            Assert.AreEqual(48, region.MaximumY);

            Assert.Throws<ArgumentNullException>(() => new List<ContourPolygon>().GetRegion());
        }

        [Description("Tests that getting min/ max slices returns the correct result.")]
        [Test]
        public void GetMinMaxSlicesTest()
        {
            var contoursBySlice = new Contours.ContoursPerSlice(new Dictionary<int, IReadOnlyList<ContourPolygon>>()
            {
                { 5, new List<ContourPolygon>() },
                { 7, new List<ContourPolygon>() },
                { 10, new List<ContourPolygon>() },
                { 15, new List<ContourPolygon>() },
                { 6, new List<ContourPolygon>() },
                { 8, new List<ContourPolygon>() },
                { 12, new List<ContourPolygon>() },
                { 90, new List<ContourPolygon>() },
            });

            var minMax = contoursBySlice.GetMinMaxSlices();

            Assert.AreEqual(5, minMax.Min);
            Assert.AreEqual(90, minMax.Max);
        }

        [Description("Tests that getting min/ max intensities of a volume3d returns correct values")]
        [Test]
        public void GetMinMaxOfVolume3D()
        {
            var array = Enumerable.Range(0, 100).Select(x => (short)x).ToArray();
            var volume = new Volume3D<short>(array, array.Length, 1, 1, 1, 1, 1);

            var minMax = volume.GetMinMax();

            Assert.AreEqual(0, minMax.Minimum);
            Assert.AreEqual(99, minMax.Maximum);
        }
    }
}
