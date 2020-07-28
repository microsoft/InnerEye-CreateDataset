///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Windows;


    using PointInt = System.Drawing.Point;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public static class SmoothPolygonHelpers
    {
        /// <summary>
        /// We current have three approaches to smoothing. The application code should only use Small smoothing as None and Medium/ Large
        /// are current for debugging purposes (and comparitative purposes).
        ///     - None: Takes the mask representation of the polygon and return the outer edge
        ///     - Small: Uses a simplistic 'code-book' approach to smoothing the outer edge of the polygon
        ///     - Medium/ Large: Uses an interative approach which produces far smoother contours but will also generate more points.
        ///                      This should not be used when saving contours.
        /// </summary>
        /// <param name="polygon">The input mask representation of the extracted polygon.</param>
        /// <param name="smoothingType">The smoothing type to use.</param>
        /// <returns>The smoothed polygon.</returns>
        public static Point[] SmoothPolygon(PointInt[] polygon, SmoothingType smoothingType = SmoothingType.Small)
        {
            Point[] result;

            switch (smoothingType)
            {
                case SmoothingType.None:
                    result = ClockwisePointsToExternalPathWindowsPoints(polygon);
                    break;
                case SmoothingType.Small:
                    result = SmallSmoothPolygon(polygon);
                    break;
                default:
                    result = LargeSmoothPolygon(polygon);
                    break;
            }

            return ContourSimplifier.RemoveRedundantPoints(result);
        }

        private static Point[] SmallSmoothPolygon(PointInt[] polygon)
        {
            // The contour simplification code called below expects contours in a string format
            // describing a sequence of unit moves (left, right, straight). The ideal place to
            // compute such a string would be in the code for walking around the contour bounary.
            // But to facilitate early integration, we'll do this here for now.
            var perimeterPath = ClockwisePointsToExternalPathWindowsPoints(polygon, 0.0);
            var perimeterPath_ = perimeterPath.Select(x => { return new PointInt((int)x.X, (int)x.Y); }).ToList();

            PointInt start, initialDirection;

            string turns;
            ConvertPerimeterPointsToTurnString(perimeterPath_, out start, out initialDirection, out turns);

            var simplified = ContourSimplifier.Simplify(start, initialDirection, turns);

            for (int i = 0; i < simplified.Length; i++)
            {
                simplified[i] = new Point(simplified[i].X - 0.5, simplified[i].Y - 0.5);
            }

            return simplified;
        }

        private static Point[] LargeSmoothPolygon(PointInt[] polygon, double smoothnessStrength = 0.5d, int additionalPoints = 2, int iterations = 5)
        {
            var perimeterPath = ClockwisePointsToExternalPathWindowsPoints(polygon);
            var result = new Point[perimeterPath.Length];

            for (int i = 0; i < perimeterPath.Length; i++)
            {
                var j = i;
                var k = j + 1 == perimeterPath.Length ? 0 : j + 1;

                result[i] = new Point(
                    0.5f * (perimeterPath[j].X + perimeterPath[k].X),
                    0.5f * (perimeterPath[j].Y + perimeterPath[k].Y));
            }

            var augmentedContourPoints = new Point[result.Length * (additionalPoints + 1)];

            // Augment the points
            var augmentedContourPointCount = 0;

            for (var i = 0; i < result.Length; i++)
            {
                var point1 = result[i];
                var point2 = i < result.Length - 1 ? result[i + 1] : result[0];

                augmentedContourPoints[augmentedContourPointCount++] = point1;

                for (var ii = 0; ii < additionalPoints; ii++)
                {
                    var pointIncrement = (ii + 1d) / (additionalPoints + 1d);

                    augmentedContourPoints[augmentedContourPointCount++] = new Point((
                        (1 - pointIncrement) * point1.X) + (pointIncrement * point2.X),
                        ((1 - pointIncrement) * point1.Y) + (pointIncrement * point2.Y));
                }
            }

            // Smooth the augmented contours
            var smoothedContourPoints = new Point[augmentedContourPoints.Length];

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                for (var i = 0; i < augmentedContourPoints.Length; i++)
                {
                    var pointCurrent = augmentedContourPoints[i];

                    var pointPrevious = i > 0 ? augmentedContourPoints[i - 1] : augmentedContourPoints[augmentedContourPoints.Length - 1];
                    var pointAfter = i < augmentedContourPoints.Length - 1 ? augmentedContourPoints[i + 1] : augmentedContourPoints[0];

                    smoothedContourPoints[i] = new Point(
                        smoothnessStrength * pointCurrent.X + (1 - smoothnessStrength) * (pointPrevious.X + pointAfter.X) / 2,
                        smoothnessStrength * pointCurrent.Y + (1 - smoothnessStrength) * (pointPrevious.Y + pointAfter.Y) / 2);
                }

                augmentedContourPoints = smoothedContourPoints;
            }

            return smoothedContourPoints;
        }

        private static void ConvertPerimeterPointsToTurnString(IList<PointInt> points, out PointInt start, out PointInt initialDirection, out string turns)
        {
            if (points.Count < 4) // a single pixel mask should induce four perimeter points
            {
                throw new ArgumentException("Too few points, expected at least four.", "points");
            }

            using (var stringWriter = new StringWriter())
            {
                var previous = points.Last();
                var direction = new PointInt(points[0].X - previous.X, points[0].Y - previous.Y);

                previous = points[0];

                for (int i = 1; i <= points.Count; i++)
                {
                    var j = i >= points.Count ? i - points.Count : i;
                    var delta = new PointInt(points[j].X - previous.X, points[j].Y - previous.Y);

                    if (delta.X == direction.X && delta.Y == direction.Y)
                    {
                        stringWriter.Write('F');
                    }
                    else if (delta.X == -direction.Y && delta.Y == direction.X)
                    {
                        stringWriter.Write('L');
                    }
                    else if (delta.X == direction.Y && delta.Y == -direction.X)
                    {
                        stringWriter.Write('R');
                    }
                    else
                    {
                        // Contour has doubled back on itself
                        throw new ArgumentException($"Degenerate contour: delta = {delta}, direction = {direction}", "points");
                    }

                    previous = points[j];
                    direction = delta;
                }

                turns = stringWriter.ToString();
            }

            start = points[0];
            var last = points[points.Count - 1];

            initialDirection = new PointInt(start.X - last.X, start.Y - last.Y);
        }
        /// <summary>
        /// Takes a collection of clockwise ordered points and makes the point follow the outer edge of the pixel.
        /// This method assumes the collection is ordered clockwise and that there are no gaps between the points (i.e. (0,0) -> (0,2) would not be valid, you would need to have (0,0), (0,1), (0,2))
        /// </summary>
        /// <param name="points">The clockwise collection of points.</param>
        /// <param name="shift">This parameter shifts the origin of the points. We do this because DICOM has the origin at the center of the pixel and we extract contours with the origin at the top left.</param>
        /// <returns>The resulting outer edge clockwise ordered collection.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static Point[] ClockwisePointsToExternalPathWindowsPoints(IList<PointInt> points, double shift = -0.5d)
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            var point = points[0];

            // Round the X/Y point to the containing voxel
            var currentPointX = point.X;
            var currentPointY = point.Y;

            var shiftX = false;
            var shiftY = false;

            var lastEqualsFirst = points[0] == points[points.Count - 1] && points.Count > 1;

            var result = new List<Point>();

            if (points.Count == (lastEqualsFirst ? 2 : 1))
            {
                result.Add(new Point(currentPointX + shift, currentPointY + shift));
                result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
            }
            else
            {
                for (var i = 1; i < points.Count + (lastEqualsFirst ? 0 : 1); i++)
                {
                    point = points[i >= points.Count ? i - points.Count : i];

                    // Round the X/Y point to the containing voxel
                    var pointX = point.X;
                    var pointY = point.Y;

                    var changeX = pointX - currentPointX;
                    var changeY = pointY - currentPointY;

                    // South East Diagonal
                    if (changeX == 1 && changeY == 1)
                    {
                        if (shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                        }
                        else if (!shiftX & !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                        }
                        else if (shiftY && !shiftX)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                        }

                        shiftX = true;
                        shiftY = false;
                    }
                    // South West Diagonal
                    else if (changeX == -1 && changeY == 1)
                    {
                        if (shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                        }
                        else if (shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                        }
                        else if (!shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                        }

                        shiftY = true;
                        shiftX = true;
                    }
                    // Nort West Diagonal
                    else if (changeX == -1 && changeY == -1)
                    {
                        if (!shiftX & shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                        }
                        else if (shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                        }
                        else if (shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                        }

                        shiftX = false;
                        shiftY = true;
                    }
                    // North East Diagonal
                    else if (changeX == 1 && changeY == -1)
                    {
                        if (!shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                        }
                        else if (!shiftX & shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                        }
                        else if (shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                        }

                        shiftX = false;
                        shiftY = false;
                    }
                    // West
                    else if (changeX == 1 && changeY == 0)
                    {
                        if (!shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                        }
                        if (!shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                        }
                        else if (shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                        }

                        shiftX = true;
                        shiftY = false;
                    }
                    // East
                    else if (changeX == -1 && changeY == 0)
                    {
                        if (shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                        }
                        if (shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                        }
                        else if (!shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                        }

                        shiftX = false;
                        shiftY = true;
                    }
                    // South
                    else if (changeX == 0 && changeY == 1)
                    {
                        if (shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                        }
                        if (!shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                        }
                        else if (!shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift));
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                        }

                        shiftX = true;
                        shiftY = true;
                    }
                    // North
                    else if (changeX == 0 && changeY == -1)
                    {
                        if (!shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                        }
                        if (shiftX && shiftY)
                        {
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                        }
                        else if (shiftX && !shiftY)
                        {
                            result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                            result.Add(new Point(currentPointX + shift, currentPointY + shift));
                        }

                        shiftX = false;
                        shiftY = false;
                    }

                    result.Add(new Point(pointX + shift + (shiftX ? 1 : 0), pointY + shift + (shiftY ? 1 : 0)));

                    currentPointX = pointX;
                    currentPointY = pointY;
                }

                // Make the last point join up to the first
                if (!shiftX && shiftY)
                {
                    result.Add(new Point(currentPointX + shift, currentPointY + shift));
                }
                if (shiftX && shiftY)
                {
                    result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                    result.Add(new Point(currentPointX + shift, currentPointY + shift));
                }
                else if (shiftX && !shiftY)
                {
                    result.Add(new Point(currentPointX + shift + 1, currentPointY + shift + 1));
                    result.Add(new Point(currentPointX + shift, currentPointY + shift + 1));
                    result.Add(new Point(currentPointX + shift, currentPointY + shift));
                }
            }

            var distance = (result[0] - result[result.Count - 1]).Length;

            if (Math.Abs(1 - distance) > 0.01)
            {
                throw new Exception("This code did not function correctly");
            }

            return result.ToArray();
        }
    }
}
