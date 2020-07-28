///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using InnerEye.CreateDataset.Math.Morphology;
using InnerEye.CreateDataset.Volumes;

using NUnit.Framework;

namespace InnerEye.CreateDataset.Math.Tests.Morphology
{
    [TestFixture]
    public class StructuringElementTests
    {
        ///<summary>
        /// Test to capture encoding of the structuring element
        ///</summary>
        [Test]
        public void StructuringElementEncodingTest()
        {
            var result = new StructuringElement(1, 1, 0).Mask;
            var expected = new Volume3D<byte>(3, 3, 1, 1, 1, 1);
            expected[1, 0, 0] = 1;
            expected[0, 1, 0] = 1;
            expected[1, 1, 0] = 1;
            expected[2, 1, 0] = 1;
            expected[1, 2, 0] = 1;
            CollectionAssert.AreEqual(expected.Array, result.Array);
        }

        ///<summary>
        /// Test to capture encoding of the structuring element with zero margin
        /// (which is not possible for dilation but can be possible for erosion).
        /// The expected behaviour is to generate a mask with only the center voxel
        /// in the foreground.
        ///</summary>
        [Test]
        public void StructuringElementEncodingZeroMarginTest()
        {
            var result = new StructuringElement(0, 0, 0).Mask;
            var expected = new Volume3D<byte>(1, 1, 1, 1, 1, 1);
            expected[0, 0, 0] = 1;
            CollectionAssert.AreEqual(expected.Array, result.Array);
        }
    }
}
