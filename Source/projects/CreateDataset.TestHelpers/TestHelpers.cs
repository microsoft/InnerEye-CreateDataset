///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using System.Collections.Generic;
using System.IO;

using MedLib.IO;

using InnerEye.CreateDataset.Math;
using InnerEye.CreateDataset.Volumes;

using NUnit.Framework;

namespace InnerEye.CreateDataset.TestHelpers
{
    public static class TestHelpers
    {

        /// <summary>
        /// Whether tests that involve writing and comparing multiple files should run to completion even after
        /// a mismatch is found (and eventually throw an AggregateException). Normally this should be false, but
        /// it can be temporarily set to true to generate a new set of files which can be copied to ExpectedTestResults
        /// (after suitable checking, of course).
        /// </summary>
        public const bool DefaultRunToCompletion = false;

        /// <summary>
        /// Creates a unique random file name in the user's temp folder.
        /// </summary>
        /// <param name="prefix">If provided, the file name itself will start with this prefix,
        /// then followed by a random part./param>
        /// <returns></returns>
        public static string RandomFileNameInTempFolder(string prefix = null)
            => Path.Combine(Path.GetTempPath(), prefix + Path.GetRandomFileName());

        /// <summary>
        /// Creates a unique file name for a compressed Nifti file in the user's temp folder.
        /// </summary>
        /// <param name="fileNamePrefix">If provided, the file name itself will start with this prefix,
        /// then followed by a random part./param>
        /// <returns></returns>
        public static string CreateTempNiftiName(NiftiCompression niftiCompression, string fileNamePrefix = null)
        {
            var prefix = RandomFileNameInTempFolder(fileNamePrefix);
            return prefix + MedIO.GetNiftiExtension(niftiCompression);
        }

        /// <summary>
        /// Creates a <see cref="Volume3D{T}"/> instance from the given array. The volume will be a single line along the
        /// X dimension, with the given voxel values. Y and Z dimension will be 1, all spacing will be 1.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="imageArray"></param>
        /// <returns></returns>
        public static Volume3D<T> SingleLineVolume<T>(this T[] imageArray)
            => new Volume3D<T>(imageArray, imageArray.Length, 1, 1, 1, 1, 1);

        /// <summary>
        /// Creates a <see cref="Volume2D{T}"/> instance from the given array. The volume will be a single line along the
        /// X dimension, with the given voxel values. Y and Z dimension will be 1, all spacing will be 1.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="imageArray"></param>
        /// <returns></returns>
        public static Volume2D<T> SingleLineVolume2D<T>(this T[] imageArray)
            => imageArray == null ? null : new Volume2D<T>(imageArray, imageArray.Length, 1, 1, 1, new Point2D(), new Matrix2());

        /// <summary>
        /// Creates a Volume3D with one slice only, dimensions 3 x 3, with the given values for the slice.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Volume3D<T> SingleSlice<T>(T[] values)
        {
            Assert.AreEqual(9, values.Length);
            return VolumeExtensions.FromSlices(3, 3, new List<T[]> { values });
        }
    }
}
