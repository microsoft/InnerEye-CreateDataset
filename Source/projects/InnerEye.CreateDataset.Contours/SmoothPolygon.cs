///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using PointInt = System.Drawing.Point;

    public static class SmoothPolygon
    {
        /// <summary>
        /// When converting a polygon to turtle graphics, use this to indicate "Forward".
        /// </summary>
        public const char TurtleForward = 'F';

        /// <summary>
        /// When converting a polygon to turtle graphics, use this to indicate "Turn Left".
        /// </summary>
        public const char TurtleLeft = 'L';

        /// <summary>
        /// When converting a polygon to turtle graphics, use this to indicate "Turn Right".
        /// </summary>
        public const char TurtleRight = 'R';

        /// <summary>
        /// We current have the following approaches:
        ///     - None: Takes the mask representation of the polygon and return the outer edge
        ///     - Small: Uses a simplistic 'code-book' approach to smoothing the outer edge of the polygon
        /// </summary>
        /// <param name="polygon">The input mask representation of the extracted polygon.</param>
        /// <param name="smoothingType">The smoothing type to use.</param>
        /// <returns>The smoothed polygon.</returns>
        public static PointF[] Smooth(InnerOuterPolygon polygon, ContourSmoothingType smoothingType = ContourSmoothingType.Small)
        {
            var result = SmoothAndMerge(
                polygon,
                (points, isCounterClockwise) => SmoothPoints(points, isCounterClockwise, smoothingType));

            return ContourSimplifier.RemoveRedundantPoints(result);
        }

        /// <summary>
        /// Generates a contour that traces the voxels at the given integer position, and that is smoothed
        /// using the given smoothing level.
        /// </summary>
        /// <param name="points">The set of integer points that describe the polygon.</param>
        /// <param name="isCounterClockwise">If true, the points are an inner contour and are given in CCW order.
        /// Otherwise, assume they are an outer contour and are in clockwise order.</param>
        /// <param name="smoothingType">The type of smoothing that should be applied.</param>
        /// <returns></returns>
        public static PointF[] SmoothPoints(IReadOnlyList<PointInt> points, bool isCounterClockwise, ContourSmoothingType smoothingType)
        {
            switch (smoothingType)
            {
                case ContourSmoothingType.None:
                    return ClockwisePointsToExternalPathWindowsPoints(points, isCounterClockwise, -0.5f);
                case ContourSmoothingType.Small:
                    return SmallSmoothPolygon(points, isCounterClockwise);
                default:
                    throw new NotImplementedException($"There is smoothing method for {smoothingType}");
            }
        }

        /// <summary>
        /// Finds the index of the PointF such that the line from points[index] to points[index+1]
        /// intersects the vertical line at start.X, and where the Y coordinate is smaller than
        /// start.Y. If there are multiple intersections, return
        /// the one with maximum Y coordinate if <paramref name="searchForHighestY"/> is true.
        /// This is used to search for the lowest PointF (highest Y) in a parent contour that is
        /// above the child contour.
        /// If <paramref name="searchForHighestY"/> is false, find the intersection with minimum Y coordinate,
        /// without constraints on start.Y.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int FindIntersectingPoints(IReadOnlyList<PointF> points, PointInt start, bool searchForHighestY)
        {
            float? bestY = null;
            var bestIndex = -1;
            var startX = start.X;

            // When merging a child contour with a parent contour: Points is the parent contour, start
            // is the search start PointF of the inner (child) contour.
            // Search for the line segment of the parent that is closest above the child start PointF.
            // The child is assumed to be fully contained in the parent, hence such a PointF must exist.
            // Among those points, search for the PointF that is closest to the child contour, above the child contour.
            var PointF = points[0];
            for (var index = 1; index <= points.Count; index++)
            {
                var nextPoint = index == points.Count ? points[0] : points[index];

                // A line segment above the start PointF can come from either side, depending on the winding of the parent
                // contour
                if ((PointF.X <= startX && nextPoint.X > startX)
                    || (nextPoint.X < startX && PointF.X >= startX))
                {
                    var isAboveStart = PointF.Y <= start.Y || nextPoint.Y <= start.Y;
                    if ((searchForHighestY && isAboveStart && (bestY == null || PointF.Y > bestY))
                        || (!searchForHighestY && (bestY == null || PointF.Y < bestY)))
                    {
                        bestIndex = index - 1;
                        bestY = PointF.Y;
                    }
                }

                PointF = nextPoint;
            }

            if (bestIndex < 0)
            {
                throw new ArgumentException($"Inconsistent arguments: The polygon does not contain a line segment that intersects at X = {startX}.", nameof(points));
            }

            return bestIndex;
        }

        /// <summary>
        /// Gets the PointF at which the line between point1 and point2 attains the given x value.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static PointF IntersectLineAtX(PointF point1, PointF point2, int x)
        {
            var direction = point2.Subtract(point1);
            if (Math.Abs(direction.X) < 1e-10)
            {
                return point1;
            }

            var deltaX = x - point1.X;
            return new PointF(x, point1.Y + deltaX * direction.Y / direction.X);
        }

        /// <summary>
        /// Connects a parent contour (outer rim of a structure) with a child contour that represents the inner rim
        /// of a structure with holes. The connection is done via a vertical line from the starting PointF of the child contour
        /// to a PointF above (smaller Y coordinate) in the parent contour.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="childStartingPoint"></param>
        /// <returns></returns>
        public static PointF[] ConnectViaVerticalLine(PointF[] parent, PointF[] child, PointInt childStartingPoint)
        {
            if (parent == null || parent.Length == 0)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (child == null || child.Length == 0)
            {
                throw new ArgumentNullException(nameof(child));
            }

            var parentIndex1 = FindIntersectingPoints(parent, childStartingPoint, searchForHighestY: true);
            var childIndex1 = FindIntersectingPoints(child, childStartingPoint, searchForHighestY: false);
            (int, PointF) GetIntersection(PointF[] points, int index)
            {
                var index2 = index == points.Length - 1 ? 0 : index + 1;
                return (index2, IntersectLineAtX(points[index], points[index2], childStartingPoint.X));
            }

            var (_, connectionPointParent) = GetIntersection(parent, parentIndex1);
            var (_, connectionPointChild) = GetIntersection(child, childIndex1);
            var connectionPoints = new PointF[] { connectionPointParent, connectionPointChild };
            return InsertChildIntoParent(parent, parentIndex1, child, childIndex1 + 1, connectionPoints);
        }

        /// <summary>
        /// Splices an array ("parent array") with contour points, and inserts a child contour into.
        /// The result will be composed of the following parts:
        /// * The first entries of the parent array, until and including the <paramref name="insertPositionInParent"/> index
        /// * The connection points in <paramref name="connectingPointsFromParentToChild"/>
        /// * The entries of the child array starting at <paramref name="childStartPosition"/>
        /// * The entries of the child array from 0 to before the <paramref name="childStartPosition"/>
        /// * The connection points in reverse order
        /// * then the remaining entries from the parent array, starting at <paramref name="insertPositionInParent"/> + 1.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="insertPositionInParent">The position in the parent array at which the child should be inserted.
        /// Must be in the range 0 .. parent.Length.</param>
        /// <param name="child"></param>
        /// <param name="childStartPosition">The element of the child array that should be inserted first.</param>
        /// <param name="connectingPointsFromParentToChild">The points that connect parent and child.</param>
        /// <returns></returns>
        public static T[] InsertChildIntoParent<T>(
            T[] parent,
            int insertPositionInParent,
            T[] child,
            int childStartPosition,
            T[] connectingPointsFromParentToChild)
        {
            parent = parent ?? throw new ArgumentNullException(nameof(parent));
            connectingPointsFromParentToChild = connectingPointsFromParentToChild ?? throw new ArgumentNullException(nameof(connectingPointsFromParentToChild));

            if (child == null || child.Length == 0)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (insertPositionInParent < 0 || insertPositionInParent > parent.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(insertPositionInParent));
            }

            if (childStartPosition < 0 || childStartPosition >= child.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(insertPositionInParent));
            }

            var connectionLength = connectingPointsFromParentToChild.Length;
            var result = new T[parent.Length + connectionLength + child.Length + connectionLength];
            var insertAt = 0;
            void Insert(T[] sourceArray, int sourceIndex, int length)
            {
                Array.Copy(sourceArray, sourceIndex, result, insertAt, length);
                insertAt += length;
            }

            // Copy the first elements up to and including the insertPosition directly from parent.
            Insert(parent, 0, insertPositionInParent + 1);

            // The points that make up the connection from the parent to the child starting PointF.
            Insert(connectingPointsFromParentToChild, 0, connectionLength);

            // Then follow the elements from child, up to the end of the child array.
            Insert(child, childStartPosition, child.Length - childStartPosition);

            // The remaining elements from the beginning of the child array
            Insert(child, 0, childStartPosition);

            // Add the connecting points in reverse order, back to the parent.
            for (var index = connectionLength; index > 0; index--)
            {
                result[insertAt++] = connectingPointsFromParentToChild[index - 1];
            }

            // Finally the remaining elements from the parent.
            if (insertPositionInParent < parent.Length - 1)
            {
                Insert(parent, insertPositionInParent + 1, parent.Length - insertPositionInParent - 1);
            }

            return result;
        }

        /// <summary>
        /// Smoothes a PointF polygon that contains an inner and an outer rim. Inner and outer rim are
        /// smoothed separately, and then connected via a zero width "channel": The smoothed
        /// contour will first follow the outer polygon, then go to the inner contour, follow
        /// the inner contour, and then go back to the outer contour.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="smoothingFunction"></param>
        /// <returns></returns>
        private static PointF[] SmoothAndMerge(InnerOuterPolygon polygon, Func<IReadOnlyList<PointInt>, bool, PointF[]> smoothingFunction)
        {
            var result = smoothingFunction(polygon.Outer.Points, false);
            foreach (var inner in polygon.Inner)
            {
                result = ConnectViaVerticalLine(result, smoothingFunction(inner.Points, true), inner.StartPointMinimumY);
            }

            return result;
        }

        private static PointF[] SmallSmoothPolygon(IReadOnlyList<PointInt> polygon, bool isCounterClockwise)
        {
            // The contour simplification code called below expects contours in a string format
            // describing a sequence of unit moves (left, right, straight). The ideal place to
            // compute such a string would be in the code for walking around the contour bounary.
            // But to facilitate early integration, we'll do this here for now.
            var perimeterPath = ClockwisePointsToExternalPathWindowsPoints(polygon, isCounterClockwise, 0.0f);
            var perimeterPath_ = perimeterPath.Select(x => { return new PointInt((int)x.X, (int)x.Y); }).ToList();

            PointInt start, initialDirection;

            string turns;
            ConvertPerimeterPointsToTurnString(perimeterPath_, out start, out initialDirection, out turns);

            var simplified = ContourSimplifier.Simplify(start, initialDirection, turns);

            for (int i = 0; i < simplified.Length; i++)
            {
                simplified[i] = new PointF(simplified[i].X - 0.5f, simplified[i].Y - 0.5f);
            }

            return simplified;
        }

        private static void ConvertPerimeterPointsToTurnString(
            IReadOnlyList<PointInt> points, 
            out PointInt start, 
            out PointInt initialDirection, 
            out string turns)
        {
            // a single pixel mask should induce four perimeter points
            if (points.Count < 4)
            {
                throw new ArgumentException("Too few points, expected at least four.", nameof(points));
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
                        stringWriter.Write(TurtleForward);
                    }
                    else if (delta.X == -direction.Y && delta.Y == direction.X)
                    {
                        stringWriter.Write(TurtleLeft);
                    }
                    else if (delta.X == direction.Y && delta.Y == -direction.X)
                    {
                        stringWriter.Write(TurtleRight);
                    }
                    else
                    {
                        // Contour has doubled back on itself
                        throw new ArgumentException($"Degenerate contour: delta = {delta}, direction = {direction}", nameof(points));
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
        /// Takes a collection of clockwise or counter-clockwise ordered points and makes the PointF follow
        /// the outer edge of the pixel.
        /// This method assumes that there are no gaps between the points (i.e. (0,0) -> (0,2) would not be valid, you would need to have (0,0), (0,1), (0,2))
        /// </summary>
        /// <param name="points">The collection of points.</param>
        /// <param name="shift">This parameter shifts the origin of the points. We do this because DICOM has the origin at the center of the pixel and we extract contours with the origin at the top left.</param>
        /// <returns>The resulting outer edge clockwise ordered collection.</returns>
        /// <param name="isCounterClockwise">If true, assume that the points are ordered counterclockwise
        /// (used for outer contours). If false, assume the points are ordered counterclockwise (used for
        /// inner contours).</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1502", Justification = "This is legacy code without tests, can't refactor at present")]
        private static PointF[] ClockwisePointsToExternalPathWindowsPoints(
            IReadOnlyList<PointInt> points,
            bool isCounterClockwise,
            float shift = -0.5f)
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            var PointF = points[0];

            // Round the X/Y PointF to the containing voxel
            var currentPointX = PointF.X;
            var currentPointY = PointF.Y;

            var shiftX = false;
            var shiftY = false;

            var lastEqualsFirst = points[0] == points[points.Count - 1] && points.Count > 1;
            var result = new List<PointF>();
            var discardFirstSegments = 0;
            var discardLastSegments = 0;

            if (points.Count == (lastEqualsFirst ? 2 : 1))
            {
                result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                return result.ToArray();
            }

            if (isCounterClockwise)
            {
                // To do CCW search, pretend that there is a PointF above (N) of the
                // actual starting PointF. The CW contouring will go from that fake starting
                // PointF down (direction S) to the actual starting PointF, and then turn direction E.
                // This makes assumptions about how the search for inner contours works:
                // We start from the top-left (lowest Y, lowest X) PointF that is background.
                // By construction, there is a PointF of foreground above (at Y-1), which becomes the
                // starting PointF for traversing the inner rim CCW. Furthermore, the PointF above
                // the starting PointF will not be part of the inner rim.
                // The following configurations are possible (X = 0, Y = 0 at top-left,
                // F = fake start PointF, S = first PointF of contour, L = last PointF of contour
                // 1 F 1 1 1 1
                // ? S L ? ? ?
                // or:
                // 1 F 1 1 1 1
                // ? S ? ? ? ?
                // ? 0 L
                // In both cases, walking from F to S will generate two vertical line segments, that will
                // be discarded at the end.
                discardFirstSegments = 2;
                var fakeStart = new PointInt(points[0].X, points[0].Y - 1);
                currentPointX = fakeStart.X;
                currentPointY = fakeStart.Y;
            }

            var startPosition = isCounterClockwise ? 0 : 1;
            var endPosition =
                isCounterClockwise
                ? points.Count
                : points.Count + (lastEqualsFirst ? 0 : 1);

            // The following code has been copied over unmodified from its original location in the
            // InnerEye.CreateDataset.Volumes project, Contours folder.
            for (var i = startPosition; i < endPosition; i++)
            {
                PointF = points[i >= points.Count ? i - points.Count : i];

                // Round the X/Y PointF to the containing voxel
                var pointX = PointF.X;
                var pointY = PointF.Y;

                var changeX = pointX - currentPointX;
                var changeY = pointY - currentPointY;

                // South East Diagonal
                if (changeX == 1 && changeY == 1)
                {
                    if (shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                    }
                    else if (!shiftX & !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                    }
                    else if (shiftY && !shiftX)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                    }

                    shiftX = true;
                    shiftY = false;
                }

                // South West Diagonal
                else if (changeX == -1 && changeY == 1)
                {
                    if (shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    }
                    else if (shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    }
                    else if (!shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    }

                    shiftY = true;
                    shiftX = true;
                }

                // Nort West Diagonal
                else if (changeX == -1 && changeY == -1)
                {
                    if (!shiftX & shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                    }
                    else if (shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                    }
                    else if (shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                    }

                    shiftX = false;
                    shiftY = true;
                }

                // North East Diagonal
                else if (changeX == 1 && changeY == -1)
                {
                    if (!shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                    }
                    else if (!shiftX & shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                    }
                    else if (shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                    }

                    shiftX = false;
                    shiftY = false;
                }

                // West
                else if (changeX == 1 && changeY == 0)
                {
                    if (!shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                    }

                    if (!shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                    }
                    else if (shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                    }

                    shiftX = true;
                    shiftY = false;
                }

                // East
                else if (changeX == -1 && changeY == 0)
                {
                    if (shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    }

                    if (shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    }
                    else if (!shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    }

                    shiftX = false;
                    shiftY = true;
                }

                // South
                else if (changeX == 0 && changeY == 1)
                {
                    if (shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                    }

                    if (!shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                    }
                    else if (!shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift));
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                    }

                    shiftX = true;
                    shiftY = true;
                }

                // North
                else if (changeX == 0 && changeY == -1)
                {
                    if (!shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                    }

                    if (shiftX && shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                    }
                    else if (shiftX && !shiftY)
                    {
                        result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                        result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                    }

                    shiftX = false;
                    shiftY = false;
                }

                result.Add(new PointF(pointX + shift + (shiftX ? 1 : 0), pointY + shift + (shiftY ? 1 : 0)));

                currentPointX = pointX;
                currentPointY = pointY;
            }

            var distanceTolerance = 1e-2;
            if (isCounterClockwise)
            {
                // Hacks to complete counterclockwise contour plotting: In one case, we need
                // to insert an additional segment, and in some cases (not detectable by means of the
                // variables shiftX and shiftY) remove surplus segments.
                if (shiftX && shiftY)
                {
                    result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                }

                // If first and last PointF coincide, remove the last one.
                var d0 = result[discardFirstSegments].Subtract(result[result.Count - 1]).LengthSquared();
                if (Math.Abs(d0) < distanceTolerance)
                {
                    discardLastSegments = 1;
                }
            }
            else
            {
                // Make the last PointF join up to the first
                if (!shiftX && shiftY)
                {
                    result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                }

                if (shiftX && shiftY)
                {
                    result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                }
                else if (shiftX && !shiftY)
                {
                    result.Add(new PointF(currentPointX + shift + 1, currentPointY + shift + 1));
                    result.Add(new PointF(currentPointX + shift, currentPointY + shift + 1));
                    result.Add(new PointF(currentPointX + shift, currentPointY + shift));
                }
            }

            var take = result.Count - discardLastSegments;
            var resultArray = result.Take(take).Skip(discardFirstSegments).ToArray();

            // Measure the distance from the start of the first line segment to the end.
            // This must be 1.
            var firstPoint = resultArray[0];
            var lastPoint = resultArray[resultArray.Length - 1];
            var distance = firstPoint.Subtract(lastPoint).LengthSquared();
            if (Math.Abs(1 - distance) > distanceTolerance)
            {
                throw new InvalidOperationException($"Unable to find a closed contour. The contour started at {firstPoint}, and ended at {lastPoint}, leaving a gap of {distance:0.000}.");
            }

            return resultArray;
        }
    }
}
