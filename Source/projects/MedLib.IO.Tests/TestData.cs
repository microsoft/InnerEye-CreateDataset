///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System.IO;
    using NUnit.Framework;

    /// <summary>
    /// Contains helper functions to get access to the test data.
    /// </summary>
    public static class TestData
    {
        /// <summary>
        /// Given a relative path inside of the Images submodule, create the full path to that file.
        /// This assumes that the test assembly is run in the location where the build places it,
        /// and that the full source tree is available.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static string GetFullImagesPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @".\TestData", relativePath));
        }

    }
}
