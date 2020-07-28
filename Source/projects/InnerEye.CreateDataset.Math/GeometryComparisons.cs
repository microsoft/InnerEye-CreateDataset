///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System;
    using System.Linq;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Holds methods for comparing two geometric objects of classes in InnerEye.CreateDataset.Volumes dll.
    /// </summary>
    public static class GeometryComparisons
    {
        /// <summary>
        /// Returns true iff the spacings used by the two volumes equal up to numeric errors?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool AreSpacingsApproximatelyEqual<T>(Volume3D<T> left, Volume3D<T> right)
        {
            // The same logic is implemented in InnerEye.CreateDataset.Data:Tuple3D:
            // HasSmallRelativeDifference
            var leftSpacing = new[] { left.SpacingX, left.SpacingY, left.SpacingZ };
            var rightSpacing = new[] { right.SpacingX, right.SpacingY, right.SpacingZ };
            var diff = GetRelativeDifference(leftSpacing, rightSpacing);
            return diff <= MaximumRelativeDifferenceForSpacing;
        }

        /// <summary>
        /// Returns true iff the spacings used by the two volumes equal up to numeric errors?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool AreOriginsApproximatelyEqual<T>(Volume3D<T> left, Volume3D<T> right)
        {
            // The same logic is implemented in InnerEye.CreateDataset.Data:Tuple3D:
            // HasSmallAbsoluteDifference
            var diff = GetLInfNorm(left.Origin.Data, right.Origin.Data);
            return diff <= MaximumAbsoluteDifferenceForOrigin;
        }

        /// <summary>
        /// Returns true iff the spacings used by the two volumes equal up to numeric errors?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool AreDirectionsApproximatelyEqual<T>(Volume3D<T> left, Volume3D<T> right)
        {
            // The same logic is implemented in InnerEye.CreateDataset.Data:Direction3D:
            // HasSmallAbsoluteDifference
            var diff = GetLInfNorm(left.Direction.Data, right.Direction.Data);
            return diff <= MaximumAbsoluteDifferenceForDirection;
        }

        /// <summary>
        /// Returns the maximal relative difference of other w.r.t. basis, elementwise.
        /// The relative difference is defined as the absolute value for the fractional change
        /// from basis to other.  Since the relative change is ill-defined when basis is 0,
        /// this case is special-treated - the absolute of the difference (which is equal to 
        /// other value) is used.
        /// </summary>
        /// <param name="basis">Should not be empty, and should have the same number of elements as other.</param>
        /// <param name="other">Should not be empty, and should have the same number of elements as basis.</param>
        /// <returns></returns>
        private static double GetRelativeDifference(double[] basis, double[] other)
        {
            double relativeChange(double basisElement, double otherElement)
            {
                if (basisElement == 0.0)
                {
                    return Math.Abs(otherElement);
                }
                else
                {
                    return Math.Abs(otherElement / basisElement - 1.0);
                }
            }
            return
                Enumerable.Zip(basis, other, relativeChange)
                .Max();
        }

        /// <summary>
        /// Returns the L\infty-norm of the difference between two vectors, which is
        /// equivalent to the maximum absolute difference between the elements of the two
        /// vectors.
        /// </summary>
        /// <param name="left">Should not be empty, and should have the same number of elements as right.</param>
        /// <param name="right">Should not be empty, and should have the same number of elements as left.</param>
        /// <returns></returns>
        private static double GetLInfNorm(double[] left, double[] right)
        {
            return
                Enumerable.Zip(left, right, (l, r) => Math.Abs(l - r))
                .Max();
        }

        /// The maximum relative difference allowed between Spacings of two volumes
        /// such that they are still considered equal.
        // This is duplicated from InnerEye.CreateDataset.Data:Volume3DProperties:
        // MaximumRelativeDifference
        private const double MaximumRelativeDifferenceForSpacing = 1.0e-4;

        /// The maximum absolute difference allowed between elements of Origins of two volumes,
        /// such that they are still considered equal.
        // This is duplicated from InnerEye.CreateDataset.Data:Volume3DProperties:
        // MaximumAbsoluteDifferenceForOrigin
        private const double MaximumAbsoluteDifferenceForOrigin = 1.0e-3;

        /// The maximum absolute difference allowed between elements of Directions of two volumes,
        /// such that they are still considered equal.
        // This is duplicated from InnerEye.CreateDataset.Data:Volume3DProperties:
        // MaximumAbsoluteDifferenceForDirection
        private const double MaximumAbsoluteDifferenceForDirection = 1.0e-4;

    }
}
