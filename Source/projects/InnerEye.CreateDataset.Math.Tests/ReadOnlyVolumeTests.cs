///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using InnerEye.CreateDataset.Volumes;

using NUnit.Framework;

using System;

namespace InnerEye.CreateDataset.Math.Tests
{
    [TestFixture]
    public class ReadOnlyVolumeTests
    {
        [Test]
        public void Test3D()
        {
            var readonlyVolume3d = new ReadOnlyVolume3D<short>(new Volume3D<short>(1, 1, 1));

            Assert.Throws<InvalidOperationException>(() => readonlyVolume3d[0] = 0);
        }

        [Test]
        public void Test2D()
        {
            var readonlyVolume2d = new ReadOnlyVolume2D<short>(new Volume2D<short>(1, 1, 1, 1, new Point2D(0, 0), Matrix2.CreateIdentity()));

            Assert.Throws<InvalidOperationException>(() => readonlyVolume2d[0] = 0);
        }
    }
}
