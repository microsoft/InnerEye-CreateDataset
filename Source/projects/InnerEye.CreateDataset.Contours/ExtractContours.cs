///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using InnerEye.CreateDataset.Volumes;

    using PointInt = System.Drawing.Point;

    /// <summary>
    /// Contains helper functions necessary for extracting polygons from binary masks.
    /// </summary>
    public static class ExtractContours
    {
        /// <summary>
        /// The deltas from a center pixel in 2D, going through the 8-neighborhood of the pixel,
        /// starting with (X+1, Y)
        /// </summary>
        private static readonly PointInt[] Delta =
            {
                new PointInt(1,  0),
                new PointInt(1,  1),
                new PointInt(0,  1),
                new PointInt(-1,  1),
                new PointInt(-1,  0),
                new PointInt(-1, -1),
                new PointInt(0, -1),
                new PointInt(1, -1)
            };

        /// <summary>
        /// The default value for the maximum nesting level up to which polygons should be extracted. If set to
        /// 0, only the outermost polygons will be returned. If 1, the outermost polygons and the holes therein.
        /// If 2, the outermost polygon, the holes, and the foreground inside the holes.</param>
        /// </summary>
        public const int DefaultMaxPolygonNestingLevel = 6;

        /// <summary>
        /// Takes an input volume and extracts the contours for the regions where the voxel value is
        /// <paramref name="foregroundId"/>. Region are assumed to be filled: If there is a doughnut-shaped
        /// region in the volume, only the outer rim of that region is extracted.
        /// </summary>
        /// <param name="volume">The input volume.</param>
        /// <param name="foregroundId">The voxel value that should be considered foreground.</param>
        /// <returns>The collection of contours.</returns>
        public static IReadOnlyList<PolygonPoints> PolygonsFilled(
            Volume2D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity)
        {
            volume = volume ?? throw new ArgumentNullException(nameof(volume));
            var binaryVolume = CreateBinaryVolume(volume, foregroundId);
            var foundPolygons = new ushort[volume.Length];
            var polygons = ExtractPolygons(
                binaryVolume,
                foundPolygons,
                searchInsidePolygon: 0,
                isInnerPolygon: false,
                firstNewPolygon: 1);
            return polygons.Values.ToList();
        }

        /// <summary>
        /// Extracts a set of possibly nested polygons from a given binary mask.
        /// The top level extracted polygons are the outermost regions on the canvas where the voxel value is 1.
        /// Inside of each of these polygons can be further polygons that describe holes (doughnut shape,
        /// voxel value 0), which in turn can contain further polygons that have voxel value 1, etc.
        /// </summary>
        /// <param name="volume">The binary mask from which the polygons should be extracted. </param>
        /// <param name="foregroundId">The voxel value that should be considered foreground.</param>
        /// <param name="maxNestingLevel">The maximum nesting level up to which polygons should be extracted. If set to
        /// 0, only the outermost polygons will be returned. If 1, the outermost polygons and the holes therein.
        /// If 2, the outermost polygon, the holes, and the foreground inside the holes.</param>
        /// <param name="enableVerboseOutput">If true, print statistics about the found polygons to Trace.</param>
        /// <returns></returns>
        public static IReadOnlyList<InnerOuterPolygon> PolygonsWithHoles(
            Volume2D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            int maxNestingLevel = DefaultMaxPolygonNestingLevel,
            bool enableVerboseOutput = false)
        {
            var contoursWithHoles = new Stack<ushort>();
            void PushIfHolesPresent(KeyValuePair<ushort, PolygonPoints> p)
            {
                var backgroundVoxels = p.Value.VoxelCounts.Other;
                if (backgroundVoxels > 0)
                {
                    contoursWithHoles.Push(p.Key);
                }
            }

            var binaryVolume = CreateBinaryVolume(volume, foregroundId);
            var foundPolygons = new ushort[volume.Length];
            var searchInside = (ushort)0;
            var contours = ExtractPolygons(
                binaryVolume,
                foundPolygons,
                searchInside,
                isInnerPolygon: false,
                firstNewPolygon: 1);
            var result = new Dictionary<ushort, InnerOuterPolygon>();
            foreach (var p in contours)
            {
                result.Add(p.Key, new InnerOuterPolygon(p.Value));
                PushIfHolesPresent(p);
                if (enableVerboseOutput)
                {
                    Trace.TraceInformation($"Polygon {p.Key} on canvas: {p.Value.Count} points on the outside. Contains {p.Value.VoxelCounts.Other} background voxels.");
                }
            }

            var remainingHoles = 0;
            uint remainingHoleVoxels = 0;
            while (contoursWithHoles.Count > 0)
            {
                var parentIndex = contoursWithHoles.Pop();
                var parentWithHoles = contours[parentIndex];
                if (parentWithHoles.NestingLevel < maxNestingLevel)
                {
                    // Inside of the polygon just popped from the stack, search for holes or inserts:
                    // What is background in the enclosing polygon is now foreground.
                    var nextIndex = (ushort)(contours.Keys.Max() + 1);
                    var searchInsidePolygon = parentIndex;
                    var isInnerPolygon = !parentWithHoles.IsInnerContour;
                    var holePolygons = ExtractPolygons(
                        binaryVolume,
                        foundPolygons,
                        searchInsidePolygon,
                        isInnerPolygon,
                        nextIndex);
                    foreach (var p in holePolygons)
                    {
                        p.Value.NestingLevel = parentWithHoles.NestingLevel + 1;
                        contours.Add(p.Key, p.Value);
                        if (p.Value.IsInnerContour)
                        {
                            result[parentIndex].AddInnerContour(p.Value);
                        }
                        else
                        {
                            result.Add(p.Key, new InnerOuterPolygon(p.Value));
                        }

                        PushIfHolesPresent(p);
                        if (enableVerboseOutput)
                        {
                            Trace.TraceInformation($"Polygon {p.Key} inside {p.Value.InsideOfPolygon} (nesting level {p.Value.NestingLevel}): {p.Value.Count} points on the outside. Contains {p.Value.VoxelCounts.Other} hole voxels.");
                        }
                    }
                }
                else
                {
                    remainingHoles++;
                    remainingHoleVoxels += parentWithHoles.VoxelCounts.Other;
                }
            }

            if (remainingHoles > 0 && enableVerboseOutput)
            {
                Trace.TraceWarning($"Capping at maximum nesting level was applied. There are still {remainingHoles} hole/insert regions, with a total of {remainingHoleVoxels} voxels.");
            }

            return result.Values.ToList();
        }

        /// <summary>
        /// Takes an input volume and extracts the contours for all voxels that have the given
        /// foreground value.
        /// Contour extraction will take account of holes and inserts, up to the default nesting level.
        /// </summary>
        /// <param name="volume">The input volume.</param>
        /// <param name="foregroundId">The ID we are looking for when extracting contours.</param>
        /// <param name="smoothingType">The type of smoothing that should be applied when going from a
        /// point polygon to a contour.</param>
        /// <param name="maxNestingLevel">The maximum nesting level up to which polygons should be extracted. If set to
        /// 0, only the outermost polygons will be returned. If 1, the outermost polygons and the holes therein.
        /// If 2, the outermost polygon, the holes, and the foreground inside the holes.</param>
        /// <returns>The collection of contours.</returns>
        public static IReadOnlyList<ContourPolygon> ContoursWithHoles(Volume2D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            ContourSmoothingType smoothingType = ContourSmoothingType.Small,
            int maxNestingLevel = DefaultMaxPolygonNestingLevel)
        {
            var polygonPoints = PolygonsWithHoles(volume, foregroundId, maxNestingLevel);
            return polygonPoints
                .Select(x => new ContourPolygon(SmoothPolygon.Smooth(x, smoothingType), x.TotalPixels))
                .ToList();
        }

        /// <summary>
        /// Takes an input volume and extracts the contours for all voxels that have the given
        /// foreground value.
        /// Contour extraction will not take account of holes, and hence only return the outermost
        /// contour around a region of interest.
        /// </summary>
        /// <param name="volume">The input volume.</param>
        /// <param name="foregroundId">The ID we are looking for when extracting contours.</param>
        /// <param name="smoothingType">The type of smoothing that should be applied when going from a
        /// point polygon to a contour.</param>
        /// <returns>The collection of contours.</returns>
        public static IReadOnlyList<ContourPolygon> ContoursFilled(Volume2D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            ContourSmoothingType smoothingType = ContourSmoothingType.Small)
        {
            var polygonPoints = PolygonsFilled(volume, foregroundId);
            return polygonPoints
                .Select(x =>
                {
                    var isCounterClockwise = false;
                    var smoothedPoints = SmoothPolygon.SmoothPoints(x.Points, isCounterClockwise, smoothingType);
                    return new ContourPolygon(smoothedPoints, x.VoxelCounts.Total);
                })
                .ToList();
        }

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
            Volume3D<byte> volume,
            byte foregroundId = ModelConstants.MaskForegroundIntensity,
            SliceType sliceType = SliceType.Axial,
            bool filterEmptyContours = true,
            Region3D<int> regionOfInterest = null,
            ContourSmoothingType axialSmoothingType = ContourSmoothingType.Small)
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
                    smoothingType = ContourSmoothingType.None;
                    break;
                case SliceType.Sagittal:
                    startPoint = region.MinimumX;
                    endPoint = region.MaximumX;
                    smoothingType = ContourSmoothingType.None;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sliceType), sliceType, null);
            }

            var numberOfSlices = endPoint - startPoint + 1;
            var arrayOfContours = new Tuple<int, IReadOnlyList<ContourPolygon>>[numberOfSlices];

            for (var i = 0; i < arrayOfContours.Length; i++)
            {
                var z = startPoint + i;
                var volume2D = ExtractSlice.Slice(volume, sliceType, z);
                var contours = ContoursWithHoles(volume2D, foregroundId, smoothingType);
                arrayOfContours[i] = Tuple.Create(z, contours);
            }

            return new ContoursPerSlice(
                arrayOfContours
                    .Where(x => !filterEmptyContours || x.Item2.Count > 0)
                    .ToDictionary(x => x.Item1, x => x.Item2));
        }

        /// <summary>
        /// Creates volume of the same size as the argument, with pixel values equal to 1 for all
        /// pixels that have the given <paramref name="foregroundId"/>, and pixel value 0 for all others.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundId">The voxel value that should be considered foreground.</param>
        /// <returns></returns>
        private static Volume2D<byte> CreateBinaryVolume(Volume2D<byte> volume, byte foregroundId)
        {
            var binaryVolume = volume.CreateSameSize<byte>();
            var binaryArray = binaryVolume.Array;
            var volumeArray = volume.Array;
            for (var index = 0; index < volumeArray.Length; index++)
            {
                binaryArray[index] = volumeArray[index] == foregroundId ? (byte)1 : (byte)0;
            }

            return binaryVolume;
        }

        /// <summary>
        /// Extracts polygons by walking the edge of the foreground values of the worker volume.
        /// This method will return closed polygons. Also, the polygons will be filled (any holes removed).
        /// The <paramref name="foundPolygons"/> argument will be updated in place, by marking all voxels
        /// that are found to be inside of a polygon with the index of that polygon.
        /// The first new polygon that is found will be given the number supplied in <paramref name="firstNewPolygon"/>
        /// (must be 1 or higher)
        /// </summary>
        /// <param name="binaryVolume">The volume we are extracting polygons from. Must only contain values 0 and 1.</param>
        /// <param name="foundPolygons">A volume that contains the IDs for all polygons that have been found already. 0 means no polygon found.</param>
        /// <param name="searchInsidePolygon">For walking along edges, limit to the voxels that presently are assigned to
        /// the polygon given here.</param>
        /// <param name="isInnerPolygon">If true, the polygon will walk along boundaries around regions with
        /// voxel value 0 (background), but keep the polyon points on voxel values 1, counterclockwises.</param>
        /// <param name="firstNewPolygon">The ID for the first polygon that is found.</param>
        /// <returns>A collection of polygons and there respective sizes (number of foreground points in each polygon)</returns>
        private static Dictionary<ushort, PolygonPoints> ExtractPolygons(
            Volume2D<byte> binaryVolume,
            ushort[] foundPolygons,
            ushort searchInsidePolygon,
            bool isInnerPolygon,
            ushort firstNewPolygon)
        {
            if (binaryVolume == null)
            {
                throw new ArgumentNullException(nameof(binaryVolume));
            }

            if (foundPolygons == null)
            {
                throw new ArgumentNullException(nameof(foundPolygons));
            }

            if (firstNewPolygon < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(firstNewPolygon), "Polygon index 0 is reserved for 'not assigned'");
            }

            var foregroundId = isInnerPolygon ? (byte)0 : (byte)1;
            var polygons = new Dictionary<ushort, PolygonPoints>();
            var dimX = binaryVolume.DimX;
            var dimY = binaryVolume.DimY;
            var volumeArray = binaryVolume.Array;
            for (var y = 0; y < dimY; y++)
            {
                // Manually computing index, rather than relying on GetIndex, brings substantial speedup.
                var offsetY = y * dimX;
                for (var x = 0; x < dimX; x++)
                {
                    var pixelIndex = x + offsetY;

                    // Starting point of a new polygon is where we see the desired foreground in the original
                    // volume, and have either not found any polygon yet (searchInsidePolygon == 0)
                    // or have found a polygon already and now search for the holes inside it.
                    if (volumeArray[pixelIndex] == foregroundId && foundPolygons[pixelIndex] == searchInsidePolygon)
                    {
                        PointInt startPoint;
                        if (isInnerPolygon)
                        {
                            Debug.Assert(y >= 1, "When searching for innner polygons (holes), expecting that there is foreground in the row above.");
                            startPoint = new PointInt(x, y - 1);
                        }
                        else
                        {
                            startPoint = new PointInt(x, y);
                        }

                        VoxelCounts voxelCounts;
                        PointInt[] contourPoints;
                        if (isInnerPolygon)
                        {
                            var innerPoints = FindPolygon(
                                binaryVolume,
                                foundPolygons,
                                searchInsidePolygon,
                                new PointInt(x, y),
                                backgroundId: 1,
                                searchClockwise: true);
                            voxelCounts = FillPolygon.FillPolygonAndCount(
                                innerPoints,
                                foundPolygons,
                                firstNewPolygon,
                                binaryVolume,
                                foregroundId: 0);
                            contourPoints = FindPolygon(
                                binaryVolume,
                                foundPolygons,
                                searchInsidePolygon,
                                startPoint,
                                backgroundId: 0,
                                searchClockwise: false);
                        }
                        else
                        {
                            contourPoints = FindPolygon(
                                binaryVolume,
                                foundPolygons,
                                searchInsidePolygon,
                                startPoint,
                                backgroundId: 0,
                                searchClockwise: true);
                            voxelCounts = FillPolygon.FillPolygonAndCount(
                                contourPoints,
                                foundPolygons,
                                firstNewPolygon,
                                binaryVolume,
                                foregroundId);
                        }

                        var polygon = new PolygonPoints(
                            contourPoints,
                            voxelCounts,
                            searchInsidePolygon,
                            isInside: isInnerPolygon,
                            startPointMinimumY: startPoint);
                        polygons.Add(firstNewPolygon, polygon);
                        firstNewPolygon++;
                    }
                }
            }

            return polygons;
        }

        private static PointInt[] FindPolygon(
            Volume2D<byte> volume,
            ushort[] foundPolygons,
            ushort searchInsidePolygon,
            PointInt start,
            byte backgroundId,
            bool searchClockwise)
        {
            // Clockwise search starts at a point (x, y) where there is no foreground
            // on lines with smaller y. Next point can hence be in direction 0
            // (neighbor x+1,y) or at directions larger than 0.
            // For counterclockwise search, we start with a point that we know is background,
            // and go to the line above (y-1) from that, where we are guaranteed to find foreground.
            // From that point, going in direction hits the point we know is background, and can
            // continue to search in clockwise direction.
            var nextSearchDirection = searchClockwise ? 0 : 2;
            var done = false;
            var current = start;
            var nextPoint = start;
            var contour = new List<PointInt>();
            var dimX = volume.DimX;
            var dimY = volume.DimY;
            var neighbors = Delta.Length;
            var array = volume.Array;

            (int, PointInt) FindNextPoint(int currentX, int currentY, int deltaIndex)
            {
                for (var i = 0; i < 7; i++)
                {
                    var delta = Delta[deltaIndex];
                    var x = currentX + delta.X;
                    var y = currentY + delta.Y;
                    // Manually computing index, rather than relying on GetIndex, brings substantial speedup.
                    var index = x + y * dimX;
                    if (x < 0 || x >= dimX
                        || y < 0 || y >= dimY
                        || array[index] == backgroundId
                        || foundPolygons[index] != searchInsidePolygon)
                    {
                        deltaIndex = (deltaIndex + 1) % neighbors;
                    }
                    else
                    {
                        // found non-background pixel
                        return (deltaIndex, new PointInt(x, y));
                    }
                }
                return (deltaIndex, new PointInt(currentX, currentY));
            }

            while (!done)
            {
                contour.Add(current);
                (nextSearchDirection, nextPoint) = FindNextPoint(
                    current.X,
                    current.Y,
                    nextSearchDirection);

                // Delta positions for search of the next neighbor are specified in clockwise order,
                // starting with the point (x+1,y). After finding a neighbor, reset those by going
                // "back" two increments.
                // Because of % 8, going back by 2 for clockwise search amounts to going forward by 6.
                nextSearchDirection = (nextSearchDirection + 6) % neighbors;

                // Terminate when we are back at the starting position.
                done = nextPoint == start;
                current = nextPoint;
            }

            return contour.ToArray();
        }
    }
}