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
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Contains extension methods for working with contours.
    /// </summary>
    public static class ContourExtensions
    {
        /// <summary>
        /// Extracts contours from all slices of the given volume, searching for the given foreground value.
        /// Contour extraction will take holes into account.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId">The voxel value that should be used in the contour search as foreground.</param>
        /// <param name="axialSmoothingType">The smoothing that should be applied when going from a point polygon to
        /// contours. This will only affect axial slice, for other slice types no smoothing will be applied.
        /// <param name="sliceType">The type of slice that should be used for contour extraction.</param>
        /// <param name="filterEmptyContours">If true, contours with no points are not extracted.</param>
        /// <param name="regionOfInterest"></param>
        /// <returns></returns>
        public static ContoursPerSlice ContoursWithHolesPerSlice(
            this Volume3D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            SliceType sliceType = SliceType.Axial,
            bool filterEmptyContours = true,
            Region3D<int> regionOfInterest = null,
            ContourSmoothingType axialSmoothingType = ContourSmoothingType.Small)
            => ExtractContours.ContoursWithHolesPerSlice(volume, foregroundId, sliceType, filterEmptyContours, regionOfInterest, axialSmoothingType);

        /// <summary>
        /// Extracts the contours around all voxel values in the volume that have the given foreground value.
        /// All other voxel values (zero and anything that is not the foreground value) is treated as background.
        /// Contour extraction will take account of holes and inserts, up to the default nesting level.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId">The voxel value that should be used as foreground in the contour search.</param>
        /// <param name="smoothingType">The smoothing that should be applied when going from a point polygon to
        /// a contour.</param>
        /// <returns></returns>
        public static IReadOnlyList<ContourPolygon> ContoursWithHoles(
            this Volume2D<byte> volume,
            byte foregroundId = 1,
            ContourSmoothingType smoothingType = ContourSmoothingType.Small)
            => ExtractContours.ContoursWithHoles(volume, foregroundId, smoothingType);

        /// <summary>
        /// Extracts the contours around all voxel values in the volume that have the given foreground value.
        /// All other voxel values (zero and anything that is not the foreground value) is treated as background.
        /// Contour extraction will not take account of holes, and hence only return the outermost
        /// contour around a region of interest.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId">The voxel value that should be used as foreground in the contour search.</param>
        /// <param name="smoothingType">The smoothing that should be applied when going from a point polygon to
        /// a contour.</param>
        /// <returns></returns>
        public static IReadOnlyList<ContourPolygon> ContoursFilled(
            this Volume2D<byte> volume,
            byte foregroundId = 1,
            ContourSmoothingType smoothingType = ContourSmoothingType.Small)
            => ExtractContours.ContoursFilled(volume, foregroundId, smoothingType);

        /// <summary>
        /// Modifies the present volume by filling all points that fall inside of the given contours,
        /// using the provided fill value. Contours are filled on axial slices.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume">The volume that should be modified.</param>
        /// <param name="contours">The contours per axial slice.</param>
        /// <param name="value">The value that should be used to fill all points that fall inside of
        /// the given contours.</param>
        public static void Fill<T>(this Volume3D<T> volume, ContoursPerSlice contours, T value)
            => FillPolygon.FillContours(volume, contours, value);

        /// <summary>
        /// Modifies the present volume by filling all points that fall inside of the given contours,
        /// using the provided fill value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume">The volume that should be modified.</param>
        /// <param name="contours">The contours that should be used for filling.</param>
        /// <param name="value">The value that should be used to fill all points that fall inside of
        /// any of the given contours.</param>
        public static void Fill<T>(this Volume2D<T> volume, IEnumerable<ContourPolygon> contours, T value)
            => FillPolygon.FillContours(volume, contours, value);

        /// <summary>
        /// Applies flood filling to all holes in all Z slices of the given volume.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId"></param>
        /// <param name="backgroundId"></param>
        public static void FillHoles(
            this Volume3D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            byte backgroundId = ModelConstants.MaskBackgroundIntensity)
            => FillPolygon.FloodFillHoles(volume, foregroundId, backgroundId);

        /// <summary>
        /// Applies flood filling to all holes in the given volume.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId"></param>
        /// <param name="backgroundId"></param>
        public static void FillHoles(
            this Volume2D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            byte backgroundId = ModelConstants.MaskBackgroundIntensity)
            => FillPolygon.FloodFillHoles(volume, foregroundId, backgroundId);

        /// <summary>
        /// Creates a volume that has the same size, spacing, and coordinate system as the reference volume,
        /// and fills all points that fall inside of the contours in the present object with the default foreground value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contours">The contours to use for filling.</param>
        /// <param name="refVolume3D">The reference volume to copy spacing and coordinate system from.</param>
        /// <returns></returns>
        public static Volume3D<byte> ToVolume3D<T>(this ContoursPerSlice contours, Volume3D<T> refVolume3D)
        {
            return contours.ToVolume3D(
                refVolume3D.SpacingX,
                refVolume3D.SpacingY,
                refVolume3D.SpacingZ,
                refVolume3D.Origin,
                refVolume3D.Direction,
                refVolume3D.GetFullRegion());
        }

        /// <summary>
        /// Creates a volume that has the same spacing and coordinate system as the reference volume,
        /// and fills all points that fall inside of the contours in the present object with the
        /// default foreground value. The returned volume has its size determined by the given region of interest.
        /// Contour points are transformed using the region of interest.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contours">The contours to use for filling.</param>
        /// <param name="refVolume3D">The reference volume to copy spacing and coordinate system from.</param>
        /// <param name="regionOfInterest"></param>
        /// <returns></returns>
        public static Volume3D<byte> ToVolume3D<T>(this ContoursPerSlice contours, Volume3D<T> refVolume3D, Region3D<int> regionOfInterest)
        {
            return contours.ToVolume3D(
                refVolume3D.SpacingX,
                refVolume3D.SpacingY,
                refVolume3D.SpacingZ,
                refVolume3D.Origin,
                refVolume3D.Direction,
                regionOfInterest);
        }

        /// <summary>
        /// Creates the minimum 2D volume from the list of contours and the region of interest.
        /// </summary>
        /// <param name="contours">The contours by slice.</param>
        /// <param name="spacingX">The X-dimension pixel spacing.</param>
        /// <param name="spacingY">The Y-dimension pixel spacing.</param>
        /// <param name="origin">The patient position origin.</param>
        /// <param name="direction">The directional matrix.</param>
        /// <param name="region">The region of interest.</param>
        /// <returns>The minimum 2D volume.</returns>
        public static Volume2D<byte> CreateVolume2D(this IReadOnlyList<ContourPolygon> contours, double spacingX, double spacingY, Point2D origin, Matrix2 direction, Region2D<int> region)
        {
            // Convert every point to within the region
            var subContours = contours.Select(x =>
                                new ContourPolygon(
                                    x.ContourPoints.Select(
                                        point => new PointF(point.X - region.MinimumX, point.Y - region.MinimumY)).ToArray(), 0)).ToList();

            // Create 2D volume
            var result = new Volume2D<byte>(region.MaximumX - region.MinimumX + 1, region.MaximumY - region.MinimumY + 1, spacingX, spacingY, origin, direction);
            result.Fill(subContours, ModelConstants.MaskForegroundIntensity);

            return result;
        }

        /// <summary>
        /// Gets the region of interest from the collection of contours (one slice).
        /// </summary>
        /// <param name="contours">The collection of contours.</param>
        /// <exception cref="ArgumentException">Returns an argument exception if the contours are null or do not contain any values.</exception>
        /// <returns>The region of interest.</returns>
        public static Region2D<double> GetRegion(this IReadOnlyList<ContourPolygon> contours)
        {
            if (contours == null || contours.Count == 0)
            {
                throw new ArgumentNullException(nameof(contours));
            }

            var minimumX = double.MaxValue;
            var minimumY = double.MaxValue;

            var maximumX = double.MinValue;
            var maximumY = double.MinValue;

            var foundPoint = false;

            for (var i = 0; i < contours.Count; i++)
            {
                var contour = contours[i];

                foreach (var point in contour.ContourPoints)
                {
                    foundPoint = true;

                    if (point.X < minimumX)
                    {
                        minimumX = point.X;
                    }

                    if (point.Y < minimumY)
                    {
                        minimumY = point.Y;
                    }

                    if (point.X > maximumX)
                    {
                        maximumX = point.X;
                    }

                    if (point.Y > maximumY)
                    {
                        maximumY = point.Y;
                    }
                }
            }

            if (!foundPoint)
            {
                throw new ArgumentException("The contours do not contain points, hence no region can be extracted.", nameof(contours));
            }

            return new Region2D<double>(minimumX, minimumY, maximumX, maximumY);
        }

        /// <summary>
        /// Gets the smallest cuboid region that full encloses all the per-slice contours in the present object.
        /// </summary>
        /// <param name="axialContours"></param>
        /// <returns></returns>
        public static Region3D<int> GetRegion(this ContoursPerSlice axialContours)
        {
            var minX = double.MaxValue;
            var maxX = double.MinValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;
            var minZ = int.MaxValue;
            var maxZ = int.MinValue;

            foreach (var contours in axialContours)
            {
                if (contours.Value.Count == 0)
                {
                    continue;
                }

                if (contours.Key < minZ)
                {
                    minZ = contours.Key;
                }

                if (contours.Key > maxZ)
                {
                    maxZ = contours.Key;
                }

                foreach (var contour in contours.Value)
                {
                    foreach (var point in contour.ContourPoints)
                    {
                        if (point.X < minX)
                        {
                            minX = point.X;
                        }

                        if (point.X > maxX)
                        {
                            maxX = point.X;
                        }

                        if (point.Y < minY)
                        {
                            minY = point.Y;
                        }

                        if (point.Y > maxY)
                        {
                            maxY = point.Y;
                        }
                    }
                }
            }

            minX = minX == double.MaxValue ? 0 : minX;
            minY = minY == double.MaxValue ? 0 : minY;
            minZ = minZ == int.MaxValue ? 0 : minZ;

            maxX = maxX == double.MinValue ? 0 : maxX;
            maxY = maxY == double.MinValue ? 0 : maxY;
            maxZ = maxZ == int.MinValue ? 0 : maxZ;

            return new Region3D<int>((int)Math.Floor(minX), (int)Math.Floor(minY), minZ, (int)Math.Ceiling(maxX), (int)Math.Ceiling(maxY), maxZ);
        }

        /// <summary>
        /// Fills the contour using high accuracy (point in polygon testing).
        /// </summary>
        /// <typeparam name="T">The volume type.</typeparam>
        /// <param name="volume">The volume.</param>
        /// <param name="contourPoints">The points that defines the contour we are filling.</param>
        /// <param name="region">The value we will mark in the volume when a point is within the contour.</param>
        /// <returns>The number of points filled.</returns>
        public static int FillContour<T>(this Volume2D<T> volume, PointF[] contourPoints, T value)
        {
            return FillPolygon.Fill(contourPoints, volume.Array, volume.DimX, volume.DimY, 0, 0, value);
        }

        private static Volume3D<byte> ToVolume3D(
            this ContoursPerSlice contours,
            double spacingX,
            double spacingY,
            double spacingZ,
            Point3D origin,
            Matrix3 direction,
            Region3D<int> roi)
        {
            ContoursPerSlice subContours = new ContoursPerSlice(
                contours.Where(x => x.Value != null).Select(
                    contour =>
                        new KeyValuePair<int, IReadOnlyList<ContourPolygon>>(
                            contour.Key - roi.MinimumZ,
                            contour.Value.Select(x =>
                                new ContourPolygon(
                                    x.ContourPoints.Select(
                                        point => new PointF(point.X - roi.MinimumX, point.Y - roi.MinimumY)).ToArray(), 0))
                                .ToList())).ToDictionary(x => x.Key, y => y.Value));

            var result = new Volume3D<byte>(roi.MaximumX - roi.MinimumX + 1, roi.MaximumY - roi.MinimumY + 1, roi.MaximumZ - roi.MinimumZ + 1, spacingX, spacingY, spacingZ, origin, direction);

            result.Fill(subContours, ModelConstants.MaskForegroundIntensity);

            return result;
        }
    }
}