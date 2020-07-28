///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using InnerEye.CreateDataset.Contours;
    using PointInt = System.Drawing.Point;
    using Point = System.Drawing.PointF;
    using NUnit.Framework;

    [TestFixture]
    public class ContourSimplifierTests
    {
        ///<summary>
        /// Check that a very simple contour (one pixel square) produces expected result.
        ///</summary>
        [Test]
        public void SimplifyContour()
        {
            var result1 = ContourSimplifier.Simplify(new PointInt(100, 100), new PointInt(0, 1), "LLLL");
            var result2 = ContourSimplifier.RemoveRedundantPoints(result1);

            var expected = new Point[] { new Point(100, 99.5f), new Point(99.5f, 100), new Point(99, 99.5f), new Point(99.5f, 99) };
            Assert.AreEqual(true, Equals(result2, expected));
        }

        ///<summary>
        /// Check that a very simple contour (one pixel square) produces expected result.
        ///</summary>
        [Test]
        public void RemoveRedundantPoints()
        {
            {
                // Remove some conincident points
                var contour = new Point[] {
                    new Point(100, 99.5f),
                    new Point(99.5f, 100),
                    new Point(99, 99.5f),
                    new Point(99, 99.5f), // dupe predecessor
                    new Point(99.5f, 99),
                    new Point(100, 99.5f) }; // last dupes first
                var result = ContourSimplifier.RemoveRedundantPoints(contour);
                var expected = new Point[] { new Point(100, 99.5f), new Point(99.5f, 100), new Point(99, 99.5f), new Point(99.5f, 99) };
                Assert.AreEqual(true, Equals(result, expected));
            }

            {
                // Remove some colinear points
                var contour = new Point[] {
                    new Point(100, 99.5f),
                    new Point(99.5f, 100),
                    new Point(99.25f, 99.75f),
                    new Point(99, 99.5f),
                    new Point(99.5f, 99) };
                var result = ContourSimplifier.RemoveRedundantPoints(contour);
                var expected = new Point[] { new Point(100, 99.5f), new Point(99.5f, 100), new Point(99, 99.5f), new Point(99.5f, 99) };
                Assert.AreEqual(true, Equals(result, expected));
            }

            {
                var polygon = new[] { new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(2, 2), new Point(0, 1) };

                var result = ContourSimplifier.RemoveRedundantPoints(polygon);
                var expected = new[] { new Point(0, 0), new Point(2, 0), new Point(2, 2), new Point(0, 1) };

                for (var i = 0; i < expected.Length; i++)
                {
                    Assert.AreEqual(expected[i], result[i]);
                }

                result = ContourSimplifier.RemoveRedundantPoints(result);
                Assert.AreEqual(true, Equals(result, expected));

                polygon = new[] { new Point(0, 0), new Point(0, 0), new Point(1, 0), new Point(2, 0), new Point(2, 2), new Point(0, 1) };

                result = ContourSimplifier.RemoveRedundantPoints(result);
                Assert.AreEqual(true, Equals(result, expected));
            }
        }

        private bool Equals(Point[] left, Point[] right)
        {
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (System.Math.Abs(left[i].X - right[i].X) > 0.01 || System.Math.Abs(left[i].Y - right[i].Y) > 0.01)
                    return false;
            }

            return true;
        }
    }
}