///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Linq;

    using PointInt = System.Drawing.Point;

    /// <summary>
    /// Class to simplify contours induced by segmentation masks defined on a raster grid.
    /// Such contours can be described by sequences of one-unit-long moves with direction
    /// expressed relative to the preceding move. For illustration, consider the following open
    /// contour:
    /// x0
    /// |
    /// x1
    /// |
    /// x2--x3--x4
    ///          |
    ///          x5
    /// This contour may be defined by 6 vertices x0, x1, etc and 5 edges. Equivalently
    /// the contour may be defined using a starting position (x0) and starting direction
    /// (down the page) and a sequence of moves (in {Forwards, Left, Right}:
    /// FFLFR
    /// The appeal of this representation is invariance to local orientation. This motivates
    /// the contour simplification approach used here, which works by replacing patterns of moves
    /// described by sequences of characters in a string (e.g. LRL) with more complex paths defined
    /// (in a direction-relative way) in the continuous 2D domain.
    /// </summary>
    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public static class ContourSimplifier
    {
        private static readonly Tuple<string, Point[]>[] PatternToFragmentMap = new Tuple<string, Point[]>[]
        {
            // NB These fragments are defined in order of decreasing priorty.
            Tuple.Create("FRF", new Point[] { new Point(0d, -0.5d), new Point(0d, 0.1d) , new Point(-0.9d, 1d),new Point(-1.5d, 1d)  }),
            Tuple.Create("FLF", new Point[] { new Point(0d, -0.5d), new Point(0d, 0.1d) , new Point(0.9d, 1d),new Point(1.5d, 1d) }),
            Tuple.Create("RFL", new Point[] { new Point(0d, -0.5d), new Point(-2d, 0.5d) }),
            Tuple.Create("LFR", new Point[] { new Point(0d, -0.5d), new Point(2d, 0.5d) }),
            Tuple.Create("RL", new Point[] { new Point(0d, -0.5d), new Point(-1d, 0.5d) }),
            Tuple.Create("LR", new Point[] { new Point(0d, -0.5d), new Point(1d, 0.5d) }),
            Tuple.Create("R", new Point[] { new Point(0d, -0.5d), new Point(-0.5d, 0d) }),
            Tuple.Create("L", new Point[] { new Point(0d, -0.5d), new Point(0.5d, 0d) })
        };

        /// <summary>
        /// Simplify a countour defined by a string of moves.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="d0"></param>
        /// <param name="turns">String of characters in {F,R,L} defining a sequence of one unit long moves.</param>
        /// <returns>Simplified, smoother polygon with fewer vertices.</returns>
        public static Point[] Simplify(PointInt x0, PointInt d0, string turns)
        {
            PointInt[] points = null;
            PointInt[] directions = null;

            var isClosed = TraceContour(x0, d0, turns, out points, out directions);
            var sequences = new Point[turns.Length][];

            foreach (var pattern in PatternToFragmentMap)
            {
                MatchFragments(turns, isClosed, pattern.Item1, pattern.Item2, sequences);
            }

            return ExpandFragments(points, directions, sequences);
        }

        private static void MatchFragments(string turns, bool isClosed, string pattern, Point[] fragment, Point[][] fragments)
        {
            Debug.Assert(fragments.Length == turns.Length);

            if (pattern.Length > turns.Length)
            {
                return;
            }

            var nullFragment = new Point[0];

            if (isClosed)
            {
                turns += turns.Substring(0, pattern.Length - 1); // wrap around to allow pattern match at last character of turns
            }

            var indexPosition = turns.IndexOf(pattern, 0);

            while (indexPosition != -1)
            {
                var alreadyMatched = false;

                for (int i = indexPosition; i < indexPosition + pattern.Length; i++)
                {
                    if (fragments[i >= fragments.Length ? i - fragments.Length : i] != null)
                    {
                        alreadyMatched = true;
                        continue;
                    }
                }

                if (alreadyMatched == false)
                {
                    fragments[indexPosition] = fragment;

                    for (int i = indexPosition + 1; i < indexPosition + pattern.Length; i++)
                    {
                        fragments[i >= fragments.Length ? i - fragments.Length : i] = nullFragment; // to mark a point that has been substituted
                    }
                }

                indexPosition = turns.IndexOf(pattern, indexPosition + 1);
            }
        }

        /// <summary>
        /// Walk around a contour defined by a sequence of one pixel long moves.
        /// </summary>
        /// <param name="x0">First vertex position.</param>
        /// <param name="d0">Unit vector describing initial direction of motion.</param>
        /// <param name="recipe">String with characters in {F,L,R} defining a seqeunce of moves required to build an open or closed polygon.</param>
        /// <param name="points">Output polygon vertices</param>
        /// <param name="directions">Output edge directions</param>
        /// <returns></returns>
        public static bool TraceContour(PointInt x0, PointInt d0, string recipe, out PointInt[] points, out PointInt[] directions)
        {
            points = new PointInt[recipe.Length + 1];
            points[0] = x0;

            directions = new PointInt[recipe.Length + 1];

            directions[0] = d0;

            var simplified = new List<PointInt>();
            simplified.Add(x0);

            var p = new PointInt();

            for (int i = 0; i < recipe.Length + 1; i++) // we evaluate one more point than is necessary, to determine whether contour is properly closed
            {
                var d = recipe[i >= recipe.Length ? i - recipe.Length : i];

                int r00 = 1;
                int r01 = 0;
                int r10 = 0;
                int r11 = 1; // identity

                if (d == 'L') // left turn
                {
                    r00 = 0; r01 = -1;
                    r10 = 1; r11 = 0;
                }
                else if (d == 'R') // right turn
                {
                    r00 = 0; r01 = 1;
                    r10 = -1; r11 = 0;
                }

                // Update delta
                d0 = new PointInt(r00 * d0.X + r01 * d0.Y, r10 * d0.X + r11 * d0.Y);
                p = new PointInt(x0.X + d0.X, x0.Y + d0.Y);

                if (i + 1 <= recipe.Length)
                {
                    points[i + 1] = p;
                    directions[i + 1] = d0;
                }

                x0 = p;
            }

            // Test for a closed contour... Must start with the turn reference frame
            // defined relative to the last edge and start and end at the same point.
            return points[0].Equals(points[points.Length - 1]) && points[1].Equals(p);
        }

        private static Point[] ExpandFragments(PointInt[] points, PointInt[] directions, Point[][] fragments)
        {
            var contour = new List<Point>();

            for (int i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null && fragments[i].Length > 0)
                {
                    var d0 = directions[i];
                    var d1 = new Point(-d0.Y, d0.X);

                    for (int j = 0; j < fragments[i].Length; j++)
                    {
                        contour.Add(new Point(
                            points[i].X + fragments[i][j].X * d1.X + fragments[i][j].Y * d0.X,
                            points[i].Y + fragments[i][j].X * d1.Y + fragments[i][j].Y * d0.Y));
                    }
                }
            }

            return contour.ToArray();
        }

        // Removes repeated or intermediate points that are not necessary on a polygon
        // Without changing the shape        
        public static Point[] RemoveRedundantPoints(Point[] polygon)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon), "The polygon is null");
            }

            if (polygon.Length <= 4)
            {
                return polygon;
            }

            // Remove conincident vertices
            var deduped = new List<Point>() { polygon[0] };

            Point previousVertex = polygon[0];

            for (int i = 1; i < polygon.Length; i++)
            {
                if ((polygon[i] - previousVertex).Length > 0)
                {
                    deduped.Add(polygon[i]);
                    previousVertex = polygon[i];
                }
            }

            if (deduped.Count > 1 && (deduped.Last() - deduped.First()).Length <= 0) // if last is conincident with first, remove it too
            {
                deduped.RemoveAt(deduped.Count - 1);
            }

            // Remove intermediate vertices in sequences of three or more colinear vertices
            var simplified = new List<Point>();

            simplified.Add(deduped[0]);
            simplified.Add(deduped[1]);

            var direction = deduped[1] - deduped[0];
            direction.Normalize();

            previousVertex = deduped[1];

            for (int i = 2; i < deduped.Count; i++)
            {
                Vector delta = deduped[i] - previousVertex;

                if (Math.Abs((delta.X * direction.Y - delta.Y * direction.X)) <= 0) // component of delta perpendicular to direction
                {
                    simplified.RemoveAt(simplified.Count - 1);
                }

                simplified.Add(deduped[i]);

                previousVertex = deduped[i];
                direction = delta;
                direction.Normalize();
            }

            return simplified.ToArray();
        }
    }
}