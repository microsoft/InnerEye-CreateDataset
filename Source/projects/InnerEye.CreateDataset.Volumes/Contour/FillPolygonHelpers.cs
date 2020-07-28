///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;

    using Bitmap = System.Drawing.Bitmap;
    using Rectangle = System.Drawing.Rectangle;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public static class FillPolygonHelpers
    {
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
        public static void FloodFillHoles(byte[] mask, int dimX, int dimY, int dimZ, int sliceZ, byte foregroundId, byte backgroundId)
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
                            FloodFill(mask, dimX, dimY, sliceZ, new System.Drawing.Point(x, y), tempBackgoundId, backgroundId, bounds);
                        }
                    }
                }
                // Middle rows
                else
                {
                    if (mask[left + y * dimX + sliceZ * dimXy] == backgroundId)
                    {
                        FloodFill(mask, dimX, dimY, sliceZ, new System.Drawing.Point(left, y), tempBackgoundId, backgroundId, bounds);
                    }
                    else if (mask[right + y * dimX + sliceZ * dimXy] == backgroundId)
                    {
                        FloodFill(mask, dimX, dimY, sliceZ, new System.Drawing.Point(right, y), tempBackgoundId, backgroundId, bounds);
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
        private static void FloodFill(byte[] mask, int dimX, int dimY, int sliceZ, System.Drawing.Point startPoint, byte fillId, byte targetId, Int32Rect bounds)
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

            var pixels = new Stack<System.Drawing.Point>();
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
                        pixels.Push(new System.Drawing.Point(temp.X - 1, y1));
                        spanLeft = true;
                    }
                    else if (spanLeft && (temp.X - 1 == left || mask[temp.X - 1 + dimX * y1 + sliceZ * dimXY] != targetId))
                    {
                        spanLeft = false;
                    }

                    if (!spanRight && temp.X < right && mask[temp.X + 1 + dimX * y1 + sliceZ * dimXY] == targetId)
                    {
                        pixels.Push(new System.Drawing.Point(temp.X + 1, y1));
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

        private struct IntersectionXPoint
        {
            public double X;

            // If true then is Plus Epsilon if false is Minus Epsilon
            public bool IsPlusEpsilon;

            public bool IsMinusEpsilon
            {
                get
                {
                    return !IsPlusEpsilon;
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
        public static int FillPolygon<T>(Point[] polygon, T[] result, int dimX, int dimY, int dimZ, int sliceZ, T value)
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
                throw new ArgumentException(nameof(result), "The result array does not have the correct size. The size must equal dimX * dimY * dimZ.");
            }

            var bounds = GetBoundingBox(polygon);

            const double epsilon = 0.01;
            var zOffset = sliceZ * dimX * dimY;
            var nodeIntersections = new IntersectionXPoint[polygon.Length * 2];
            var nodeX = new double[polygon.Length];
            int total = 0;
            //  Loop through the rows of the image.
            for (int y = 0; y < dimY; y++)
            {
                double yPlusEpsilon = y + epsilon;
                double yMinusEpsilon = y - epsilon;

                if ((yPlusEpsilon < bounds.Top && yMinusEpsilon < bounds.Top)
                    || (yPlusEpsilon > bounds.Bottom && yMinusEpsilon > bounds.Bottom))
                {
                    continue;
                }

                //  Build a list of nodes, sorted
                int nodesBoth = FindIntersections(polygon, nodeIntersections, y, yPlusEpsilon, yMinusEpsilon);

                // Merge
                int nodes = MergeIntersections(nodeIntersections, nodeX, nodesBoth);

                //  Fill the pixels between node pairs.
                total = FillNodePairs(result, dimX, value, epsilon, zOffset, nodeX, total, y, nodes);
            }

            return total;
        }

        private static int FillNodePairs<T>(T[] result, int dimX, T value, double epsilon, int zOffset, double[] nodeX, int total, int y, int nodes)
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

        enum State { Background, Bottom, Top, Inside };

        private static int MergeIntersections(IntersectionXPoint[] nodeIntersections, double[] nodeX, int nodesBoth)
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
                    else if (current.IsMinusEpsilon)
                    {
                        nodeX[nodes++] = current.X;
                        state = State.Bottom;
                    }
                }
                else if (state == State.Bottom)
                {
                    if (current.IsMinusEpsilon)
                    {
                        nodeX[nodes++] = current.X;
                        state = State.Background;
                    }
                    else if (current.IsPlusEpsilon)
                    {
                        state = State.Inside;
                    }
                }
                else if (state == State.Top)
                {
                    if (current.IsPlusEpsilon)
                    {
                        nodeX[nodes++] = current.X;
                        state = State.Background;
                    }
                    else if (current.IsMinusEpsilon)
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
                    else if (current.IsMinusEpsilon)
                    {
                        state = State.Top;
                    }
                }
            }

            Debug.Assert(state == State.Background);

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
        ///  Finds the intersections on a polygon given y and yPlusEpsilon and yMinusEpsilon
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="nodeX"></param>
        /// <param name="y"></param>
        /// <param name="yPlusEpsilon"></param>
        /// <param name="yMinusEpsilon"></param>
        /// <returns></returns>
        private static int FindIntersections(Point[] polygon, IntersectionXPoint[] nodeX, int y, double yPlusEpsilon, double yMinusEpsilon)
        {
            int nodes = 0;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y != polygon[j].Y)
                {
                    if (polygon[i].Y < yPlusEpsilon && polygon[j].Y >= yPlusEpsilon
                    || polygon[j].Y < yPlusEpsilon && polygon[i].Y >= yPlusEpsilon)
                    {
                        double x = polygon[i].X
                            + (y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y)
                                                    * (polygon[j].X - polygon[i].X);
                        Debug.Assert(!double.IsNaN(x));
                        Debug.Assert(!double.IsInfinity(x));

                        nodeX[nodes].X = x;
                        nodeX[nodes].IsPlusEpsilon = true;
                        nodes++;
                    }

                    if (polygon[i].Y < yMinusEpsilon && polygon[j].Y >= yMinusEpsilon
                    || polygon[j].Y < yMinusEpsilon && polygon[i].Y >= yMinusEpsilon)
                    {
                        double x = polygon[i].X
                            + (y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y)
                                                    * (polygon[j].X - polygon[i].X);
                        Debug.Assert(!double.IsNaN(x));
                        Debug.Assert(!double.IsInfinity(x));

                        nodeX[nodes].X = x;
                        nodeX[nodes].IsPlusEpsilon = false;
                        nodes++;
                    }
                }
                j = i;
            }

            Sort(nodeX, nodes);

            return nodes;
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
        public static int PointInComplexPolygon(Point[] polygon, Point point, Rect? polygonBounds = null, double epsilon = 0.01d)
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
        private static double IsLeftOfLine(Point linePoint1, Point linePoint2, Point testPoint)
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
        private static int PointInPolygon(Point[] polygon, Point point, double epsilon)
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

        /// <summary>
        /// Checks if point is on the line line1-line2 within some tolerance epsilon
        /// </summary>
        /// <param name="line1">The start point of the line.</param>
        /// <param name="line2">The end point of the line.</param>
        /// <param name="point">The point to check if on/near.</param>
        /// <param name="epsilon">The tolerance for being near/ on a line</param>
        /// <returns>If the point is on the line line1-line2 within the tolerance epsilon.</returns>
        public static bool PointOnLine(Point line1, Point line2, Point point, double epsilon = 0.01d)
        {
            // Compute unit line direction d (and note normal is [-d.Y, d.X]).
            var direction = new Point(line2.X - line1.X, line2.Y - line1.Y);
            var length = (float)(Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y));

            var v = new Point(point.X - line1.X, point.Y - line1.Y);

            if (length == 0)
            {
                // 0 length distance, check if we are near the point
                return Math.Abs(v.X) < epsilon && Math.Abs(v.Y) < epsilon;
            }

            direction = new Point(direction.X / length, direction.Y / length);

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
        /// Gets all the intermediary points along the line segments of the polygon.
        /// Note this does not implement boundary checking, and therefore if the polygon exists within a boundary, these intermediary points
        /// can potentially go outside of these constraints with a radius.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="radius">The radius of points around each point along the line.</param>
        /// <param name="action">The action to invoke at every found point.</param>
        /// <returns>The collection of intermediary along the polygon line segments.</returns>
        public static IList<System.Drawing.Point> GetPointsOnPolygon(IList<Point> polygon, int radius = 0, Action<System.Drawing.Point> action = null)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            if (polygon.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon does not contain any points.");
            }

            var result = new List<System.Drawing.Point>();

            // Init to last
            var previousPoint = polygon[polygon.Count - 1];

            for (var i = 0; i < polygon.Count; i++)
            {
                var currentPoint = polygon[i];

                result.AddRange(GetPointsOnLine(previousPoint, currentPoint, radius: radius, action: action));

                previousPoint = currentPoint;
            }

            return result;
        }

        /// <summary>
        /// Implements Bresenham's Line Algorithm to get all points between two points
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <param name="action">The action to invoke at every found point.</param>
        /// <returns>The list of points between two points.</returns>
        public static IList<System.Drawing.Point> GetPointsOnLine(Point point1, Point point2, double step = 1d, int radius = 0, Action<System.Drawing.Point> action = null)
        {
            var result = new List<System.Drawing.Point>();

            var steepingValue = Math.Abs(step);

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
            var ystep = (y0 < y1) ? steepingValue : -steepingValue;
            var y = y0;

            for (var x = x0; x <= x1; x += steepingValue)
            {
                var point = new System.Drawing.Point((int)(steep ? y : x), (int)(steep ? x : y));

                if (radius <= 0)
                {
                    result.Add(point);
                    action?.Invoke(point);
                }
                else
                {
                    for (var radiusY = point.Y - radius; radiusY <= point.Y + radius; radiusY++)
                    {
                        for (var radiusX = point.X - radius; radiusX <= point.X + radius; radiusX++)
                        {
                            var currentPoint = new System.Drawing.Point(radiusX, radiusY);

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
        public static Rect GetBoundingBox(Point[] polygon)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            if (polygon.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(polygon), "The polygon does not contain any points.");
            }

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

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

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        /// <summary>
        /// Gets the bounding box for the mask. This is an approximation and 
        /// is implementing by getting the min/max values in the X/Y dimensions
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <returns>The bounding Rect.</returns>
        public static Int32Rect GetBoundingBox(byte[] mask, byte id, int dimX, int dimY, int dimZ, int sliceZ)
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

            return new Int32Rect(minX, minY, maxX - minX, maxY - minY);
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
        public static int FillPolygon<T>(System.Drawing.Point[] polygon, T[] result, int dimX, int dimY, int dimZ, int sliceZ, T value)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            var polygonDouble = polygon.Select(p => new Point(p.X, p.Y)).ToArray();
            return FillPolygon(polygonDouble, result, dimX, dimY, dimZ, sliceZ, value);
        }

        /// <summary>
        /// Fills a resulting array using a bitmap.
        /// </summary>
        /// <typeparam name="T">The array result type.</typeparam>
        /// <param name="bitmap">The filled bitmap.</param>
        /// <param name="result">The result array.</param>
        /// <param name="dimX">The width of the result array.</param>
        /// <param name="dimY">The height of the result array.</param>
        /// <param name="sliceZ">The depth slices we are filling.</param>
        /// <param name="value">The value to fill.</param>
        /// <returns>The number of values filled./</returns>
        private static int FillArrayFromBitmap<T>(Bitmap bitmap, T[] result, int dimX, int dimY, int sliceZ, T value)
        {
            var imageWidth = bitmap.Width;
            var imageHeight = bitmap.Height;

            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);

            var bpp = bitmapData.Stride / imageWidth;

            var pixelData = new byte[imageWidth * imageHeight * bpp];
            var foregroundResults = new int[imageHeight];

            // Copy the image values into the array.
            Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);

            Parallel.For(0, imageHeight, delegate (int y)
            {
                var currentIndex = y * dimX + sliceZ * dimX * dimY;

                for (var x = 0; x < imageWidth; x++)
                {
                    var index = y * imageWidth + x;

                    if (pixelData[y * bitmapData.Stride + (x * bpp)] > 0)
                    {
                        foregroundResults[y]++;
                        result[x + currentIndex] = value;
                    }
                }
            });

            bitmap.UnlockBits(bitmapData);

            var foregroundResult = 0;

            for (var i = 0; i < foregroundResults.Length; i++)
            {
                foregroundResult += foregroundResults[i];
            }

            return foregroundResult;
        }
    }
}