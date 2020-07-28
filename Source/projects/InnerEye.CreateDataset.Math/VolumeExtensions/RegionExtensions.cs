///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

// ReSharper disable once CheckNamespace
namespace InnerEye.CreateDataset.Math
{
    using InnerEye.CreateDataset.Volumes;
    using System;

    public static class RegionExtensions
    {
        public static Region3D<int> Dilate<T>(this Region3D<int> region, Volume3D<T> volume, double mmDilationX, double mmDilationY, double mmDilationZ)
        {
            if (region.IsEmpty())
            {
                return region.Clone();
            }
            var dilatedMinimumX = region.MinimumX - (int)Math.Ceiling(mmDilationX / volume.SpacingX);
            var dilatedMaximumX = region.MaximumX + (int)Math.Ceiling(mmDilationX / volume.SpacingX);

            var dilatedMinimumY = region.MinimumY - (int)Math.Ceiling(mmDilationY / volume.SpacingY);
            var dilatedMaximumY = region.MaximumY + (int)Math.Ceiling(mmDilationY / volume.SpacingY);

            var dilatedMinimumZ = region.MinimumZ - (int)Math.Ceiling(mmDilationZ / volume.SpacingZ);
            var dilatedMaximumZ = region.MaximumZ + (int)Math.Ceiling(mmDilationZ / volume.SpacingZ);

            return new Region3D<int>(
                dilatedMinimumX < 0 ? 0 : dilatedMinimumX,
                dilatedMinimumY < 0 ? 0 : dilatedMinimumY,
                dilatedMinimumZ < 0 ? 0 : dilatedMinimumZ,
                dilatedMaximumX >= volume.DimX ? volume.DimX - 1 : dilatedMaximumX,
                dilatedMaximumY >= volume.DimY ? volume.DimY - 1 : dilatedMaximumY,
                dilatedMaximumZ >= volume.DimZ ? volume.DimZ - 1 : dilatedMaximumZ);
        }

        /// <summary>
        /// Gets the number of points in the region, that is the product of the 
        /// region length across the three dimensions.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static int Size(this Region3D<int> region)
        {
            return region.LengthX() * region.LengthY() * region.LengthZ();
        }

        /// <summary>
        /// Gets the length of the region along the X dimensions. The length
        /// of the region is the number of points (integers) that are included
        /// in the region. If the region has Minimum of 4, and Maximum of 5,
        /// the length is 2 (Minimum and Maximum are inclusive).
        /// </summary>
        /// <param name="region3D"></param>
        /// <returns></returns>
        public static int LengthX(this Region3D<int> region3D)
        {
            var length = region3D.MaximumX - region3D.MinimumX + 1;
            return length < 0 ? 0 : length;
        }

        /// <summary>
        /// Gets the length of the region along the Y dimensions. The length
        /// of the region is the number of points (integers) that are included
        /// in the region. If the region has Minimum of 4, and Maximum of 5,
        /// the length is 2 (Minimum and Maximum are inclusive).
        /// </summary>
        /// <param name="region3D"></param>
        /// <returns></returns>
        public static int LengthY(this Region3D<int> region3D)
        {
            var length = region3D.MaximumY - region3D.MinimumY + 1;
            return length < 0 ? 0 : length;
        }

        /// <summary>
        /// Gets the length of the region along the Z dimensions. The length
        /// of the region is the number of points (integers) that are included
        /// in the region. If the region has Minimum of 4, and Maximum of 5,
        /// the length is 2 (Minimum and Maximum are inclusive).
        /// </summary>
        /// <param name="region3D"></param>
        /// <returns></returns>
        public static int LengthZ(this Region3D<int> region3D)
        {
            var length = region3D.MaximumZ - region3D.MinimumZ + 1;
            return length < 0 ? 0 : length;
        }

        /// <summary>
        /// Gets whether the region contains zero voxels. That is the case if, for any of the
        /// dimensions X, Y, Z, the minimum is larger than the maximum.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static bool IsEmpty(this Region3D<int> region)
        {
            return region.MinimumX > region.MaximumX || 
                   region.MinimumY > region.MaximumY ||
                   region.MinimumZ > region.MaximumZ;
        }

        /// <summary>
        /// Creates a new region that contains no voxels (that is, <see cref="IsEmpty(Region3D{int})"/> returns true).
        /// The boundaries of that region are such that they would not lead to numerical problems if accidentially doing
        /// numerical operations on that empty region.
        /// </summary>
        /// <returns></returns>
        public static Region3D<int> EmptyIntRegion()
        {
            return new Region3D<int>(0, 0, 0, -1, -1, -1);
        }

        /// <summary>
        /// Gets whether a point with the given (x, y, z) coordinates is inside the region.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static bool ContainsPoint(this Region3D<int> region, int x, int y, int z)
        {
            return (x >= region.MinimumX && x <= region.MaximumX
                    && y >= region.MinimumY && y <= region.MaximumY
                    && z >= region.MinimumZ && z <= region.MaximumZ);
        }

        /// <summary>
        /// Gets whether the present object is inside of the given <paramref name="outer"/> 
        /// region. If the present object is at the boundaries along any of the edges,
        /// this is still considered inside.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        public static bool InsideOf(this Region3D<int> region, Region3D<int> outer)
        {
            if (region.IsEmpty())
            {
                throw new ArgumentException("This operation can only be computed on non-empty regions.", nameof(region));
            }

            if (outer.IsEmpty())
            {
                throw new ArgumentException("This operation can only be computed on non-empty regions.", nameof(outer));
            }

            return region.MinimumX >= outer.MinimumX
                && region.MaximumX <= outer.MaximumX
                && region.MinimumY >= outer.MinimumY
                && region.MaximumY <= outer.MaximumY
                && region.MinimumZ >= outer.MinimumZ
                && region.MaximumZ <= outer.MaximumZ;
        }
    }
}
