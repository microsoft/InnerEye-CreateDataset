///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public static class PolygonHelpers
    {
        public static double CalculateDistance(double point1X, double point1Y, double point2X, double point2Y)
        {
            return Math.Pow(point2X - point1X, 2) + Math.Pow(point2Y - point1Y, 2);
        }

        public static bool TryGetClosestPointOnPolygon(this System.Drawing.Point[] polygon, Point currentPosition, out Tuple<double, System.Drawing.Point> closestPoint)
        {
            var distance = double.MaxValue;
            var bestPoint = new System.Drawing.Point();

            if (polygon == null || polygon.Length == 0)
            {
                closestPoint = Tuple.Create(distance, bestPoint);

                return false;
            }

            foreach (var point in polygon)
            {
                var tempDistance = CalculateDistance(currentPosition.X, currentPosition.Y, point.X, point.Y);

                if (tempDistance < distance)
                {
                    bestPoint = point;
                    distance = tempDistance;
                }
            }

            closestPoint = Tuple.Create(distance, bestPoint);

            return true;
        }

        public static bool TryGetClosestPointOnPolygon(Point[] polygon, Point currentPosition, out Tuple<double, Point> closestPoint)
        {
            if (polygon == null || polygon.Length == 0)
            {
                closestPoint = Tuple.Create(0d, currentPosition);
                return false;
            }

            var currentPoint = polygon[0];
            var distance = CalculateDistance(currentPoint.X, currentPoint.Y, currentPosition.X, currentPosition.Y);

            var startEqualsFirst = polygon[0] == polygon[polygon.Length - 1];

            if (polygon.Length == (startEqualsFirst ? 2 : 1))
            {
                closestPoint = Tuple.Create(distance, currentPoint);
                return true;
            }

            var bestPoint = currentPoint;
            var bestPointIndex = 0;

            for (var i = 1; i < polygon.Length; i++)
            {
                currentPoint = polygon[i];

                var tempDistance = CalculateDistance(currentPoint.X, currentPoint.Y, currentPosition.X, currentPosition.Y);

                if (tempDistance < distance)
                {
                    bestPointIndex = i;
                    bestPoint = currentPoint;

                    distance = tempDistance;
                }
            }

            var leftPointIndex = bestPointIndex - 1 >= 0 ? bestPointIndex - 1 : polygon.Length - (startEqualsFirst ? 2 : 1);
            var rightPointIndex = bestPointIndex + 1 < polygon.Length ? bestPointIndex + 1 : (startEqualsFirst ? 1 : 0);

            // Get the point left and right of the current position in the polygon
            var leftPair = polygon[leftPointIndex];
            var rightPair = polygon[rightPointIndex];

            // Find the closest point on both line pairs to the current position
            var closestPointOnLeftPair = GetClosestPointOnLine(leftPair, bestPoint, currentPosition);
            var closestPointOnRightPair = GetClosestPointOnLine(rightPair, bestPoint, currentPosition);

            // Now work out which point on the line pairs is closest to the current position
            var leftDistance = CalculateDistance(closestPointOnLeftPair.X, closestPointOnLeftPair.Y, currentPosition.X, currentPosition.Y);
            var rightDistance = CalculateDistance(closestPointOnRightPair.X, closestPointOnRightPair.Y, currentPosition.X, currentPosition.Y);

            closestPoint = leftDistance < rightDistance ? Tuple.Create(leftDistance, closestPointOnLeftPair) : Tuple.Create(rightDistance, closestPointOnRightPair);

            return true;
        }
        
        private static Point GetClosestPointOnLine(Point start, Point end, Point p)
        {
            var vector = end - start;
            var length = vector.LengthSquared;

            if (length == 0.0)
            {
                return start;
            }
            
            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line. 
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            var t = (p - start) * vector / length;

            if (t < 0.0)
            {
                return start; // Beyond the 'v' end of the segment
            }
            else if (t > 1.0)
            {
                return end;   // Beyond the 'w' end of the segment
            }

            // Projection falls on the segment
            return start + t * vector;
        }

        public static Tuple<System.Drawing.Point[], System.Drawing.Point> GetClosestPolygonAndPointToPoint(IList<System.Drawing.Point[]> polygons, Point point)
        {
            if (polygons == null || polygons.Count == 0)
            {
                return null;
            }

            System.Drawing.Point[] bestPolygon = null;
            System.Drawing.Point bestPoint = new System.Drawing.Point();

            var bestDistance = double.MaxValue;

            foreach (var polygon in polygons)
            {
                Tuple<double, System.Drawing.Point> closestPoint;

                if (TryGetClosestPointOnPolygon(polygon, point, out closestPoint) && closestPoint.Item1 < bestDistance)
                { 
                    bestPolygon = polygon;

                    bestDistance = closestPoint.Item1;
                    bestPoint = closestPoint.Item2;
                }
            }

            return bestPolygon == null ? null : Tuple.Create(bestPolygon, bestPoint);
        }

        public static Tuple<Contour, Point> GetClosestContourAndPointToPoint(IList<Contour> contour, Point point)
        {
            if (contour == null || contour.Count == 0)
            {
                return null;
            }

            var bestContour = new Contour();
            var bestPoint = new Point();

            var bestDistance = double.MaxValue;

            foreach (var currentContour in contour)
            {
                Tuple<double, Point> closestPoint;

                if(TryGetClosestPointOnPolygon(currentContour.ContourPoints, point, out closestPoint) && closestPoint.Item1 < bestDistance)
                { 
                    bestContour = currentContour;

                    bestDistance = closestPoint.Item1;
                    bestPoint = closestPoint.Item2;
                }
            }

            return Tuple.Create(bestContour, bestPoint);
        }
    }
}