///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using PointInt = System.Drawing.Point;

    /// <summary>
    /// Describes a closed polygon, specified by a set of integer points.
    /// </summary>
    public class PolygonPoints
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="voxelCounts"></param>
        /// <param name="insideOfPolygon"></param>
        /// <param name="isBackground"></param>
        /// <param name="startPointMinimumY"></param>
        public PolygonPoints(
            PointInt[] points,
            VoxelCounts voxelCounts,
            ushort insideOfPolygon,
            bool isInside,
            PointInt startPointMinimumY)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            VoxelCounts = voxelCounts ?? throw new ArgumentNullException(nameof(voxelCounts));
            InsideOfPolygon = insideOfPolygon;
            IsInnerContour = isInside;
            StartPointMinimumY = startPointMinimumY;
        }

        /// <summary>
        /// Gets the coordinates of the points that make out the edge of the contour (the outermost points
        /// that have the foreground value when searching for outer rims, or the innermost points that have
        /// the foreground value when searching for inner rims).
        /// </summary>
        public PointInt[] Points { get; }

        /// <summary>
        /// Gets the number of polygon points.
        /// </summary>
        public int Count => Points.Length;

        /// <summary>
        /// Gets the numbers of different voxel values that were found inside the polygon.
        /// The semantics of voxel counts is different for polygons with <see cref="IsInnerContour"/> true or false:
        /// If <see cref="IsInnerContour"/> is false, the polygon represents the outside of a foreground region.
        /// Voxel counts are obtained over the points of the polygon and all voxels inside.
        /// If <see cref="IsInnerContour"/> is true, the polygon represents the inner rim of a hole inside foreground
        /// structure. The voxel counts are obtained over all points that are full inside of the polygon, but
        /// not the polygon points itself.
        /// </summary>
        public VoxelCounts VoxelCounts { get; }

        /// <summary>
        /// Gets the number of the polygon that the present polygon is contained in. If the value is 0,
        /// the polygon is a top-level polygon, found when searching over the whole canvas.
        /// </summary>
        public ushort InsideOfPolygon { get; }

        /// <summary>
        /// If true, the present polygon was found by search for the inside of another polygon (walking
        /// along the rim of the region with value 0).
        /// If false, it was found by searching for foreground voxels with value 1.
        /// </summary>
        public bool IsInnerContour { get; }

        /// <summary>
        /// Gets or sets the number of parent polygons that the present polygon has. Nesting level 0 means that the
        /// polygon is a top-level polygon, found when searching over the whole canvas. Nesting level 1 would
        /// be for holes inside of a top-level polygon, and so forth.
        /// </summary>
        public int NestingLevel { get; set; }

        /// <summary>
        /// Gets the point at which the polygon search started. This must be a point that is inside the polygon,
        /// and obtains the minimum Y coordinate among all points.
        /// </summary>
        public PointInt StartPointMinimumY { get; }

        /// <summary>
        /// Gets the voxel value that was used as the foreground when creating the present polygon.
        /// </summary>
        public byte ForegroundValue => VoxelValue(IsInnerContour);

        /// <summary>
        /// Gets the voxel value that was used as the background when creating the present polygon.
        /// </summary>
        public byte BackgroundValue => VoxelValue(!IsInnerContour);

        /// <summary>
        /// Gets the voxel value to search for, if the polyon represents background or not.
        /// If true, return 0. If false, return 1.
        /// </summary>
        /// <param name="isBackground"></param>
        /// <returns></returns>
        public static byte VoxelValue(bool isBackground) => isBackground ? (byte)0 : (byte)1;
    }
}
