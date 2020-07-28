///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using InnerEye.CreateDataset.Volumes;

using NUnit.Framework;

namespace InnerEye.CreateDataset.Math.Tests
{
    [TestFixture]
    public class Region3DTests
    {
        [Test]
        public void EqualityCheck()
        {
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1),new Region3D<int>(1, 1, 1, 0, 0, 0));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 1, 0, 1, 1, 1));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 0, 1, 1, 1, 1));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 1, 1, 1, 1, 1));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(1, 0, 1, 1, 1, 1));
            

            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 0, 0, 0, 1, 0));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 1, 0, 0, 0, 1));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 0, 1, 1, 1, 1));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 1, 1, 0, 1, 1));
            Assert.AreNotEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(1, 0, 1, 1, 0, 1));
            Assert.AreEqual(new Region3D<int>(0, 0, 0, 1, 1, 1), new Region3D<int>(0, 0, 0, 1, 1, 1));
        }
    }
}
