///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using InnerEye.CreateDataset.Volumes;
    using PointInt = System.Drawing.Point;

    /// <summary>
    /// Contains helper methods for doing polygon filling.
    /// </summary>
    public static class FillPolygon
    {
        private enum State
        {
            Background, Bottom, Top, Inside
        }

        /// <summary>
        /// Modifies the present volume by filling all points that fall inside of the given contours,
        /// using the provided fill value. Contours are filled on axial slices.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume">The volume that should be modified.</param>
        /// <param name="contours">The contours per axial slice.</param>
        /// <param name="value">The value that should be used to fill all points that fall inside of
        /// the given contours.</param>
        public static void FillContours<T>(Volume3D<T> volume, ContoursPerSlice contours, T value)
        {
            foreach (var contourPerSlice in contours)
            {
                foreach (var contour in contourPerSlice.Value)
                {
                    Fill(contour.ContourPoints, volume.Array, volume.DimX, volume.DimY, volume.DimZ, contourPerSlice.Key, value);
                }
            }
        }

        /// <summary>
        /// Modifies the present volume by filling all points that fall inside of the given contours,
        /// using the provided fill value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume">The volume that should be modified.</param>
        /// <param name="contours">The contours that should be used for filling.</param>
        /// <param name="value">The value that should be used to fill all points that fall inside of
        /// any of the given contours.</param>
        public static void FillContours<T>(Volume2D<T> volume, IEnumerable<ContourPolygon> contours, T value)
        {
            Parallel.ForEach(
                contours,
                contour =>
                {
                    FillContour(volume, contour.ContourPoints, value);
                });
        }

        /// <summary>
        /// Fills the contour using high accuracy (point in polygon testing).
        /// </summary>
        /// <typeparam name="T">The volume type.</typeparam>
        /// <param name="volume">The volume.</param>
        /// <param name="contourPoints">The points that defines the contour we are filling.</param>
        /// <param name="region">The value we will mark in the volume when a point is within the contour.</param>
        /// <returns>The number of points filled.</returns>
        public static int FillContour<T>(Volume2D<T> volume, PointF[] contourPoints, T value)
        {
            return Fill(contourPoints, volume.Array, volume.DimX, volume.DimY, 0, 0, value);
        }

        /// <summary>
        /// Applies flood filling to all holes in all Z slices of the given volume.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId"></param>
        /// <param name="backgroundId"></param>
        public static void FloodFillHoles(
            Volume3D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            byte backgroundId = ModelConstants.MaskBackgroundIntensity)
        {
            Parallel.For(0, volume.DimZ, sliceZ =>
            {
                FloodFillHoles(volume.Array, volume.DimX, volume.DimY, volume.DimZ, sliceZ, foregroundId, backgroundId);
            });
        }

        /// <summary>
        /// Applies flood filling to all holes in the given volume.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId"></param>
        /// <param name="backgroundId"></param>
        public static void FloodFillHoles(
            Volume2D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            byte backgroundId = ModelConstants.MaskBackgroundIntensity)
        {
            FloodFillHoles(volume.Array, volume.DimX, volume.DimY, 0, 0, foregroundId, backgroundId);
        }

        /// <summary>
        /// Uses a scan line implementation of flood filling to fill holes within a mask.
        /// </summary>
        /// <param name="mask">The mask we are flood filling.</param>
        /// <param name="dimX">The X-Dimension length of the mask.</param>
        /// <param name="dimY">The Y-Dimension length of the mask.</param>
        /// <param name="dimZ">The Z-Dimension length of the mask.</param>
        /// <param name="sliceZ">The Z dimension slice we are flood filling holes on.</param>
        /// <param name="foregroundId">The foreground ID.</param>
        /// <param name="backgroundId">The background ID.</param>
        public static void FloodFillHoles(
            byte[] mask,
            int dimX,
            int dimY,
            int dimZ,
            int sliceZ,
            byte foregroundId,
            byte backgroundId)
        {
            if (mask == null)
            {
                throw new ArgumentNullException(nameof(mask));
            }

            if (foregroundId == backgroundId)
            {
                throw new ArgumentException("The foreground ID cannot be the same as the background ID");
            }

            if (mask.Length != dimX * dimY * (dimZ <= 0 ? 1 : dimZ))
            {
                throw new ArgumentException("The X or Y dimension length is incorrect.");
            }

            var bounds = GetBoundingBox(mask, foregroundId, dimX, dimY, dimZ, sliceZ);

            var left = bounds.X;
            var right = bounds.X + bounds.Width;
            var top = bounds.Y;
            var bottom = bounds.Y + bounds.Height;

            var tempBackgoundId = (byte)(foregroundId > backgroundId ? foregroundId + 1 : backgroundId + 1);
            var dimXy = dimX * dimY;

            // Start by flood filling from the outer edge of the mask
            for (var y = top; y < bottom + 1; y++)
            {
                // Top or bottom rows
                if (y == top || y == bottom)
                {
                    for (var x = left; x < right + 1; x++)
                    {
                        if (mask[x + y * dimX + sliceZ * dimXy] == backgroundId)
                        {
                            FloodFill(mask, dimX, dimY, sliceZ, new PointInt(x, y), tempBackgoundId, backgroundId, bounds);
                        }
                    }
                }

                // Middle rows
                else
                {
                    if (mask[left + y * dimX + sliceZ * dimXy] == backgroundId)
                    {
                        FloodFill(mask, dimX, dimY, sliceZ, new PointInt(left, y), tempBackgoundId, backgroundId, bounds);
                    }
                    else if (mask[right + y * dimX + sliceZ * dimXy] == backgroundId)
                    {
                        FloodFill(mask, dimX, dimY, sliceZ, new PointInt(right, y), tempBackgoundId, backgroundId, bounds);
                    }
                }
            }

            // Fix up the output mask
            for (var y = top; y < bottom + 1; y++)
            {
                for (var x = left; x < right + 1; x++)
                {
                    var index = x + y * dimX + sliceZ * dimXy;

                    if (mask[index] == tempBackgoundId)
                    {
                        mask[index] = backgroundId;
                    }
                    else
                    {
                        mask[index] = foregroundId;
                    }
                }
            }
        }

        /// <summary>
        /// http://alienryderflex.com/polygon_fill/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="polygon"></param>
        /// <param name="result"></param>
        /// <param name="dimX"></param>
        /// <param name="dimY"></param>
        /// <param name="dimZ"></param>
        /// <param name="sliceZ"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int Fill<T>(
            PointF[] polygon,
            T[] result,
            int dimX,
            int dimY,
            int dimZ,
            int sliceZ,
            T value)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result), "The result array is null");
            }

            if (polygon.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon does not contain any points.");
            }

            if (dimX < 0 || dimY < 0 || dimZ < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon dimension lengths cannot be less than 0.");
            }

            if (sliceZ < 0 || (dimZ == 0 && sliceZ != 0) || (dimZ > 0 && sliceZ > dimZ - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The Z slice must be within the Z dimensions.");
            }

            var dimXy = dimX * dimY;

            if (result.Length != (dimZ == 0 ? dimXy : dimXy * dimZ))
            {
                throw new ArgumentException("The result array does not have the correct size. The size must equal dimX * dimY * dimZ.", nameof(result));
            }

            var bounds = GetBoundingBox(polygon);

            const float epsilon = 0.01f;
            var zOffset = sliceZ * dimX * dimY;
            var length = polygon.Length;
            var nodeIntersections = new IntersectionXPoint[length * 2];
            int total = 0;
            var nodeX = new float[length];
            var polygonX = new float[length];
            var polygonY = new float[length];
            for (var index = 0; index < length; index++)
            {
                var point = polygon[index];
                polygonX[index] = point.X;
                polygonY[index] = point.Y;
            }

            // Loop through the rows of the image.
            for (int y = 0; y < dimY; y++)
            {
                float yPlusEpsilon = y + epsilon;
                float yMinusEpsilon = y - epsilon;

                if ((yPlusEpsilon < bounds.Top && yMinusEpsilon < bounds.Top)
                    || (yPlusEpsilon > bounds.Bottom && yMinusEpsilon > bounds.Bottom))
                {
                    continue;
                }

                // Build a list of nodes, sorted
                int nodesBoth = FindIntersections(polygonX, polygonY, nodeIntersections, y, yPlusEpsilon, yMinusEpsilon);

                // Merge
                int nodes = MergeIntersections(nodeIntersections, nodeX, nodesBoth);

                // Fill the pixels between node pairs.
                total = FillNodePairs(result, dimX, value, epsilon, zOffset, nodeX, total, y, nodes);
            }

            return total;
        }

        /// <summary>
        /// Fills all points that fall inside of a given polygon, and at the same time,
        /// aggregate statistics on what values are present at the filled pixel
        /// positions in a "count volume" that has the same size as the fill volume.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="polygon"></param>
        /// <param name="fillVolume"></param>
        /// <param name="dimX"></param>
        /// <param name="dimY"></param>
        /// <param name="dimZ"></param>
        /// <param name="sliceZ"></param>
        /// <param name="fillValue"></param>
        /// <param name="countVolume"></param>
        /// <param name="foregroundId"></param>
        /// <returns></returns>
        public static VoxelCounts FillPolygonAndCount(
            PointInt[] polygon,
            ushort[] fillVolume,
            ushort fillValue,
            Volume2D<byte> countVolume,
            byte foregroundId)
        {
            polygon = polygon ?? throw new ArgumentNullException(nameof(polygon));
            fillVolume = fillVolume ?? throw new ArgumentNullException(nameof(fillVolume));
            countVolume = countVolume ?? throw new ArgumentNullException(nameof(countVolume));

            if (polygon.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon does not contain any points.");
            }

            if (fillVolume.Length != countVolume.Length)
            {
                throw new ArgumentException("The fill and the count volume must have the same size.", nameof(fillVolume));
            }

            var pointsAsFloat = polygon.Select(point => new PointF(point.X, point.Y)).ToArray();
            uint foregroundCount = 0;
            uint otherCount = 0;
            var countArray = countVolume.Array;
            var dimX = countVolume.DimX;
            foreach (var point in polygon)
            {
                // Manually computing index, rather than relying on GetIndex, brings substantial speedup.
                var index = point.X + dimX * point.Y;
                if (fillVolume[index] != fillValue)
                {
                    fillVolume[index] = fillValue;
                    if (countArray[index] == foregroundId)
                    {
                        foregroundCount++;
                    }
                    else
                    {
                        otherCount++;
                    }
                }
            }

            var voxelCountsAtPoints = new VoxelCounts(foregroundCount, otherCount);
            var voxelCountsInside = FillPolygonAndCount(
                pointsAsFloat,
                fillVolume,
                fillValue,
                countVolume,
                foregroundId);
            return voxelCountsAtPoints + voxelCountsInside;
        }

        /// <summary>
        /// Checks if point is on the line line1-line2 within some tolerance epsilon
        /// </summary>
        /// <param name="line1">The start point of the line.</param>
        /// <param name="line2">The end point of the line.</param>
        /// <param name="point">The point to check if on/near.</param>
        /// <param name="epsilon">The tolerance for being near/ on a line</param>
        /// <returns>If the point is on the line line1-line2 within the tolerance epsilon.</returns>
        public static bool PointOnLine(PointF line1, PointF line2, PointF point, double epsilon = 0.01d)
        {
            // Compute unit line direction d (and note normal is [-d.Y, d.X]).
            var direction = new PointF(line2.X - line1.X, line2.Y - line1.Y);
            var length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            var v = new PointF(point.X - line1.X, point.Y - line1.Y);

            if (length == 0)
            {
                // 0 length distance, check if we are near the point
                return Math.Abs(v.X) < epsilon && Math.Abs(v.Y) < epsilon;
            }

            direction = new PointF(direction.X / length, direction.Y / length);

            // Check x is within distance epsilon of infinite line
            if (Math.Abs(-v.X * direction.Y + v.Y * direction.X) > epsilon)
            {
                return false;
            }

            // Check x is within line segment
            var p = v.X * direction.X + v.Y * direction.Y;

            if (p + epsilon < 0.0 || p - epsilon > length)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a point is within a complex polygon using window ordering.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="point">The point to check.</param>
        /// <param name="polygonBounds">The pre-calculated polygon bounding box (this could result in computational speed up if provided)</param>
        /// <returns>
        /// Less than 0 if the point is outside the complex polygon
        /// 0  if the point is on the complex polygon bounds, within some tolerance epsilon
        /// Greater than 0 if the point is within the complex polygon
        /// </returns>
        public static int PointInComplexPolygon(PointF[] polygon, PointF point, RectangleF? polygonBounds = null, double epsilon = 0.01d)
        {
            if (polygonBounds.HasValue &&
                (point.X < polygonBounds.Value.Left - epsilon || point.X > polygonBounds.Value.Right + epsilon ||
                point.Y < polygonBounds.Value.Top - epsilon || point.Y > polygonBounds.Value.Bottom + epsilon))
            {
                return -1;
            }

            return PointInPolygon(polygon, point, epsilon);
        }

        /// <summary>
        /// Gets all the intermediary integer points along the line segments of the polygon.
        /// The polygon is specified in terms of fractional points, the result and the action are
        /// executed on integer points.
        /// Note this does not implement boundary checking, and therefore if the polygon exists within a boundary, these intermediary points
        /// can potentially go outside of these constraints with a radius.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="radius">The radius of points around each point along the line.</param>
        /// <param name="action">The action to invoke at every found point.</param>
        /// <returns>The collection of intermediary along the polygon line segments.</returns>
        public static IReadOnlyList<PointInt> GetIntegerPointsOnPolygon(
            IReadOnlyList<PointF> polygon, 
            int radius = 0, 
            Action<PointInt> action = null)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            if (polygon.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon does not contain any points.");
            }

            var result = new List<PointInt>();

            // Init to last
            var previousPoint = polygon[polygon.Count - 1];

            for (var i = 0; i < polygon.Count; i++)
            {
                var currentPoint = polygon[i];

                result.AddRange(GetPointsOnLine(previousPoint, currentPoint, radius, action));

                previousPoint = currentPoint;
            }

            return result;
        }

        /// <summary>
        /// Implements Bresenham's Line Algorithm to get all integer points on the line between two points,
        /// that are given with fractional coordinates. If <paramref name="boxSize"/> is larger than zero,
        /// all points (x +- boxSize, y+- boxSize) are added to the result as well. This will create duplicates
        /// in the results, and the action being called multiple times on the same points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <param name="boxSize">If 0, only the point on the line is added to the result.</param>
        /// <param name="action">The action to invoke at every found point.</param>
        /// <returns>The list of points between two points.</returns>
        public static IReadOnlyList<PointInt> GetPointsOnLine(
            PointF point1, 
            PointF point2,
            int boxSize = 0, 
            Action<PointInt> action = null)
        {
            var result = new List<PointInt>();
            var steppingValue = 1;
            var x0 = point1.X;
            var y0 = point1.Y;
            var x1 = point2.X;
            var y1 = point2.Y;
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            if (steep)
            {
                var t = x0; // swap x0 and y0
                x0 = y0;
                y0 = t;
                t = x1; // swap x1 and y1
                x1 = y1;
                y1 = t;
            }

            if (x0 > x1)
            {
                var t = x0; // swap x0 and x1
                x0 = x1;
                x1 = t;
                t = y0; // swap y0 and y1
                y0 = y1;
                y1 = t;
            }

            var dx = x1 - x0;
            var dy = Math.Abs(y1 - y0);

            var error = dx / 2;
            var ystep = (y0 < y1) ? steppingValue : -steppingValue;
            var y = y0;

            for (var x = x0; x <= x1; x += steppingValue)
            {
                var point = new PointInt((int)(steep ? y : x), (int)(steep ? x : y));

                if (boxSize <= 0)
                {
                    result.Add(point);
                    action?.Invoke(point);
                }
                else
                {
                    for (var radiusY = point.Y - boxSize; radiusY <= point.Y + boxSize; radiusY++)
                    {
                        for (var radiusX = point.X - boxSize; radiusX <= point.X + boxSize; radiusX++)
                        {
                            var currentPoint = new PointInt(radiusX, radiusY);

                            result.Add(currentPoint);
                            action?.Invoke(currentPoint);
                        }
                    }
                }

                error = error - dy;

                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the bounding box for the polygon. This is an approximation and
        /// is implementing by getting the min/max values in the X/Y dimensions
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>The bounding Rect.</returns>
        public static RectangleF GetBoundingBox(PointF[] polygon)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            if (polygon.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon does not contain any points.");
            }

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            for (var i = 0; i < polygon.Length; i++)
            {
                var current = polygon[i];

                if (current.X < minX)
                {
                    minX = current.X;
                }

                if (current.Y < minY)
                {
                    minY = current.Y;
                }

                if (current.X > maxX)
                {
                    maxX = current.X;
                }

                if (current.Y > maxY)
                {
                    maxY = current.Y;
                }
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Gets the bounding box for the mask. This is an approximation and
        /// is implementing by getting the min/max values in the X/Y dimensions
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <returns>The bounding Rect.</returns>
        public static Rectangle GetBoundingBox(byte[] mask, byte id, int dimX, int dimY, int dimZ, int sliceZ)
        {
            if (mask == null)
            {
                throw new ArgumentNullException(nameof(mask), "The mask is null");
            }

            if (mask.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mask), "The mask does not contain any points.");
            }

            if (mask.Length < dimX * dimY * dimZ)
            {
                throw new ArgumentOutOfRangeException(nameof(mask), "The mask is the incorrect size.");
            }

            var dimXy = dimX * dimY;

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            for (var y = 0; y < dimY; y++)
            {
                for (var x = 0; x < dimX; x++)
                {
                    if (mask[x + y * dimX + sliceZ * dimXy] == id)
                    {
                        if (x < minX)
                        {
                            minX = x;
                        }

                        if (y < minY)
                        {
                            minY = y;
                        }

                        if (x > maxX)
                        {
                            maxX = x;
                        }

                        if (y > maxY)
                        {
                            maxY = y;
                        }
                    }
                }
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// This fill polygon methods uses the GDI to accurately fill an integer value polygon. This works correctly when the numbers are
        /// round numbers, and therefore this method only supports System.Drawing points.
        /// </summary>
        /// <typeparam name="T">The result array type.</typeparam>
        /// <param name="polygon">The polygon.</param>
        /// <param name="result">The result array.</param>
        /// <param name="dimX">The width of the result array.</param>
        /// <param name="dimY">The height of the result array.</param>
        /// <param name="dimZ">The depth of the result array.</param>
        /// <param name="sliceZ">The Z slice index (must be equal to or greater than 0).</param>
        /// <param name="value">The value to mark the result array if a pixel is marked as foreground.</param>
        /// <returns>The number of pixels filled.</returns>
        public static int Fill<T>(PointInt[] polygon, T[] result, int dimX, int dimY, int dimZ, int sliceZ, T value)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            var polygonFloat = polygon.Select(p => new PointF(p.X, p.Y)).ToArray();
            return Fill(polygonFloat, result, dimX, dimY, dimZ, sliceZ, value);
        }

        /// <summary>
        /// Implementation of flood fill using scans lines as per https://simpledevcode.wordpress.com/2015/12/29/flood-fill-algorithm-using-c-net/
        /// modified with bug fixes (from comments) and region of interest.
        /// </summary>
        /// <param name="mask">The input mask.</param>
        /// <param name="dimX">The X dimension of the mask.</param>
        /// <param name="dimY">The Y dimension of the mask.</param>
        /// <param name="sliceZ">The Z dimension slice we are flood filling holes on.</param>
        /// <param name="startPoint">The start point to flood file from.</param>
        /// <param name="fillId">The fill ID.</param>
        /// <param name="targetId">The ID we are looking to fill.</param>
        private static void FloodFill(
            byte[] mask,
            int dimX,
            int dimY,
            int sliceZ,
            PointInt startPoint,
            byte fillId,
            byte targetId,
            Rectangle bounds)
        {
            var dimXY = dimX * dimY;

            if (mask[startPoint.X + startPoint.Y * dimX + sliceZ * dimXY] == fillId)
            {
                return;
            }

            var left = bounds.X;
            var right = bounds.X + bounds.Width;
            var top = bounds.Y;
            var bottom = bounds.Y + bounds.Height;

            var pixels = new Stack<PointInt>();
            pixels.Push(startPoint);

            while (pixels.Count != 0)
            {
                var temp = pixels.Pop();
                var y1 = temp.Y;

                while (y1 >= top && mask[temp.X + dimX * y1 + sliceZ * dimXY] == targetId)
                {
                    y1--;
                }

                y1++;

                var spanLeft = false;
                var spanRight = false;

                while (y1 < bottom + 1 && mask[temp.X + dimX * y1 + sliceZ * dimXY] == targetId)
                {
                    mask[temp.X + dimX * y1 + sliceZ * dimXY] = fillId;

                    if (!spanLeft && temp.X > left && mask[temp.X - 1 + dimX * y1 + sliceZ * dimXY] == targetId)
                    {
                        pixels.Push(new PointInt(temp.X - 1, y1));
                        spanLeft = true;
                    }
                    else if (spanLeft && (temp.X - 1 == left || mask[temp.X - 1 + dimX * y1 + sliceZ * dimXY] != targetId))
                    {
                        spanLeft = false;
                    }

                    if (!spanRight && temp.X < right && mask[temp.X + 1 + dimX * y1 + sliceZ * dimXY] == targetId)
                    {
                        pixels.Push(new PointInt(temp.X + 1, y1));
                        spanRight = true;
                    }
                    else if (spanRight && temp.X < right && mask[temp.X + 1 + dimX * y1 + sliceZ * dimXY] != targetId)
                    {
                        spanRight = false;
                    }

                    y1++;
                }
            }
        }

        private static int FillNodePairs<T>(T[] result, int dimX, T value, double epsilon, int zOffset, float[] nodeX, int total, int y, int nodes)
        {
            for (int i = 0; i < nodes; i += 2)
            {
                var first = nodeX[i];
                var second = nodeX[i + 1];

                var initRange = Math.Max(0, (int)Math.Ceiling(first - epsilon));
                var endRange = (int)(second + epsilon);

                if (initRange >= dimX)
                {
                    break;
                }

                if (endRange >= 0)
                {
                    if (endRange >= dimX)
                    {
                        endRange = dimX - 1;
                    }

                    var offsetIndex = y * dimX + zOffset;
                    for (int x = initRange; x <= endRange; x++)
                    {
                        // Fill pixel between x and y
                        result[offsetIndex + x] = value;
                        total++;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// http://alienryderflex.com/polygon_fill/
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="polygon"></param>
        /// <param name="fillVolume"></param>
        /// <param name="dimX"></param>
        /// <param name="dimY"></param>
        /// <param name="dimZ"></param>
        /// <param name="sliceZ"></param>
        /// <param name="fillValue"></param>
        /// <returns></returns>
        private static VoxelCounts FillPolygonAndCount(
            PointF[] polygon,
            ushort[] fillVolume,
            ushort fillValue,
            Volume2D<byte> countVolume,
            byte foregroundId)
        {
            var bounds = GetBoundingBox(polygon);
            const float epsilon = 0.01f;
            var length = polygon.Length;
            var nodeIntersections = new IntersectionXPoint[length * 2];
            var nodeX = new float[length];
            var polygonX = new float[length];
            var polygonY = new float[length];
            for (var index = 0; index < length; index++)
            {
                var point = polygon[index];
                polygonX[index] = point.X;
                polygonY[index] = point.Y;
            }

            var voxelCounts = new VoxelCounts();
            // Loop through the rows of the image.
            for (int y = 0; y < countVolume.DimY; y++)
            {
                float yPlusEpsilon = y + epsilon;
                float yMinusEpsilon = y - epsilon;

                if ((yPlusEpsilon < bounds.Top && yMinusEpsilon < bounds.Top)
                    || (yPlusEpsilon > bounds.Bottom && yMinusEpsilon > bounds.Bottom))
                {
                    continue;
                }

                // Build a list of nodes, sorted
                int nodesBoth = FindIntersections(polygonX, polygonY, nodeIntersections, y, yPlusEpsilon, yMinusEpsilon);

                // Merge
                int nodes = MergeIntersections(nodeIntersections, nodeX, nodesBoth);

                // Fill the pixels between node pairs.
                voxelCounts += FillNodePairsAndCount(
                    fillVolume,
                    fillValue,
                    epsilon,
                    nodeX,
                    y,
                    nodes,
                    countVolume,
                    foregroundId);
            }

            return voxelCounts;
        }

        private static VoxelCounts FillNodePairsAndCount(
            ushort[] result,
            ushort fillValue,
            double epsilon,
            float[] nodeX,
            int y,
            int nodes,
            Volume2D<byte> countVolume,
            byte foregroundId)
        {
            uint foregroundCount = 0;
            uint otherCount = 0;
            var dimX = countVolume.DimX;
            var countArray = countVolume.Array;
            for (int i = 0; i < nodes; i += 2)
            {
                var first = nodeX[i];
                var second = nodeX[i + 1];

                var initRange = Math.Max(0, (int)Math.Ceiling(first - epsilon));
                var endRange = (int)(second + epsilon);

                if (initRange >= dimX)
                {
                    break;
                }

                if (endRange >= 0)
                {
                    if (endRange >= dimX)
                    {
                        endRange = dimX - 1;
                    }

                    // Manually computing index, rather than relying on GetIndex, brings substantial speedup.
                    var offsetY = dimX * y;
                    for (int x = initRange; x <= endRange; x++)
                    {
                        var index = x + offsetY;
                        if (result[index] != fillValue)
                        {
                            result[index] = fillValue;
                            if (countArray[index] == foregroundId)
                            {
                                foregroundCount++;
                            }
                            else
                            {
                                otherCount++;
                            }
                        }
                    }
                }
            }

            return new VoxelCounts(foregroundCount, otherCount);
        }

        private static int MergeIntersections(IntersectionXPoint[] nodeIntersections, float[] nodeX, int nodesBoth)
        {
            int nodes = 0;
            State state = State.Background;
            for (int i = 0; i < nodesBoth; i++)
            {
                var current = nodeIntersections[i];
                if (state == State.Background)
                {
                    if (current.IsPlusEpsilon)
                    {
                        nodeX[nodes++] = current.X;
                        state = State.Top;
                    }
                    else
                    {
                        nodeX[nodes++] = current.X;
                        state = State.Bottom;
                    }
                }
                else if (state == State.Bottom)
                {
                    if (current.IsPlusEpsilon)
                    {
                        state = State.Inside;
                    }
                    else
                    {
                        nodeX[nodes++] = current.X;
                        state = State.Background;
                    }
                }
                else if (state == State.Top)
                {
                    if (current.IsPlusEpsilon)
                    {
                        nodeX[nodes++] = current.X;
                        state = State.Background;
                    }
                    else
                    {
                        state = State.Inside;
                    }
                }
                else if (state == State.Inside)
                {
                    if (current.IsPlusEpsilon)
                    {
                        state = State.Bottom;
                    }
                    else
                    {
                        state = State.Top;
                    }
                }
            }

            Debug.Assert(state == State.Background, "Expected to end up in Background state.");

            return nodes;
        }

        private static void Sort(IntersectionXPoint[] nodeX, int numberOfNodes)
        {
            for (int i = 0; i < numberOfNodes - 1;)
            {
                if (nodeX[i].X > nodeX[i + 1].X)
                {
                    var swap = nodeX[i];
                    nodeX[i] = nodeX[i + 1];
                    nodeX[i + 1] = swap;
                    if (i > 0)
                    {
                        i--;
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Finds the intersections on a polygon given y and yPlusEpsilon and yMinusEpsilon.
        /// The polygon is consumed already split into X and Y coordinates, to avoid the overhead
        /// by repeated calling the property accessors for X and Y coordinates.
        /// </summary>
        /// <param name="polygonX">The x coordinates of all polygon points.</param>
        /// <param name="polygonY">The y coordinates of all polygon points.</param>
        /// <param name="nodeX"></param>
        /// <param name="y"></param>
        /// <param name="yPlusEpsilon"></param>
        /// <param name="yMinusEpsilon"></param>
        /// <returns></returns>
        private static int FindIntersections(
            float[] polygonX, 
            float[] polygonY, 
            IntersectionXPoint[] nodeX, 
            float y, 
            float yPlusEpsilon, 
            float yMinusEpsilon)
        {
            int nodes = 0;
            var length = polygonX.Length;
            var xj = polygonX[length - 1];
            var yj = polygonY[length - 1];
            for (int i = 0; i < length; i++)
            {
                var xi = polygonX[i];
                var yi = polygonY[i];
                if (yi != yj)
                {
                    if (yi < yPlusEpsilon && yj >= yPlusEpsilon
                    || yj < yPlusEpsilon && yi >= yPlusEpsilon)
                    {
                        var x = xi + (y - yi) / (yj - yi) * (xj - xi);
                        Debug.Assert(!float.IsNaN(x), "Intersection is degenerate: NaN");
                        Debug.Assert(!float.IsInfinity(x), "Intersection is degenerate: Infinity");
                        nodeX[nodes].X = x;
                        nodeX[nodes].IsPlusEpsilon = true;
                        nodes++;
                    }

                    if (yi < yMinusEpsilon && yj >= yMinusEpsilon
                    || yj < yMinusEpsilon && yi >= yMinusEpsilon)
                    {
                        var x = xi + (y - yi) / (yj - yi) * (xj - xi);
                        Debug.Assert(!float.IsNaN(x), "Intersection is degenerate: NaN");
                        Debug.Assert(!float.IsInfinity(x), "Intersection is degenerate: Infinity");
                        nodeX[nodes].X = x;
                        nodeX[nodes].IsPlusEpsilon = false;
                        nodes++;
                    }
                }

                xj = xi;
                yj = yi;
            }

            Sort(nodeX, nodes);

            return nodes;
        }

        /// <summary>
        /// Tests if a point is left, on, or right of an infinite line.
        /// </summary>
        /// <param name="linePoint1">A point on the infinte line.</param>
        /// <param name="linePoint2">A second point on the infinte line.</param>
        /// <param name="testPoint">The point to test.</param>
        /// <returns>
        /// Greater than 0 if the testPoint is left of the line
        /// 0 if the testPoint is on the line
        /// Less than 0 if the testPoint is right of the line
        /// </returns>
        private static double IsLeftOfLine(PointF linePoint1, PointF linePoint2, PointF testPoint)
        {
            return (linePoint2.X - linePoint1.X) * (testPoint.Y - linePoint1.Y) - (testPoint.X - linePoint1.X) * (linePoint2.Y - linePoint1.Y);
        }

        /// <summary>
        /// Checks if a point is inside of a complex polygon using wind order of the point.
        /// The algorithm counts the number of times the complex polygon windws around the point.
        /// If the count returns 0, there is an equal number of points left and right of the polygon line pairs. This means the point is outside the polygon.
        /// This code has been modified from http://geomalgorithms.com/a03-_inclusion.html to also check if the point is near a line pair within some tolerance. This allows us to check if we are on the line pair.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>0 if the point is on the line, else inside.</returns>
        private static int PointInPolygon(PointF[] polygon, PointF point, double epsilon)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            if (polygon.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon does not contain any points.");
            }

            var lastEqualsFirst = polygon[0] == polygon[polygon.Length - 1] && polygon.Length > 1;
            var windingOrder = 0;

            // Loop from last to first point (if the last point does not equal the first point, we loop back to the last point)
            for (var i = polygon.Length - 1; i >= (lastEqualsFirst ? 1 : 0); i--)
            {
                var currentPoint = polygon[i];
                var nextPoint = polygon[i == 0 ? polygon.Length - 1 : i - 1];

                // Check if the point is on the line within some tolerance epsilon
                if (PointOnLine(currentPoint, nextPoint, point, epsilon))
                {
                    return 0;
                }

                if (currentPoint.Y <= point.Y)
                {
                    if (nextPoint.Y > point.Y)
                    {
                        // Increase the winding order if the left of the current line
                        if (IsLeftOfLine(currentPoint, nextPoint, point) > 0)
                        {
                            windingOrder++;
                        }
                    }
                }
                else if (nextPoint.Y <= point.Y)
                {
                    // Decrease the winding order if right of the current line
                    if (IsLeftOfLine(currentPoint, nextPoint, point) < 0)
                    {
                        windingOrder--;
                    }
                }
            }

            // If the winding order is 0, we are outside the complex polygon.
            return windingOrder == 0 ? -1 : 1;
        }

        private struct IntersectionXPoint
        {
            public float X;

            // If true then is Plus Epsilon if false is Minus Epsilon
            public bool IsPlusEpsilon;
        }
    }
}