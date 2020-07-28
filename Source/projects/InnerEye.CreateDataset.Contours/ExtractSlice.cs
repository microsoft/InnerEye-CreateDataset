///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System.Threading.Tasks;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Contains methods to extract slices from volumes.
    /// </summary>
    public static class ExtractSlice
    {
        /// <summary>
        /// Creates an empty <see cref="Volume2D"/> that is sized such that it can hold a slice of the
        /// present volume, when the slice is of given type (orientation). 
        /// Note that these slices are NOT extracted
        /// according to the patient-centric coordinate system, but in the coordinate system of the volume
        /// alone.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TK"></typeparam>
        /// <param name="volume">The volume from which the slice should be extracted.</param>
        /// <param name="sliceType">The type (orientation) of the slice that should be extracted.</param>
        /// <returns></returns>
        public static Volume2D<TK> AllocateSlice<T, TK>(Volume3D<T> volume, SliceType sliceType)
        {
            var width = 0;
            var height = 0;

            var spacingX = 0d;
            var spacingY = 0d;

            var origin = new Point2D(0, 0);
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

        /// <summary>
        /// Extracts a slice of a chosen type (orientation) from the present volume, and returns it as a
        /// <see cref="Volume2D"/> instance of correct size. Note that these slices are NOT extracted
        /// according to the patient-centric coordinate system, but in the coordinate system of the volume
        /// alone.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume"></param>
        /// <param name="sliceType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Volume2D<T> Slice<T>(Volume3D<T> volume, SliceType sliceType, int index)
        {
            var result = AllocateSlice<T, T>(volume, sliceType);
            if (result != null)
            {
                Extract(volume, sliceType, index, result.Array);
            }

            return result;
        }

        /// <summary>
        /// Extracts a slice of a given type (orientation) from the present volume, and writes it to
        /// the provided array in <paramref name="outVolume"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume"></param>
        /// <param name="sliceType"></param>
        /// <param name="sliceIndex"></param>
        /// <param name="outVolume"></param>
        /// <param name="skip"></param>
        private static void Extract<T>(Volume3D<T> volume, SliceType sliceType, int sliceIndex, T[] outVolume, int skip = 1)
        {
            switch (sliceType)
            {
                case SliceType.Axial:
                    if (sliceIndex < volume.DimZ && outVolume.Length == volume.DimXY * skip)
                    {
                        Parallel.For(0, volume.DimY, y =>
                        {
                            for (var x = 0; x < volume.DimX; x++)
                            {
                                outVolume[(x + y * volume.DimX) * skip] = volume[(sliceIndex * volume.DimY + y) * volume.DimX + x];
                            }
                        });
                    }

                    break;
                case SliceType.Coronal:
                    if (sliceIndex < volume.DimY && outVolume.Length == volume.DimZ * volume.DimX * skip)
                    {
                        Parallel.For(0, volume.DimZ, z =>
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
                        Parallel.For(0, volume.DimZ, z =>
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
    }
}
