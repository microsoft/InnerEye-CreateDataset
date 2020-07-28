///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests
{
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;
    using NUnit.Framework;

    [TestFixture]
    public class GeometryComparisonsTests
    {
        [Test]
        public void VolumeDirectionApproximateEquality()
        {
            var direction1 = new Matrix3(
                new double[]
                {
                    0.99956005811691284,
                    -0.00059527973497270628,
                    -0.02965429337083975,
                    -0.00059527973497270628,
                    0.99919456243515015,
                    -0.040122962957073682,
                    0.02965429337083975,
                    0.040122962957073682,
                    0.998754620552063,
                }
                );
            // direction1a is identical to direction1
            var direction1a = new Matrix3(direction1);
            // direction1b is only numerically different from direction1
            var direction1b = new Matrix3(
                new double[]
                {
                    0.99956005811691284,
                    -0.00059527966024747712,
                    -0.029654289648348053,
                    -0.00059527966024747712,
                    0.99919456243515015,
                    -0.04012296295929059,
                    0.029654289648348053,
                    0.04012296295929059,
                    0.998754620552063,
                });
            // direction2 is significantly different from direction1
            var direction2 = new Matrix3(
                new double[]
                {
                    0.99956005811691284,
                    -0.00059527966024747712,
                    -0.029654289648348053,
                    -0.00059527966024747712,
                    0.99919456243515015,
                    -0.14012296295929059,
                    0.029654289648348053,
                    0.04012296295929059,
                    0.998754620552063,
                });
            var origin = new Point3D(-126.98542022705078, -124.45243835449219, -64.447196960449219);
            var volume1 = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin,
                direction1);
            var volume1a = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin,
                direction1a);
            var volume1b = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin,
                direction1b);
            var volume2 = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin,
                direction2);
            Assert.IsTrue(
                GeometryComparisons.AreDirectionsApproximatelyEqual(volume1, volume1),
                "Volume directions reported significantly different when comparing a volume to itself.");
            Assert.IsTrue(
                GeometryComparisons.AreDirectionsApproximatelyEqual(volume1, volume1a),
                "Volume directions reported significantly different when comparing volumes with identical directions.");
            Assert.IsTrue(
                GeometryComparisons.AreDirectionsApproximatelyEqual(volume1, volume1b),
                "Volume directions reported significantly different when comparing volumes with directions identical up to numerics.");
            Assert.IsFalse(
                GeometryComparisons.AreDirectionsApproximatelyEqual(volume1, volume2),
                "Volume directions reported nearly identical when comparing volumes with significantly different directions.");
        }

        [Test]
        public void VolumeSpacingsApproximateEquality()
        {
            var direction = new Matrix3(
                new double[]
                {
                    0.99956005811691284,
                    -0.00059527973497270628,
                    -0.02965429337083975,
                    -0.00059527973497270628,
                    0.99919456243515015,
                    -0.040122962957073682,
                    0.02965429337083975,
                    0.040122962957073682,
                    0.998754620552063,
                }
                );
            var origin = new Point3D(-126.98542022705078, -124.45243835449219, -64.447196960449219);
            var volume1 = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin,
                direction);
            // volume1a has spacings identical to those of volume1
            var volume1a = new Volume3D<int>(
                124, 124, 88,
                2.01, 2.01, 2.01,
                origin,
                direction);
            // volume1b has spacings only numerically different from those of volume1
            var volume1b = new Volume3D<int>(
                124, 124, 88,
                2.01, 2.01, 2.01002,
                origin,
                direction);
            // volume2 has spacings significantly different to those of volume
            var volume2 = new Volume3D<int>(
                124, 124, 88,
                2.01, 2.01, 2.1,
                origin,
                direction);
            Assert.IsTrue(
                GeometryComparisons.AreSpacingsApproximatelyEqual(volume1, volume1),
                "Volume spacings reported significantly different when comparing a volume to itself.");
            Assert.IsTrue(
                GeometryComparisons.AreSpacingsApproximatelyEqual(volume1, volume1a),
                "Volume spacings reported significantly different when comparing volumes with identical spacings.");
            Assert.IsTrue(
                GeometryComparisons.AreSpacingsApproximatelyEqual(volume1, volume1b),
                "Volume spacings reported significantly different when comparing volumes with spacings identical up to numerics.");
            Assert.IsFalse(
                GeometryComparisons.AreSpacingsApproximatelyEqual(volume1, volume2),
                "Volume spacings reported nearly identical when comparing volumes with significantly different spacings.");
        }

        [Test]
        public void VolumeOriginApproximateEquality()
        {
            var origin1 = new Point3D(-126.98542022705078, -124.45243835449219, -64.447196960449219);
            // origin1a is identical to origin1
            var origin1a = new Point3D(origin1.Data);
            // origin1b is only numerically different from origin1
            var origin1b = new Point3D(-126.985420227050, -124.452438354493, -64.447196);
            // origin2 is significantly different from origin1
            var origin2 = new Point3D(-126.98542022705078, -124.45243835449219, -64.547196960449219);
            var direction = new Matrix3(
                new double[]
                {
                    0.99956005811691284,
                    -0.00059527973497270628,
                    -0.02965429337083975,
                    -0.00059527973497270628,
                    0.99919456243515015,
                    -0.040122962957073682,
                    0.02965429337083975,
                    0.040122962957073682,
                    0.998754620552063,
                }
                );
            var volume1 = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin1,
                direction);
            var volume1a = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin1a,
                direction);
            var volume1b = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin1b,
                direction);
            var volume2 = new Volume3D<int>(
                125, 125, 88,
                2.01, 2.01, 2.01,
                origin2,
                direction);
            Assert.IsTrue(
                GeometryComparisons.AreOriginsApproximatelyEqual(volume1, volume1),
                "Volume origins reported significantly different when comparing a volume to itself.");
            Assert.IsTrue(
                GeometryComparisons.AreOriginsApproximatelyEqual(volume1, volume1a),
                "Volume origins reported significantly different when comparing volumes with identical origins.");
            Assert.IsTrue(
                GeometryComparisons.AreOriginsApproximatelyEqual(volume1, volume1b),
                "Volume origins reported significantly different when comparing volumes with origins identical up to numerics.");
            Assert.IsFalse(
                GeometryComparisons.AreOriginsApproximatelyEqual(volume1, volume2),
                "Volume origins reported nearly identical when comparing volumes with significantly different origins.");
        }

    }
}
