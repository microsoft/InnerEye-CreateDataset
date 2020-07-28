///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.TestHelpers
{
    using System;
    using InnerEye.CreateDataset.Common;
    using InnerEye.CreateDataset.Data;
    using InnerEye.CreateDataset.Volumes;
    using MoreLinq;
    using NUnit.Framework;

    public class VolumeAssert
    {
        /// <summary>
        /// Compares two instances of Volume3D, and throws an exception if they have different size or spacing.
        /// </summary>
        /// <param name="expected">The expected volume.</param>
        /// <param name="actual">The actual volume.</param>
        /// <param name="loggingMessage">A string prefix for the messages created in Asserts (usually a file name pointing to the source
        /// of the discrepancy)</param>
        public static void AssertVolumeSizeAndSpacingMatches<T>(Volume3D<T> expected, Volume3D<T> actual, string loggingPrefix = "") where T : IEquatable<T>
        {
            Assert.AreEqual(expected.DimX, actual.DimX, $"{loggingPrefix}: X dimension must match");
            Assert.AreEqual(expected.DimY, actual.DimY, $"{loggingPrefix}: Y dimension must match");
            Assert.AreEqual(expected.DimZ, actual.DimZ, $"{loggingPrefix}: Z dimension must match");
            Assert.AreEqual(expected.SpacingX, actual.SpacingX, 1e-5, $"{loggingPrefix}: X spacing must match");
            Assert.AreEqual(expected.SpacingY, actual.SpacingY, 1e-5, $"{loggingPrefix}: Y spacing must match");
            Assert.AreEqual(expected.SpacingZ, actual.SpacingZ, 1e-5, $"{loggingPrefix}: Z spacing must match");
            var expectedProperties = Volume3DProperties.Create(expected);
            var actualProperties = Volume3DProperties.Create(actual);
            Assert.IsTrue(expectedProperties.IsApproximatelyEqual(actualProperties), "Volume properties should match up to small numeric differences.");
            Assert.AreEqual(expected.Array.Length, actual.Array.Length, $"{loggingPrefix}: The volumes have a different size");
        }

        /// <summary>
        /// Compares two instances of Volume3D, and throws an exception if they don't match exactly. If they don't match,
        /// the first 20 differences are printed out on the console, as well as statistics about the distinct values contained 
        /// in the two volumes.
        /// </summary>
        /// <param name="expected">The expected volume.</param>
        /// <param name="actual">The actual volume.</param>
        /// <param name="loggingMessage">A string prefix for the messages created in Asserts (usually a file name pointing to the source</param>
        /// <param name="logHistogramOnMatch">Whether to print logs if volumes match</param>
        /// of the discrepancy)</param>
        public static void AssertVolumesMatch<T>(
            Volume3D<T> expected,
            Volume3D<T> actual,
            string loggingPrefix = "" ) where T : IEquatable<T>
        {
            AssertVolumeSizeAndSpacingMatches(expected, actual, loggingPrefix);
            var numDifferences = 0;
            var maxDifferences = 20;
            for (var index = 0; index < expected.Array.Length; index++)
            {
                var e = expected[index];
                var a = actual[index];
                if (!e.Equals(a) && numDifferences < maxDifferences)
                {
                    numDifferences++;
                    Console.WriteLine($"Difference at index {index}: Expected {e}, actual {a}");
                    if (numDifferences >= maxDifferences)
                    {
                        Console.WriteLine($"Stopping at {maxDifferences} differences");
                    }
                }
            }
            if (numDifferences > 0)
            {
                Assert.Fail($"{loggingPrefix}: Volumes are different. Console has detailed diff.");
            }
        }
    }
}
