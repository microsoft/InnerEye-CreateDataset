///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    /// <summary>
    /// Represents the coordinates of a point, and its index with respect to the dimensions of
    /// a structure which is handed to the constructor but not stored.
    /// </summary>
    public struct Index3D
    {
        /// <summary>
        /// Location of the point on the x-axis
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Location of the point on the y-axis
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Location of the point on the z-axis
        /// </summary>
        public int Z { get; }

        /// <summary>
        /// Index of the point in a Volume3D structure
        /// </summary>
        public int Index { get; }

        public Index3D((int x, int y, int z) p, int index)
        {
            Index = index;
            X = p.x;
            Y = p.y;
            Z = p.z;
        }
    }
}