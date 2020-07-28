///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    /// <summary>
    /// Contains a contour in an image, described as a polygon with fractional coordinates.
    /// The points making up the polygon can be smoothed or unsmoothed. 
    /// For checking if points are inside/outside
    /// the polygon, it is assumed to be drawn with the drawing convention "Alternate".
    /// </summary>
    [Serializable]
    public struct ContourPolygon : IEquatable<ContourPolygon>
    {
        /// <summary>
        /// Creates a new contour from the given points.
        /// </summary>
        /// <param name="contourPoints">The points of the contour polygon.</param>
        /// <param name="regionAreaPixels">The number of pixels that are contained in the contour.</param>
        public ContourPolygon(PointF[] contourPoints, uint regionAreaPixels)
        {
            ContourPoints = contourPoints ?? throw new ArgumentNullException(nameof(contourPoints));
            RegionAreaPixels = regionAreaPixels;
        }

        /// <summary>
        /// Creates a new contour from points given as integer coordinates.
        /// </summary>
        /// <param name="contourPoints">The points of the contour polygon.</param>
        /// <param name="regionAreaPixels">The number of pixels that are contained in the contour.</param>
        public ContourPolygon(IReadOnlyList<Point> contourPoints, uint regionAreaPixels)
        {
            contourPoints = contourPoints ?? throw new ArgumentNullException(nameof(contourPoints));
            ContourPoints = contourPoints.Select(point => new PointF(point.X, point.Y)).ToArray();
            RegionAreaPixels = regionAreaPixels;
        }

        /// <summary>
        /// Gets the points that make up the contour polygon. The first and the last point are assumed to be connected.
        /// </summary>
        public PointF[] ContourPoints { get; }

        /// <summary>
        /// Gets the number of points that make up the contour.
        /// </summary>
        public int Length => ContourPoints.Length;

        /// <summary>
        /// Gets the number of pixels that are enclosed in the contour. Enclosed here means that the pixel's center,
        /// which are assumed to be drawn at integral coordinates, are enclosed in the contour.
        /// </summary>
        public uint RegionAreaPixels { get; }

        public static bool operator ==(ContourPolygon c1, ContourPolygon c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(ContourPolygon c1, ContourPolygon c2)
        {
            return !c1.Equals(c2);
        }

        public bool Equals(ContourPolygon other)
        {
            if (other == null)
            {
                return false;
            }

            if (Length != other.Length)
            {
                return false;
            }

            return !ContourPoints.Where((t, i) => t != other.ContourPoints[i]).Any();
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals((ContourPolygon)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ContourPoints.Aggregate(19, (current, foo) => current * 31 + foo.GetHashCode());
            }
        }
    }
}