///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using System.Collections.Generic;

    using InnerEye.CreateDataset.Volumes;

    using NUnit.Framework;

    [TestFixture]
    public class Point3DTests
    {
        private static IEnumerable<Point3D> Set3Dimensions(double value)
        {
            yield return new Point3D(value, 0, 0);
            yield return new Point3D(0, value, 0);
            yield return new Point3D(0, 0, value);
        }

        [Test]
        public void Point3DIsInfinity()
        {
            var zero = Point3D.Zero();
            Assert.IsFalse(zero.IsInfinity());
            Assert.IsFalse(zero.IsNaN());
            Assert.IsTrue(zero.IsValid());
            foreach (var infinity in new[] { double.PositiveInfinity, double.NegativeInfinity })
            {
                foreach (var point in Set3Dimensions(infinity))
                {
                    Assert.IsTrue(point.IsInfinity());
                    Assert.IsFalse(point.IsNaN());
                    Assert.IsFalse(point.IsValid());
                }
            }
        }

        [Test]
        public void Point3DIsNaN()
        {
            foreach (var point in Set3Dimensions(double.NaN))
            {
                Assert.IsTrue(point.IsNaN());
                Assert.IsFalse(point.IsInfinity());
                Assert.IsFalse(point.IsValid());
            }
        }
    }
}
