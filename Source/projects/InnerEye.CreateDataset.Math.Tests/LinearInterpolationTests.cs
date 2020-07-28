///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using System.Collections.Generic;
    using InnerEye.CreateDataset.Contours;
    using System.Drawing;
    using NUnit.Framework;

    [TestFixture]
    public class LinearInterpolationTests
    {
        [Test]
        public void SimpleSquareLinearInterpolate()
        {
            var polygon = new PointF[] { new PointF(0, 5), new PointF(5, 5), new PointF(5, 0), new PointF(0, 0) };

            var result = LinearInterpolationHelpers.LinearInterpolate(polygon, 1, polygon, 3, 2);

            AssertComparePolygons(polygon, result);

            // Flip slice indexes
            result = LinearInterpolationHelpers.LinearInterpolate(polygon, 3, polygon, 1, 2);

            AssertComparePolygons(polygon, result);
        }

        [Test]
        public void OneLargeOneSmallSquareLinearInterpolate()
        {
            var largePolygon = new PointF[] { new PointF(0, 5), new PointF(5, 5), new PointF(5, 0), new PointF(0, 0) };
            var smallPolygon = new PointF[] { new PointF(2, 3), new PointF(3, 3), new PointF(3, 2), new PointF(2, 2) };

            var expectedResult = new PointF[] { new PointF(1, 4), new PointF(4, 4), new PointF(4, 1), new PointF(1, 1) };
            
            var result = LinearInterpolationHelpers.LinearInterpolate(largePolygon, 1, smallPolygon, 3, 2);

            AssertComparePolygons(expectedResult, result);

            // Flip input polygons
            result = LinearInterpolationHelpers.LinearInterpolate(smallPolygon, 1, largePolygon, 3, 2);

            AssertComparePolygons(expectedResult, result);

            // Flip slice indexes
            result = LinearInterpolationHelpers.LinearInterpolate(smallPolygon, 3, largePolygon, 1, 2);

            AssertComparePolygons(expectedResult, result);

            // Flip slice indexes
            result = LinearInterpolationHelpers.LinearInterpolate(largePolygon, 3, smallPolygon, 1, 2);

            AssertComparePolygons(expectedResult, result);
        }

        [Test]
        public void DifferentNumberingOfPointsTriangleLinearInterpolate()
        {
            var triangle1 = new PointF[] { new PointF(1, 1), new PointF(2, 0), new PointF(0, 0) };
            var triangle2 = new PointF[] { new PointF(1, 1), new PointF(1, 1), new PointF(1, 1),
                                             new PointF(2, 0), new PointF(2, 0), new PointF(2, 0),
                                             new PointF(0, 0), new PointF(0, 0), new PointF(0, 0) };

            var expectedResult = new PointF[] { new PointF(1, 1), new PointF(2, 0), new PointF(0, 0) };

            var result = LinearInterpolationHelpers.LinearInterpolate(triangle1, 1, triangle2, 3, 2);

            AssertComparePolygons(expectedResult, result);

            // Flip input polygons
            result = LinearInterpolationHelpers.LinearInterpolate(triangle2, 1, triangle1, 3, 2);

            AssertComparePolygons(expectedResult, result);

            // Flip slice indexes
            result = LinearInterpolationHelpers.LinearInterpolate(triangle2, 3, triangle1, 1, 2);

            AssertComparePolygons(expectedResult, result);

            // Flip slice indexes
            result = LinearInterpolationHelpers.LinearInterpolate(triangle1, 3, triangle2, 1, 2);

            AssertComparePolygons(expectedResult, result);
        }

        [Test]
        public void TestMultipleContoursOnDifferentSlices()
        {
            var volume = new Volumes.Volume3D<byte>(20, 20, 20, 1, 1, 1);

            var contours = new List<ContourPolygon>()
            {
                new ContourPolygon(new PointF[] { new PointF(3, 3), new PointF(4, 2), new PointF(2, 2) }, 0),
                new ContourPolygon(new PointF[] { new PointF(6, 3), new PointF(8, 2), new PointF(6, 2) }, 0),
            };

            var tempContoursBySlice = new ContoursPerSlice(new Dictionary<int, IReadOnlyList<ContourPolygon>>()
            {
                [0] = contours,
                [2] = contours
            });

            volume.Fill<byte>(tempContoursBySlice, 1);

            var contoursBySlice = volume.ContoursWithHolesPerSlice();

            var expectedContoursBySlice = new ContoursPerSlice(new Dictionary<int, IReadOnlyList<ContourPolygon>>()
            {
                [0] = contoursBySlice.ContoursForSlice(0),
                [1] = contoursBySlice.ContoursForSlice(0),
                [2] = contoursBySlice.ContoursForSlice(0)
            });

            var result = LinearInterpolationHelpers.LinearInterpolate(volume, contoursBySlice);

            AssertCompareContoursBySlice(expectedContoursBySlice, result);
        }

        private void AssertCompareContoursBySlice(ContoursPerSlice expected, ContoursPerSlice actual)
        {
            var expectedSlices = expected.GetSlicesWithContours();
            var actualSlices = actual.GetSlicesWithContours();

            Assert.AreEqual(expectedSlices.Count, actualSlices.Count);

            for (var i = 0; i < expectedSlices.Count; i++)
            {
                Assert.AreEqual(expectedSlices[i], actualSlices[i]);
                AssertCompareContours(expected.ContoursForSlice(expectedSlices[i]), actual.ContoursForSlice(actualSlices[i]));
            }
        }

        private void AssertCompareContours(IReadOnlyList<ContourPolygon> expected, IReadOnlyList<ContourPolygon> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count, "Number of contours");

            for (var i = 0; i < expected.Count; i++)
            {
                AssertComparePolygons(expected[i].ContourPoints, actual[i].ContourPoints);
            }
        }

        private void AssertComparePolygons(IReadOnlyList<PointF> expected, IReadOnlyList<PointF> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count, "Number of points");

            for (var i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
