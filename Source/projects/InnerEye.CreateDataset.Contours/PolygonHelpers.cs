///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// Contains methods for distance computation to polygons.
    /// </summary>
    public static class PolygonHelpers
    {
        /// <summary>
        /// Computes the square of the Euclidean distance between the two points.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static double CalculateDistance(PointF point1, PointF point2)
        {
            var dx = point1.X - point2.X;
            var dy = point1.Y - point2.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Computes the point in the polygon that is closest to the point given in <paramref name="currentPosition"/>.
        /// Returns false if the polygon is null or does not contain any points. If the function returns true,
        /// <paramref name="closestPoint"/> contains the squared Euclidean distance to the closest point in the polygon,
        /// and the closest polygon point itself.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="currentPosition"></param>
        /// <param name="closestPoint"></param>
        /// <returns></returns>
        public static bool TryGetClosestPointOnPolygon(IEnumerable<Point> polygon, PointF currentPosition, out Tuple<double, Point> closestPoint)
        {
            var distance = double.MaxValue;
            var bestPoint = new Point(0, 0);
            var any = false;
            if (polygon != null)
            {
                foreach (var point in polygon)
                {
                    any = true;
                    var tempDistance = CalculateDistance(currentPosition, point);
                    if (tempDistance < distance)
                    {
                        bestPoint = point;
                        distance = tempDistance;
                    }
                }
            }
            closestPoint = Tuple.Create(distance, bestPoint);
            return any;
        }

        /// <summary>
        /// Computes the point in the polygon that is closest to the point given in <paramref name="currentPosition"/>.
        /// This is legacy code for which no tests existed. After finding the closest point, some additional logic is applied,
        /// based on how close polygon segments get to the point given in <paramref name="currentPosition"/>.
        /// Returns false if the polygon is null or does not contain any points. 
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="currentPosition"></param>
        /// <param name="closestPoint"></param>
        /// <returns></returns>
        public static bool TryGetClosestPointOnPolygon(IReadOnlyList<PointF> polygon, PointF currentPosition, out Tuple<double, PointF> closestPoint)
        {
            if (polygon == null || polygon.Count == 0)
            {
                closestPoint = Tuple.Create(0d, currentPosition);
                return false;
            }

            var currentPoint = polygon[0];
            var distance = CalculateDistance(currentPoint, currentPosition);
            var startEqualsFirst = polygon[0] == polygon[polygon.Count - 1];
            if (polygon.Count == (startEqualsFirst ? 2 : 1))
            {
                closestPoint = Tuple.Create(distance, currentPoint);
                return true;
            }

            var bestPoint = currentPoint;
            var bestPointIndex = 0;
            for (var i = 1; i < polygon.Count; i++)
            {
                currentPoint = polygon[i];
                var tempDistance = CalculateDistance(currentPoint, currentPosition);
                if (tempDistance < distance)
                {
                    bestPointIndex = i;
                    bestPoint = currentPoint;
                    distance = tempDistance;
                }
            }

            var leftPointIndex = bestPointIndex - 1 >= 0 ? bestPointIndex - 1 : polygon.Count - (startEqualsFirst ? 2 : 1);
            var rightPointIndex = bestPointIndex + 1 < polygon.Count ? bestPointIndex + 1 : (startEqualsFirst ? 1 : 0);

            // Get the point left and right of the current position in the polygon
            var leftPair = polygon[leftPointIndex];
            var rightPair = polygon[rightPointIndex];

            // Find the closest point on both line pairs to the current position
            var closestPointOnLeftPair = GetClosestPointOnLine(leftPair, bestPoint, currentPosition);
            var closestPointOnRightPair = GetClosestPointOnLine(rightPair, bestPoint, currentPosition);

            // Now work out which point on the line pairs is closest to the current position
            var leftDistance = CalculateDistance(closestPointOnLeftPair, currentPosition);
            var rightDistance = CalculateDistance(closestPointOnRightPair, currentPosition);

            closestPoint =
                leftDistance < rightDistance
                ? Tuple.Create(leftDistance, closestPointOnLeftPair)
                : Tuple.Create(rightDistance, closestPointOnRightPair);

            return true;
        }

        /// <summary>
        /// Computes the polygon that contains a point that is closest to <paramref name="currentPosition"/> in 
        /// terms of Euclidean distance. The second return value is the closest point inside of that polygon.
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Tuple<Point[], Point> GetClosestPolygonAndPointToPoint(IEnumerable<Point[]> polygons, PointF currentPosition)
        {
            if (polygons == null)
            {
                return null;
            }

            Point[] bestPolygon = null;
            Point? bestPoint = null;
            var bestDistance = double.MaxValue;
            foreach (var polygon in polygons)
            {
                if (TryGetClosestPointOnPolygon(polygon, currentPosition, out var closestPoint)
                    && closestPoint.Item1 < bestDistance)
                {
                    bestPolygon = polygon;
                    bestDistance = closestPoint.Item1;
                    bestPoint = closestPoint.Item2;
                }
            }

            return 
                bestPolygon == null || bestPoint == null 
                ? null 
                : Tuple.Create(bestPolygon, bestPoint.Value);
        }

        /// <summary>
        /// Computes the polygon that contains a point that is closest to <paramref name="currentPosition"/> in 
        /// terms of Euclidean distance. The second return value is the closest point inside of that polygon.
        /// This is legacy code for which no tests exist. Computing the polygon-to-point distances is done via
        /// <see cref="TryGetClosestPointOnPolygon(IReadOnlyList{PointF}, PointF, out Tuple{double, PointF})"/>.
        /// This method applies some additional non-obvious logic in the distance computation.
        /// </summary>
        /// <param name="contour"></param>
        /// <param name="currentPosition"></param>
        /// <returns></returns>
        public static Tuple<ContourPolygon, PointF> GetClosestContourAndPointToPoint(IReadOnlyList<ContourPolygon> contour, PointF currentPosition)
        {
            if (contour == null || contour.Count == 0)
            {
                return null;
            }

            ContourPolygon? bestContour = null;
            PointF? bestPoint = null;

            var bestDistance = double.MaxValue;

            foreach (var currentContour in contour)
            {
                if (TryGetClosestPointOnPolygon(currentContour.ContourPoints, currentPosition, out var closestPoint) 
                    && closestPoint.Item1 < bestDistance)
                {
                    bestContour = currentContour;
                    bestDistance = closestPoint.Item1;
                    bestPoint = closestPoint.Item2;
                }
            }

            return 
                bestContour == null || bestPoint == null 
                ? null 
                : Tuple.Create(bestContour.Value, bestPoint.Value);
        }

        private static PointF GetClosestPointOnLine(PointF start, PointF end, PointF p)
        {
            var vector = end.Subtract(start);
            var length = vector.LengthSquared();
            if (length == 0.0)
            {
                return start;
            }

            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line.
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            var t = p.Subtract(start).DotProduct(vector) / length;
            if (t < 0.0)
            {
                return start; // Beyond the 'v' end of the segment
            }

            if (t > 1.0)
            {
                return end;   // Beyond the 'w' end of the segment
            }

            // Projection falls on the segment
            return start.Add(vector.Multiply(t));
        }
    }
}