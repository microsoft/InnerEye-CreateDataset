///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using InnerEye.CreateDataset.Contours;

    public static class LinearInterpolationHelpers
    {
        /// <summary>
        /// Linear interpolates between the locked contours. This algorithm expects the contours to be created using our contour
        /// extraction code (i.e. ordered and top left contour extracted first). This will not work on contours not extracted
        /// from a binary mask.
        /// </summary>
        /// <param name="lockedContours">The locked contours.</param>
        /// <returns>The locked contours and the interpolated contours.</returns>
        public static ContoursPerSlice LinearInterpolate<T>(Volumes.Volume3D<T> parentVolume, ContoursPerSlice lockedContours)
        {
            if (lockedContours == null)
            {
                throw new ArgumentNullException(nameof(lockedContours));
            }

            if (parentVolume == null)
            {
                throw new ArgumentNullException(nameof(parentVolume));
            }

            var lockedSlicesIndex = lockedContours.Select(x => x.Key).OrderBy(x => x).ToList();

            // If we have one or 0 locked slices, we don't need to interpolate, so we can return the input
            if (lockedSlicesIndex.Count <= 1)
            {
                return lockedContours;
            }

            Volumes.Volume2D<byte> tempExtractContoursVolume = null;
            
            var currentLockedSlice = lockedSlicesIndex[0];
            var currentLockedContours = lockedContours.ContoursForSlice(currentLockedSlice);

            // Make sure we add the current locked contours into the result
            var result = new Dictionary<int, IReadOnlyList<ContourPolygon>> { [currentLockedSlice] = currentLockedContours };

            // Loop over all locked slices
            for (var i = 1; i < lockedSlicesIndex.Count; i++)
            {
                var nextLockedSlice = lockedSlicesIndex[i];
                var nextLockedContours = lockedContours.ContoursForSlice(nextLockedSlice);

                // Now we have the current and next slice, we need to calculate all the interpolated slices between these two
                for (var newSlice = currentLockedSlice + 1; newSlice < nextLockedSlice; newSlice++)
                {
                    result[newSlice] = LinearInterpolate(currentLockedContours, currentLockedSlice, nextLockedContours, nextLockedSlice, newSlice);

                    // Only allocate memory if needed
                    if (tempExtractContoursVolume == null)
                    {
                        tempExtractContoursVolume = parentVolume.AllocateSlice<T,byte>(Volumes.SliceType.Axial);
                    }
                    else
                    {
                        Array.Clear(tempExtractContoursVolume.Array, 0, tempExtractContoursVolume.Length);
                    }

                    // If we have created any contours we need to rasterize and extract to make sure we don't have intersecting contours on the same slice.
                    tempExtractContoursVolume.Fill<byte>(result[newSlice], 1);
                    result[newSlice] = tempExtractContoursVolume.ContoursWithHoles(1);
                }

                // Make sure we add the locked contours into the result
                result[nextLockedSlice] = nextLockedContours;

                currentLockedSlice = nextLockedSlice;
                currentLockedContours = nextLockedContours;
            }

            return new ContoursPerSlice(result);
        }

        /// <summary>
        /// Interpolations linear between two collection of contours on different slices.
        /// </summary>
        /// <param name="contour1"></param>
        /// <param name="contour1Slice"></param>
        /// <param name="contour2"></param>
        /// <param name="contour2Slice"></param>
        /// <param name="interpolationSlice"></param>
        /// <returns></returns>
        private static IReadOnlyList<ContourPolygon> LinearInterpolate(
            IReadOnlyList<ContourPolygon> contour1, 
            int contour1Slice,
            IReadOnlyList<ContourPolygon> contour2, 
            int contour2Slice, 
            int interpolationSlice)
        {
            var result = new List<ContourPolygon>();

            var minContours = contour1;
            var maxContours = contour2;

            var switched = false;

            // Switch contours to make sure we know which slice has the most contours on
            if (minContours.Count > maxContours.Count)
            {
                minContours = contour2;
                maxContours = contour1;

                switched = true;
            }

            // Using the slice with the most contours, we need to find the closest contour on the next slice to every contour on this slice
            foreach (var currentContour in maxContours)
            {
                var closestContour = FindClosestContour(currentContour, minContours);

                if (closestContour != null)
                {
                    var newContours = LinearInterpolate(currentContour.ContourPoints, switched ? contour1Slice : contour2Slice, closestContour.Value.ContourPoints, switched ? contour2Slice : contour1Slice, interpolationSlice);
                    result.Add(new ContourPolygon(newContours, 0));
                }
            }

            return result;
        }

        private static bool IsNullOrEmpty(this ContourPolygon contour)
        {
            return contour.ContourPoints == null || contour.Length == 0;
        }

        /// <summary>
        /// Calculates the squared distance between two points.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        private static double CalculateSquaredDistance(PointF point1, PointF point2)
            => point1.Subtract(point2).LengthSquared();

        /// <summary>
        /// Using the contour we find the next closest contour in the list of contours.
        /// This done by taking the first point in each contour and doing a distance calculation.
        /// </summary>
        /// <param name="contour"></param>
        /// <param name="contours">The list of contours.</param>
        /// <returns>The closest contour from the list of supplied contours.</returns>
        private static ContourPolygon? FindClosestContour(ContourPolygon contour, IReadOnlyList<ContourPolygon> contours)
        {
            if (contour.ContourPoints == null)
            {
                throw new ArgumentNullException(nameof(contour.ContourPoints));
            }

            if (contour.Length == 0)
            {
                throw new ArgumentException("The contour does not have any points.");
            }

            var currentContourPoint = contour.ContourPoints[0];
            var minDistance = double.MaxValue;

            ContourPolygon? minDistanceContour = null;

            foreach (var nextContour in contours)
            {
                if (!contour.IsNullOrEmpty())
                {
                    var nextContourPoint = nextContour.ContourPoints[0];
                    var squaredDistance = CalculateSquaredDistance(currentContourPoint, nextContourPoint);

                    if (squaredDistance < minDistance)
                    {
                        minDistance = squaredDistance;
                        minDistanceContour = nextContour;
                    }
                }
            }

            return minDistanceContour;
        }

        /// <summary>
        /// Using the ordered lists of polygons we calculate a new polygon at a certain distance away. 
        /// This distance is calculated using the supplied slice number, where the new slice index is minSliceIndex < newSliceIndex < maxSliceIndex
        /// </summary>
        /// <param name="polygon1"></param>
        /// <param name="polygon1SliceIndex"></param>
        /// <param name="polygon2"></param>
        /// <param name="polygon2SliceIndex"></param>
        /// <param name="newSliceIndex">The slice index for the new inteprolated contours. This slice index must be between the two polygon slices.</param>
        /// <returns>The interpolated contour for the new slice.</returns>
        public static PointF[] LinearInterpolate(
            PointF[] polygon1,
            int polygon1SliceIndex,
            PointF[] polygon2,
            int polygon2SliceIndex,
            int newSliceIndex)
        {
            if (polygon1 == null || polygon2 == null)
            {
                throw new ArgumentNullException("The input polygons should not be null.");
            }

            if (polygon1SliceIndex == polygon2SliceIndex)
            {
                throw new ArgumentException("The polygons must be on different slices.");
            }

            if (polygon1.Length == 0 || polygon2.Length == 0)
            {
                throw new ArgumentException("The input polygons must have a length greater than 0.");
            }

            var minPolygon = polygon1;
            var maxPolygon = polygon2;

            var minPolygonCountSliceIndex = polygon1SliceIndex;
            var maxPolygonCountSliceIndex = polygon2SliceIndex;

            if (minPolygon.Length > maxPolygon.Length)
            {
                minPolygon = polygon2;
                maxPolygon = polygon1;

                minPolygonCountSliceIndex = polygon2SliceIndex;
                maxPolygonCountSliceIndex = polygon1SliceIndex;
            }

            if (newSliceIndex <= Math.Min(polygon1SliceIndex, polygon2SliceIndex) || newSliceIndex >= Math.Max(polygon1SliceIndex, polygon2SliceIndex))
            {
                throw new ArgumentException($"The new slice index must exist between the two polygon slices.");
            }

            var distance = (newSliceIndex - minPolygonCountSliceIndex) / (double)(maxPolygonCountSliceIndex - minPolygonCountSliceIndex);
            var result = new PointF[minPolygon.Length];
            var minLengthDouble = (double)minPolygon.Length;
            var maxLengthDouble = (double)maxPolygon.Length;
            for (var i = 1; i <= minPolygon.Length; i++)
            {
                var polygon1Value = minPolygon[i - 1];
                var polygon2Value = maxPolygon[(int)Math.Round(i / minLengthDouble * maxLengthDouble) - 1];

                result[i - 1] = PointExtensions.FromDouble(polygon1Value.X + distance * (polygon2Value.X - polygon1Value.X),
                                       polygon1Value.Y + distance * (polygon2Value.Y - polygon1Value.Y));
            }

            return result;
        }
    }
}