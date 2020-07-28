///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using PointInt = System.Drawing.Point;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    public class ExtractPolygonHelpers
    {
        /// <summary>
        /// The deltas from a center pixel in 2D.
        /// </summary>
        private static readonly int[,] Delta = { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 } };

        /// <summary>
        /// Takes an input volume and extracts the contours. This does not modify the input volume.
        /// </summary>
        /// <param name="volume">The input volume.</param>
        /// <param name="id">The ID we are looking for when extracting contours.</param>
        /// <param name="bgId">The background ID.</param>
        /// <returns>The collection of contours.</returns>
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static IList<Tuple<int, PointInt[]>> ExtractPolygonPointsInMask(Volume2D<byte> volume, 
            byte id = 1, 
            byte bgId = 0)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            // Create a copy of the volume slice in ushort so will can fill more than 255 contours
            var workerVolumeSlice = new ushort[volume.Length];

            for (var i = 0; i < volume.Length; i++)
            {
                workerVolumeSlice[i] = volume[i] == id ? id : bgId;
            }

            return ExtractPolygons(workerVolumeSlice, volume.DimX, volume.DimY, id, bgId);
        }

        /// <summary>
        /// Takes the input volume and returns the list of integer points that make up the border of the found polygons.
        /// Returns tuples (number of points in polygon, polygon)
        /// </summary>
        /// <param name="volume">The input volume.</param>
        /// <param name="dimX">The X dimension of the input array.</param>
        /// <param name="dimY">The Y dimension of the input array.</param>
        /// <param name="sliceZ">The Z dimension we are extracting contours from.</param>
        /// <param name="id">The foreground ID.</param>
        /// <param name="bgId">The background ID.</param>
        /// <returns>The array of contours found in the mask.</returns>
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static IList<Tuple<int, PointInt[]>> ExtractPolygonPointsInMask(byte[] volume, 
            int dimX, 
            int dimY, 
            int sliceZ, 
            byte id = 1, 
            byte bgId = 0)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            var dimXy = dimX * dimY;

            // Create a copy of the volume slice in ushort so will can fill more than 255 contours
            var workerVolumeSlice = new ushort[dimXy];

            // Note we are converting from a potential 3D volume to a 2D slice
            for (var y = 0; y < dimY; y++)
            {
                var zyDimX = y * dimX + sliceZ * dimXy;
                var yDimX = y * dimX;
                
                for (var x = 0; x < dimX; x++)
                {
                    workerVolumeSlice[yDimX + x] = volume[zyDimX + x] == id ? id : bgId;
                }
            }

            return ExtractPolygons(workerVolumeSlice, dimX, dimY, id, bgId);
        }

        /// <summary>
        /// Takes an input volume and extracts the contours. This does not modify the input volume.
        /// </summary>
        /// <param name="volume">The input volume.</param>
        /// <param name="id">The ID we are looking for when extracting contours.</param>
        /// <param name="bgId">The background ID.</param>
        /// <returns>The collection of contours.</returns>
        [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
        public static IList<Contour> ExtractPolygon(Volume2D<byte> volume, byte id = 1, byte bgId = 0, SmoothingType smoothingType = SmoothingType.Small)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            // Create a copy of the volume slice in ushort so will can fill more than 255 contours
            var workerVolumeSlice = new ushort[volume.Length];

            for (var i = 0; i < volume.Length; i++)
            {
                workerVolumeSlice[i] = volume[i] == id ? id : bgId;
            }

            var contours = ExtractPolygons(workerVolumeSlice, volume.DimX, volume.DimY, id, bgId);

            // NOTE: We convert the points to the outer edge and smooth
            return contours.Select(x => new Contour(SmoothPolygonHelpers.SmoothPolygon(x.Item2, smoothingType), x.Item1)).ToList();
        }

        /// <summary>
        /// Extracts polygons by walking the edge of the foreground values of the worker volume.
        /// This method will return closed polygons. Also, the polygons will be filled (any holes removed).
        /// </summary>
        /// <param name="workerVolumeSlice">The worker volume we are extracting polygons from. This must only contain ID and BG ID</param>
        /// <param name="dimX">The X-dimension of the slice.</param>
        /// <param name="dimY">The Y-dimension of the slice.</param>
        /// <param name="id">The foreground ID.</param>
        /// <param name="bgId">The background ID.</param>
        /// <returns>A collection of polygons and there respective sizes (number of foreground points in each polygon)</returns>
        private static List<Tuple<int, PointInt[]>> ExtractPolygons(ushort[] workerVolumeSlice, 
            int dimX, 
            int dimY, 
            byte id, 
            byte bgId)
        {
            if (workerVolumeSlice == null)
            {
                throw new ArgumentNullException(nameof(workerVolumeSlice));
            }

            if (id == bgId)
            {
                throw new ArgumentException("The foreground ID cannot be the same as the background ID.");
            }

            var region = (ushort)(id + 1);
            var polygons = new List<Tuple<int, PointInt[]>>();

            for (var y = 0; y < dimY; y++)
            {
                var yDimX = y * dimX;

                for (var x = 0; x < dimX; x++)
                {
                    if (workerVolumeSlice[x + yDimX] == id)
                    {
                        // We used to extract the minimum polygon (full contour bool flag). We now must always extract the full contour
                        // so we can convert the contour into the outer edge using the ClockwisePointsToExternalPathWindowsPoints method.
                        var contourPoints = FindPolygon(workerVolumeSlice, dimX, dimY, x, y, region, bgId);
                        var regionAreaPixels = FillPolygonHelpers.FillPolygon(contourPoints, workerVolumeSlice, dimX, dimY, 0, 0, region);
                        polygons.Add(Tuple.Create(regionAreaPixels, contourPoints));
                        region++;
                    }
                }
            }

            return polygons;
        }

        private static PointInt[] FindPolygon(ushort[] result, int dimX, int dimY, int x, int y, ushort regionId, byte backgroundId)
        {
            var contour = new LinkedList<PointInt>();
            contour.AddLast(new PointInt(x, y));

            int xT, yT; // T = successor of starting point (xS,yS)
            var dNext = FindNextPoint(result, dimX, dimY, backgroundId, x, y, 0);
            var dir = dNext.Item1;
            var pt = dNext.Item2;

            contour.AddLast(pt);

            var xC = xT = pt.X;
            var yC = yT = pt.Y;

            // true if isolated pixel, we dont consider isolated pixels
            var done = (x == xT && y == yT);

            if (done)
            {
                return contour.ToArray();
            }

            while (!done)
            {
                var index = xC + yC * dimX;
                result[index] = regionId;

                var dSearch = (dir + 6) % 8;

                dNext = FindNextPoint(result, dimX, dimY, backgroundId, xC, yC, dSearch);

                dir = dNext.Item1;
                pt = dNext.Item2;

                var xP = xC;
                var yP = yC;

                xC = pt.X;
                yC = pt.Y;

                // are we back at the starting position?
                done = (xP == x && yP == y && xC == xT && yC == yT);

                if (!done)
                {
                    contour.AddLast(pt);
                }
                else
                {
                    // If the last point equals the first lets remove, and return the result.
                    contour.RemoveLast();
                }
            }

            return contour.ToArray();
        }

        private static Tuple<int, PointInt> FindNextPoint(ushort[] result, int dimX, int dimY, ushort backgroundId, int currentX, int currentY, int deltaIndex)
        {
            for (var i = 0; i < 7; i++)
            {
                var x = currentX + Delta[deltaIndex, 0];
                var y = currentY + Delta[deltaIndex, 1];

                var index = x + y * dimX;

                if (x < 0 || x >= dimX || y < 0 || y >= dimY || result[index] == backgroundId)
                {
                    deltaIndex = (deltaIndex + 1) % 8;
                }
                else
                {
                    // found non-background pixel
                    return Tuple.Create(deltaIndex, new PointInt(x, y));
                }
            }

            return Tuple.Create(deltaIndex, new PointInt(currentX, currentY));
        }
    }
}