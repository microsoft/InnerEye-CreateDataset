///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using InnerEye.CreateDataset.Volumes;

    public static class Point3DExtensions
    {
        /// <summary>
        /// Gets whether the point has any component that is infinity.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool IsInfinity(this Point3D point)
        {
            return double.IsInfinity(point.X) || double.IsInfinity(point.Y) || double.IsInfinity(point.Z);
        }

        /// <summary>
        /// Gets whether the point has any component that is Not A Number (NaN).
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool IsNaN(this Point3D point)
        {
            return double.IsNaN(point.X) || double.IsNaN(point.Y) || double.IsNaN(point.Z);
        }

        /// <summary>
        /// If true, all 3 components of the point are numbers that are not Infinity, and
        /// not NaN.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool IsValid(this Point3D point)
        {
            return !(point.IsInfinity() || point.IsNaN());
        }
    }
}
