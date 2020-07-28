///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    
    using System.Diagnostics;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public static class ContourExtensions
    {
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static Volume2D<TK> AllocateSliceStorage<T, TK>(this Volume3D<T> volume, SliceType sliceType)
        {
            var width = 0;
            var height = 0;

            var spacingX = 0d;
            var spacingY = 0d;

            var origin = new Point2D();
            var direction = new Matrix2();

            switch (sliceType)
            {
                case SliceType.Axial:
                    width = volume.DimX;
                    height = volume.DimY;

                    spacingX = volume.SpacingX;
                    spacingY = volume.SpacingY;

                    if (volume.Origin.Data != null)
                    {
                        origin = new Point2D(volume.Origin.X, volume.Origin.Y);
                    }

                    if (volume.Direction.Data != null && volume.Direction.Data.Length == 9)
                    {
                        direction = new Matrix2(new[]
                        {
                            volume.Direction[0, 0],
                            volume.Direction[0, 1],
                            volume.Direction[1, 0],
                            volume.Direction[1, 1]
                        });
                    }

                    break;
                case SliceType.Coronal:
                    width = volume.DimX;
                    height = volume.DimZ;

                    spacingX = volume.SpacingX;
                    spacingY = volume.SpacingZ;

                    if (volume.Origin.Data != null)
                    {
                        origin = new Point2D(volume.Origin.X, volume.Origin.Z);
                    }

                    if (volume.Direction.Data != null && volume.Direction.Data.Length == 9)
                    {
                        direction = new Matrix2(new[]
                        {
                            volume.Direction[0, 0],
                            volume.Direction[0, 2],
                            volume.Direction[2, 0],
                            volume.Direction[2, 2]
                        });
                    }

                    break;
                case SliceType.Sagittal:
                    width = volume.DimY;
                    height = volume.DimZ;

                    spacingX = volume.SpacingY;
                    spacingY = volume.SpacingZ;

                    if (volume.Origin.Data != null)
                    {
                        origin = new Point2D(volume.Origin.Y, volume.Origin.Z);
                    }

                    if (volume.Direction.Data != null && volume.Direction.Data.Length == 9)
                    {
                        direction = new Matrix2(new[]
                        {
                            volume.Direction[1, 1],
                            volume.Direction[1, 2],
                            volume.Direction[2, 1],
                            volume.Direction[2, 2]
                        });
                    }

                    break;
            }

            return new Volume2D<TK>(width, height, spacingX, spacingY, origin, direction);
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static void ExtractSlice<T>(this Volume3D<T> volume, SliceType sliceType, int sliceIndex, T[] outVolume, int skip = 1)
        {
            switch (sliceType)
            {
                case SliceType.Axial:
                    if (sliceIndex < volume.DimZ && outVolume.Length == volume.DimXY * skip)
                    {
                        Parallel.For(0, volume.DimY, delegate (int y)
                        {
                            for (var x = 0; x < volume.DimX; x++)
                            {
                                outVolume[(x + y * volume.DimX) * skip] = volume[((sliceIndex) * volume.DimY + y) * volume.DimX + x];
                            }
                        });
                    }
                    break;
                case SliceType.Coronal:
                    if (sliceIndex < volume.DimY && outVolume.Length == volume.DimZ * volume.DimX * skip)
                    {
                        Parallel.For(0, volume.DimZ, delegate (int z)
                        {
                            for (var x = 0; x < volume.DimX; x++)
                            {
                                outVolume[(x + z * volume.DimX) * skip] = volume[(z * volume.DimY + sliceIndex) * volume.DimX + x];
                            }
                        });
                    }
                    break;
                case SliceType.Sagittal:
                    if (sliceIndex < volume.DimX && outVolume.Length == volume.DimY * volume.DimZ * skip)
                    {
                        Parallel.For(0, volume.DimZ, delegate (int z)
                        {
                            for (var y = 0; y < volume.DimY; y++)
                            {
                                outVolume[(y + z * volume.DimY) * skip] = volume[(z * volume.DimY + y) * volume.DimX + sliceIndex];
                            }
                        });
                    }

                    break;
            }
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static Volume2D<T> ExtractSlice<T>(this Volume3D<T> volume, SliceType sliceType, int index)
        {
            var result = volume.AllocateSliceStorage<T, T>(sliceType);

            if (result != null)
            {
                volume.ExtractSlice(sliceType, index, result.Array);
            }

            return result;
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static ContoursBySlice ExtractContoursPerSlice(
            this Volume3D<byte> volume,
            byte fgId = ModelConstants.MaskForegroundIntensity,
            byte bgId = ModelConstants.MaskBackgroundIntensity,
            SliceType sliceType = SliceType.Axial,
            bool filterEmptyContours = true,
            Region3D<int> regionOfInterest = null,
            SmoothingType axialSmoothingType = SmoothingType.Small)
        {
            var region = regionOfInterest ?? new Region3D<int>(0, 0, 0, volume.DimX - 1, volume.DimY - 1, volume.DimZ - 1);

            int startPoint;
            int endPoint;

            // Only smooth the output on the axial slices
            var smoothingType = axialSmoothingType;

            switch (sliceType)
            {
                case SliceType.Axial:
                    startPoint = region.MinimumZ;
                    endPoint = region.MaximumZ;
                    break;
                case SliceType.Coronal:
                    startPoint = region.MinimumY;
                    endPoint = region.MaximumY;
                    smoothingType = SmoothingType.None;
                    break;
                case SliceType.Sagittal:
                    startPoint = region.MinimumX;
                    endPoint = region.MaximumX;
                    smoothingType = SmoothingType.None;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sliceType), sliceType, null);
            }

            var numberOfSlices = endPoint - startPoint + 1;
            var arrayOfContours = new Tuple<int, IList<Contour>>[numberOfSlices];

            for (var i = 0; i < arrayOfContours.Length; i++)
            {
                var z = startPoint + i;
                var volume2D = volume.ExtractSlice(sliceType, z);
                var contours = volume2D.ExtractContours(fgId, bgId, smoothingType);
                arrayOfContours[i] = Tuple.Create(z, contours);
            }

            return new ContoursBySlice(
                arrayOfContours
                    .Where(x => !filterEmptyContours || x.Item2.Count > 0)
                    .ToDictionary(x => x.Item1, x => x.Item2));
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static IList<Contour> ExtractContours(this Volume2D<byte> volume, byte id = 1, byte bgId = 0, SmoothingType smoothingType = SmoothingType.Small)
        {
            return ExtractPolygonHelpers.ExtractPolygon(volume, id: id, bgId: bgId, smoothingType: smoothingType);
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static void FillContours<T>(this Volume3D<T> volume, ContoursBySlice contours, T value)
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (var contourPerSlice in contours)
            {
                foreach (var contour in contourPerSlice.Value)
                {
                    FillPolygonHelpers.FillPolygon(contour.ContourPoints, volume.Array, volume.DimX, volume.DimY, volume.DimZ, contourPerSlice.Key, value);
                }
            };

            stopwatch.Stop();
            Trace.TraceInformation($"Filling polygons in {stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static void FillContours<T>(this Volume2D<T> volume, IList<Contour> contours, T value)
        {
            Parallel.ForEach(contours,
                contour =>
                {
                    volume.FillContour(contour.ContourPoints, value);
                });
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static void FloodFillHoles(this Volume3D<byte> volume, 
            byte foregroundId = ModelConstants.MaskForegroundIntensity, byte backgroundId = ModelConstants.MaskBackgroundIntensity)
        {
            Parallel.For(0, volume.DimZ, sliceZ =>
            {
                FillPolygonHelpers.FloodFillHoles(volume.Array, volume.DimX, volume.DimY, volume.DimZ, sliceZ, foregroundId, backgroundId);
            });
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static void FloodFillHoles(this Volume2D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity, byte backgroundId = ModelConstants.MaskBackgroundIntensity)
        {
            FillPolygonHelpers.FloodFillHoles(volume.Array, volume.DimX, volume.DimY, 0, 0, foregroundId, backgroundId);
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static Volume3D<byte> ToVolume3D<T>(this ContoursBySlice contours, Volume3D<T> refVolume3D)
        {
            return contours.ToVolume3D(
                refVolume3D.SpacingX,
                refVolume3D.SpacingY,
                refVolume3D.SpacingZ,
                refVolume3D.Origin,
                refVolume3D.Direction,
                new Region3D<int>(0, 0, 0, refVolume3D.DimX - 1, refVolume3D.DimY - 1, refVolume3D.DimZ - 1));
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static Volume3D<byte> ToVolume3D<T>(this ContoursBySlice contours, Volume3D<T> refVolume3D, Region3D<int> roi)
        {
            return contours.ToVolume3D(
                refVolume3D.SpacingX,
                refVolume3D.SpacingY,
                refVolume3D.SpacingZ,
                refVolume3D.Origin,
                refVolume3D.Direction,
                roi);
        }

        /// <summary>
        /// Gets the minimum and maximum slices from the contour collection.
        /// </summary>
        /// <param name="contours">The  contours by slice collection.</param>
        /// <returns>The minimum and maximum slices.</returns>
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static (int Min, int Max) GetMinMaxSlices(this ContoursBySlice contours)
        {
            if (contours == null || !contours.Any())
            {
                throw new ArgumentException(nameof(contours));
            }

            var min = int.MaxValue;
            var max = int.MinValue;

            foreach (var contour in contours)
            {
                if (contour.Key < min)
                {
                    min = contour.Key;
                }

                if (contour.Key > max)
                {
                    max = contour.Key;
                }
            }

            return (min, max);
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
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static Volume2D<byte> CreateVolume2D(this IList<Contour> contours, double spacingX, double spacingY, Point2D origin, Matrix2 direction, Region2D<int> region)
        {
            // Convert every point to within the region
            var subContours = contours.Select(x =>
                                new Contour(
                                    x.ContourPoints.Select(
                                        point => new Point(point.X - region.MinimumX, point.Y - region.MinimumY)).ToArray(), 0)).ToList();

            // Create 2D volume
            var result = new Volume2D<byte>(region.MaximumX - region.MinimumX + 1, region.MaximumY - region.MinimumY + 1, spacingX, spacingY, origin, direction);
            result.FillContours(subContours, ModelConstants.MaskForegroundIntensity);

            return result;
        }

        /// <summary>
        /// Gets the region of interest from the collection of contours (one slice).
        /// </summary>
        /// <param name="contours">The collection of contours.</param>
        /// <exception cref="ArgumentException">Returns an argument exception if the contours are null or do not contain any values.</exception>
        /// <returns>The region of interest.</returns>
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static Region2D<double> GetRegion(this IList<Contour> contours)
        {
            if (contours == null || contours.Count == 0)
            {
                throw new ArgumentException(nameof(contours));
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
                throw new ArgumentException(nameof(contours));
            }

            return new Region2D<double>(minimumX, minimumY, maximumX, maximumY);
        }

        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static Region3D<int> GetRegion(this ContoursBySlice axialContours)
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

        private static Volume3D<byte> ToVolume3D(this ContoursBySlice contours, double spacingX, double spacingY, double spacingZ, Point3D origin, Matrix3 direction, Region3D<int> roi)
        {
            ContoursBySlice subContours = new ContoursBySlice(
                contours.Where(x => x.Value != null).Select(
                    contour =>
                        new KeyValuePair<int, IList<Contour>>(
                            contour.Key - roi.MinimumZ,
                            contour.Value.Select(x =>
                                new Contour(
                                    x.ContourPoints.Select(
                                        point => new Point(point.X - roi.MinimumX, point.Y - roi.MinimumY)).ToArray(), 0))
                                .ToList())).ToDictionary(x => x.Key, y => y.Value));

            var result = new Volume3D<byte>(roi.MaximumX - roi.MinimumX + 1, roi.MaximumY - roi.MinimumY + 1, roi.MaximumZ - roi.MinimumZ + 1, spacingX, spacingY, spacingZ, origin, direction);

            result.FillContours(subContours, ModelConstants.MaskForegroundIntensity);

            return result;
        }

        /// <summary>
        /// Fills the contour using high accuracy (point in polygon testing).
        /// </summary>
        /// <typeparam name="T">The volume type.</typeparam>
        /// <param name="volume">The volume.</param>
        /// <param name="contourPoints">The points that defines the contour we are filling.</param>
        /// <param name="region">The value we will mark in the volume when a point is within the contour.</param>
        /// <returns>The number of points filled.</returns>
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static int FillContour<T>(this Volume2D<T> volume, Point[] contourPoints, T value)
        {
            return FillPolygonHelpers.FillPolygon(contourPoints, volume.Array, volume.DimX, volume.DimY, 0, 0, value);
        }

        /// <summary>
        /// Fills the contour using high accuracy (point in polygon testing).
        /// </summary>
        /// <typeparam name="T">The volume type.</typeparam>
        /// <param name="volume">The volume.</param>
        /// <param name="contourPoints">The points that defines the contour we are filling.</param>
        /// <param name="region">The value we will mark in the volume when a point is within the contour.</param>
        /// <returns>The number of points filled.</returns>
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static int FillContour<T>(this Volume2D<T> volume, System.Drawing.Point[] contourPoints, T value)
        {
            return FillPolygonHelpers.FillPolygon(contourPoints, volume.Array, volume.DimX, volume.DimY, 0, 0, value);
        }
    }
}