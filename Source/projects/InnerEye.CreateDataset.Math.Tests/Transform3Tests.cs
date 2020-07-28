///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using InnerEye.CreateDataset.Volumes;

    using NUnit.Framework;

    [TestFixture]
    public class Transform3Tests
    {
        [Test]
        public void TestTransform3()
        {
            var transform = new Transform3(
                new Matrix3(new double[]
                {
                    5, 0, 0,
                    0, 5, 0,
                    0, 0, 5
                }), 
                new Point3D(6, 7, 8));

            var result1 = transform.Transform(new Point3D(6, 7, 8));
            var result2 = transform * new Point3D(6, 7, 8);

            Assert.AreEqual(36, result1.X);
            Assert.AreEqual(42, result1.Y);
            Assert.AreEqual(48, result1.Z);

            Assert.AreEqual(result1.X, result2.X);
            Assert.AreEqual(result1.Y, result2.Y);
            Assert.AreEqual(result1.Z, result2.Z);

            var inverseTransform = transform.Inverse();
            result1 = inverseTransform.Transform(new Point3D(200, 200, 200));

            Assert.AreEqual(38.8, result1.X);
            Assert.AreEqual(38.6, result1.Y);
            Assert.AreEqual(38.4, result1.Z);

            transform = inverseTransform.Inverse();
            result1 = transform.Transform(new Point3D(6, 7, 8));

            Assert.AreEqual(36, result1.X);
            Assert.AreEqual(42, result1.Y);
            Assert.AreEqual(48, result1.Z);
        }
    }
}