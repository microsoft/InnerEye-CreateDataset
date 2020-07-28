///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using MedLib.IO;
    using InnerEye.CreateDataset.Contours;
    using InnerEye.CreateDataset.ImageProcessing;
    using InnerEye.CreateDataset.Volumes;

    public static partial class Converters
    {
        /// <summary>
        /// Converts a Float32 value to an Int16 value, if it is in the correct range for Int16, and if
        /// they appear to contain integer values after rounding to 5 digits. An input value of 1.0000001 would be considered
        /// OK, and converted to (short)1. If the value is outside the Int16 range, or appears to be a fractional
        /// value, throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="index">The index at which the value was found. This is only used for creating error messages.</param>
        /// <returns></returns>
        public static short TryConvertToInt16(float value, int index = -1)
        {
            if (value < short.MinValue || value > short.MaxValue)
            {
                var position = index >= 0 ? $"at index { index}" : string.Empty;
                throw new ArgumentException($"The input image contains voxel values outside the range of Int16: Found value {value} {position}");
            }
            var rounded = Math.Round(value, 5);
            if (rounded != (short)rounded)
            {
                var position = index >= 0 ? $"at index { index}" : string.Empty;
                throw new ArgumentException($"The input image contains voxel values that are not integers after rounding to 5 decimals: Found value {value} {position}");
            }
            return (short)rounded;
        }

        /// <summary>
        /// Converts a probability with values between 0 and 1.0, to a byte with values between 0 and 255.
        /// An input voxel value of 1.0 would be mapped to 255 in the output.
        /// Any values at or below 0.0 would become 0, anything exceeding 1.0 in the input will become 255.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte PosteriorToByte(float value)
        {
            var val = value * 255.0;
            return (val <= 0) ? (byte)0 : (val >= 255) ? (byte)255 : (byte)Math.Round(val);
        }
    }

    public static class VolumeExtensions
    {
        /// <summary>
        /// Gets the X,Y,Z location coordinates for a given index positions
        /// </summary>
        public static Index3D GetCoordinates<T>(this Volume3D<T> image, int index)
        {
            var z = index / image.DimXY;
            var rem = index - z * image.DimXY;
            var y = rem / image.DimX;
            var x = rem - y * image.DimX;
            return new Index3D((x, y, z), index);
        }

        /// <summary>
        /// Gets the sequence of voxel with their values in an image, that fall inside the mask (that is, the 
        /// mask value that corresponds to the voxels is not equal to zero). Returns all voxel values
        /// if the mask is null.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <param name="mask">The binary mask that specifies which voxels of the image should be returned.
        /// Can be null.</param>
        /// <returns></returns>
        public static IEnumerable<T> VoxelsInsideMask<T>(this Volume3D<T> image, Volume3D<byte> mask)
        {
            var imageArray = (image ?? throw new ArgumentNullException(nameof(image))).Array;
            var maskArray = mask?.Array;
            return
                maskArray != null
                ? VoxelsInsideMaskWhenImageAndMaskAreNotNull(imageArray, maskArray)
                : image.Array;
        }

        /// <summary>
        /// Gets the sequence of voxel with their values and X,Y,Z location in an image, that fall inside the mask (that is, the 
        /// mask value that corresponds to the voxels is not equal to zero). Returns all voxel values
        /// if the mask is null.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <param name="mask">The binary mask that specifies which voxels of the image should be returned.
        /// Can be null.</param>
        /// <returns></returns>
        public static IEnumerable<(Index3D coordinates, T value)> VoxelsInsideMaskWithCoordinates<T>(this Volume3D<T> image, Volume3D<byte> mask)
        {
            var imageArray = (image ?? throw new ArgumentNullException(nameof(image))).Array;
            if (mask == null)
            {
                for (var index = 0; index < imageArray.Length; index++)
                {
                    yield return (image.GetCoordinates(index), imageArray[index]);
                }
            }
            else
            {
                var imageLength = imageArray.Length;
                var maskArray = mask.Array;
                if (maskArray.Length != imageLength)
                {
                    throw new ArgumentException("The image and the mask must have the same number of voxels.", nameof(mask));
                }

                // Plain vanilla loop is the fastest way of iterating, 4x faster than Array.Indices()
                for (var index = 0; index < imageLength; index++)
                {
                    if (maskArray[index] > 0)
                    {
                        yield return (image.GetCoordinates(index), imageArray[index]);
                    }
                }
            }
        }

        /// <summary>
        /// Converts the given volume to Nifti format, using the given compression method.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        public static byte[] SerializeToNiftiBytes(this Volume3D<short> volume, NiftiCompression compression)
        {
            using (var memoryStream = new MemoryStream())
            {
                NiftiIO.WriteToStream(memoryStream, volume, compression);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Converts the given volume to Nifti format, using the given compression method.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        public static byte[] SerializeToNiftiBytes(this Volume3D<byte> volume, NiftiCompression compression)
        {
            using (var memoryStream = new MemoryStream())
            {
                NiftiIO.WriteToStream(memoryStream, volume, compression);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Helper method to iterate slice wize in parallel ie: ((z,y,x) tuples over the volume dimensions) iterate over a volume and execute an method
        /// </summary>
        /// <param name="volume">The volume to be iterated</param>
        /// <param name="action">The action to be invoked upon each iteration</param>
        /// <param name="options">options to control parallelisation</param>
        /// <returns></returns>
        public static void ParallelIterateSlices<T>(this Volume3D<T> volume, Action<(int x, int y, int z)> action, ParallelOptions options = null)
        {
            Parallel.For(0, volume.DimZ, options ?? new ParallelOptions(), z =>
            {
                for (var y = 0; y < volume.DimY; y++)
                {
                    for (var x = 0; x < volume.DimX; x++)
                    {
                        action((x, y, z));
                    }
                }
            });
        }

        /// <summary>
        /// Helper method to iterate slice wize ie: ((z,y,x) tuples over the volume dimensions) iterate over a volume and execute an method
        /// </summary>
        /// <param name="volume">The volume to be iterated</param>
        /// <param name="action">The action to be invoked upon each iteration</param>
        /// <returns></returns>
        public static void IterateSlices<T>(this Volume3D<T> volume, Action<(int x, int y, int z)> action)
        {
            ParallelIterateSlices(volume, action, new ParallelOptions { MaxDegreeOfParallelism = 1 });
        }

        /// <summary>
        /// Creates a Volume3D instance from individual slices (XY planes).
        /// This is intended for testing only: The resulting Volume3D will have
        /// information like Origin set to their default values.
        /// </summary>
        /// <param name="dimX">The DimX property of the returned object.</param>
        /// <param name="dimY">The DimY property of the returned object.</param>
        /// <param name="slices">A list of individual slices. slices[i] will be used as z=i.</param>
        /// <returns></returns>
        public static Volume3D<T> FromSlices<T>(int dimX, int dimY, IReadOnlyList<IReadOnlyList<T>> slices)
        {
            var expectedSize = dimX * dimY;
            if (slices == null || slices.Count == 0)
            {
                throw new ArgumentException("Slice list must not be empty", nameof(slices));
            }
            if (slices.Any(slice => slice.Count != expectedSize))
            {
                throw new ArgumentException($"Each slice must match dimensions and have length {expectedSize}", nameof(slices));
            }
            var m = new Volume3D<T>(dimX, dimY, slices.Count);
            for (var z = 0; z < slices.Count; z++)
            {
                Array.Copy(slices[z].ToArray(), 0, m.Array, z * m.DimXY, m.DimXY);
            }
            return m;
        }

        public static IEnumerable<byte> GetAllValuesInVolume(this Volume3D<byte> volume)
        {
            var values = new int[256];

            for (var i = 0; i < volume.Length; i++)
            {
                values[volume[i]]++;
            }

            var result = new List<byte>();

            // Start at 1 as 0 is background
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] > 0)
                {
                    result.Add((byte)i);
                }
            }

            return result;
        }

        private static float[] GetSigmasForConvolution<T>(this Volume3D<T> image, float sigma)
        {
            return new float[] {
                (float)(sigma / image.SpacingX),
                (float)(sigma / image.SpacingY),
                (float)(sigma / image.SpacingZ)};
        }

        private static Direction[] GetDirectionsForConvolution()
        {
            return new Direction[] { Direction.DirectionX, Direction.DirectionY, Direction.DirectionZ };
        }

        /// <summary>
        /// Runs Gaussian smoothing on the given volume in-place.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sigma"></param>
        public static void SmoothInPlace(this Volume3D<byte> image, float sigma)
        {
            Convolution.Convolve(image.Array, image.DimX, image.DimY, image.DimZ,
                GetDirectionsForConvolution(), image.GetSigmasForConvolution(sigma));
        }

        /// <summary>
        /// Runs Gaussian smoothing on the given volume in-place.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="sigma"></param>
        public static void SmoothInPlace(this Volume3D<float> image, float sigma)
        {
            Convolution.Convolve(image.Array, image.DimX, image.DimY, image.DimZ,
                GetDirectionsForConvolution(), image.GetSigmasForConvolution(sigma));
        }

        public static Volume3D<byte> SmoothedImage(this Volume3D<byte> image, double sigma)
        {
            var output = image.Copy();
            Convolution.Convolve(output.Array, output.DimX, output.DimY, output.DimZ,
                GetDirectionsForConvolution(), output.GetSigmasForConvolution((float)sigma));
            return output;
        }

        public static Volume3D<float> SmoothedImage(this Volume3D<float> image, double sigma)
        {
            var output = image.Copy();
            Convolution.Convolve(output.Array, output.DimX, output.DimY, output.DimZ,
                GetDirectionsForConvolution(), output.GetSigmasForConvolution((float)sigma));
            return output;
        }

        /// <summary>
        /// Gets the region of the volume that contains all non-zero voxel values.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Region3D<int> GetInterestRegion(this Volume3D<byte> volume)
        {
            var minimumX = new int[volume.DimZ];
            var minimumY = new int[volume.DimZ];
            var minimumZ = new int[volume.DimZ];
            var maximumX = new int[volume.DimZ];
            var maximumY = new int[volume.DimZ];
            var maximumZ = new int[volume.DimZ];

            Parallel.For(0, volume.DimZ, delegate (int z)
            {
                minimumX[z] = int.MaxValue;
                minimumY[z] = int.MaxValue;
                minimumZ[z] = int.MaxValue;
                maximumX[z] = -int.MaxValue;
                maximumY[z] = -int.MaxValue;
                maximumZ[z] = -int.MaxValue;

                for (var y = 0; y < volume.DimY; y++)
                {
                    for (var x = 0; x < volume.DimX; x++)
                    {
                        if (volume[x + y * volume.DimX + z * volume.DimXY] <= 0)
                        {
                            continue;
                        }

                        if (x < minimumX[z])
                        {
                            minimumX[z] = x;
                        }

                        if (x > maximumX[z])
                        {
                            maximumX[z] = x;
                        }

                        if (y < minimumY[z])
                        {
                            minimumY[z] = y;
                        }

                        if (y > maximumY[z])
                        {
                            maximumY[z] = y;
                        }

                        if (z < minimumZ[z])
                        {
                            minimumZ[z] = z;
                        }

                        maximumZ[z] = z;
                    }
                }
            });

            var region = new Region3D<int>(minimumX.Min(), minimumY.Min(), minimumZ.Min(), maximumX.Max(), maximumY.Max(), maximumZ.Max());

            // If no foreground values are found, the region minimum will be Int.MaxValue, maximum will be Int.MinValue.
            // When accidentally doing operations on that region, it will most likely lead to numerical
            // overflow or underflow. Instead, return an empty region that has less troublesome boundary values.
            return region.IsEmpty() ? RegionExtensions.EmptyIntRegion() : region;
        }

        public static Image ToImage(this Volume2D<byte> volume)
        {
            var result = new Bitmap(volume.DimX, volume.DimY);

            for (var y = 0; y < volume.DimY; y++)
            {
                for (var x = 0; x < volume.DimX; x++)
                {
                    var colorValue = volume[x, y] == 0 ? 255 : 0;
                    result.SetPixel(x, y, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            return result;
        }

        public static Image ToImage(this Volume2D<float> volume)
        {
            var result = new Bitmap(volume.DimX, volume.DimY);

            for (var y = 0; y < volume.DimY; y++)
            {
                for (var x = 0; x < volume.DimX; x++)
                {
                    var colorValue = (int)volume[x, y] == 0 ? 255 : 0;
                    result.SetPixel(x, y, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            return result;
        }

        public static Image ToImage(this Volume2D<double> volume)
        {
            var result = new Bitmap(volume.DimX, volume.DimY);

            for (var y = 0; y < volume.DimY; y++)
            {
                for (var x = 0; x < volume.DimX; x++)
                {
                    var colorValue = (int)volume[x, y] == 0 ? 255 : 0;
                    result.SetPixel(x, y, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            return result;
        }

        public static Volume2D<byte> ToVolume(this Bitmap image)
        {
            return new Volume2D<byte>(image.ToByteArray(), image.Width, image.Height, 1, 1, new Point2D(), Matrix2.CreateIdentity());
        }

        /// <summary>
        /// Creates a PNG image file from the given volume. Specific voxel values in the volume are mapped
        /// to fixed colors in the PNG file, as per the given mapping.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="filePath"></param>
        /// <param name="voxelMapping"></param>
        public static void SaveVolumeToPng(this Volume2D<byte> mask, string filePath,
            IDictionary<byte, Color> voxelMapping,
            Color? defaultColor = null)
        {
            var width = mask.DimX;
            var height = mask.DimY;

            var image = new Bitmap(width, height);

            CreateFolderStructureIfNotExists(filePath);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var maskValue = mask[x, y];
                    if (!voxelMapping.TryGetValue(maskValue, out var color))
                    {
                        color = defaultColor ?? throw new ArgumentException($"The voxel-to-color mapping does not contain an entry for value {maskValue} found at point ({x}, {y}), and no default color is set.", nameof(voxelMapping));
                    }

                    image.SetPixel(x, y, color);
                }
            }

            image.Save(filePath);
        }

        /// <summary>
        /// Creates a PNG image file from the given binary mask. Value 0 is plotted as white, 
        /// value 1 as black. If the mask contains other values, an exception is thrown.
        /// </summary>
        /// <param name="brushVolume"></param>
        /// <param name="filePath"></param>
        public static void SaveBinaryMaskToPng(this Volume2D<byte> mask, string filePath)
        {
            var voxelMapping = new Dictionary<byte, Color>
            {
                {0, Color.White },
                {1, Color.Black }
            };
            SaveVolumeToPng(mask, filePath, voxelMapping);
        }

        /// <summary>
        /// Creates a PNG image file from the given volume. Specific voxel values in the volume are mapped
        /// to fixed colors in the PNG file:
        /// Background (value 0) is plotted in Red
        /// Foreground (value 1) is Green
        /// Value 2 is Orange
        /// Value 3 is MediumAquamarine
        /// All other voxel values are plotted in Blue.
        /// </summary>
        /// <param name="brushVolume"></param>
        /// <param name="filePath"></param>
        public static void SaveBrushVolumeToPng(this Volume2D<byte> brushVolume, string filePath)
        {
            const byte fg = 1;
            const byte bg = 0;
            const byte bfg = 3;
            const byte bbg = 2;
            var voxelMapping = new Dictionary<byte, Color>
            {
                { fg, Color.Green },
                { bfg, Color.MediumAquamarine },
                { bg, Color.Red },
                { bbg, Color.Orange }
            };
            SaveVolumeToPng(brushVolume, filePath, voxelMapping, Color.Blue);
        }

        // DO NOT USE ONLY FOR DEBUGGING PNGS
        private static Tuple<float, float> MinMaxFloat(float[] volume)
        {
            var max = float.MinValue;
            var min = float.MaxValue;

            for (var i = 0; i < volume.Length; i++)
            {
                var value = volume[i];

                if (Math.Abs(value - short.MinValue) < 1 || Math.Abs(value - short.MaxValue) < 1)
                {
                    continue;
                }

                if (max < value)
                {
                    max = value;
                }

                if (min > value)
                {
                    min = value;
                }
            }

            return Tuple.Create(min, max);
        }

        public static void SaveDistanceVolumeToPng(this Volume2D<float> distanceVolume, string filePath)
        {
            var width = distanceVolume.DimX;
            var height = distanceVolume.DimY;
            var image = new Bitmap(distanceVolume.DimX, distanceVolume.DimY);

            CreateFolderStructureIfNotExists(filePath);

            var minMax = MinMaxFloat(distanceVolume.Array);

            var minimum = minMax.Item1;
            var maximum = minMax.Item2;
            float extval = Math.Min(Math.Min(Math.Abs(minimum), maximum), 3000);

            if (minimum >= 0)
            {
                extval = maximum;
            }
            else if (maximum <= 0)
            {
                extval = Math.Abs(minimum);
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var currentDistanceValue = distanceVolume[x, y];

                    if (currentDistanceValue < -extval) currentDistanceValue = -extval;
                    if (currentDistanceValue > extval) currentDistanceValue = extval;

                    float alpha = (currentDistanceValue - (-extval)) / (2 * extval);

                    float R, G, B;

                    R = 255 * alpha;
                    G = 255 * (1 - alpha);
                    B = 255 * (float)(1 - Math.Abs(alpha - 0.5) * 2);

                    Color color = Color.FromArgb(255, (byte)R, (byte)G, (byte)B);

                    // Background (color intensity for red)
                    if (currentDistanceValue < short.MinValue)
                    {
                        color = Color.Orange;
                    }
                    else if (currentDistanceValue > short.MaxValue)
                    {
                        color = Color.HotPink;
                    }
                    else if ((int)currentDistanceValue == 0)
                    {
                        color = Color.Yellow;
                    }
                    image.SetPixel(x, y, color);
                }
            }

            image.Save(filePath);
        }

        public static byte[] ToByteArray(this Bitmap image)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var result = new byte[imageWidth * imageHeight];

            var bitmapData = image.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadWrite,
                image.PixelFormat);

            var stride = bitmapData.Stride / imageWidth;

            var pixelData = new byte[Math.Abs(bitmapData.Stride) * imageHeight];

            // Copy the values into the array.
            Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);

            Parallel.For(0, imageHeight, delegate (int y)
            {
                for (var x = 0; x < imageWidth; x++)
                {
                    if (pixelData[y * bitmapData.Stride + (x * stride)] == 0)
                    {
                        result[x + y * imageWidth] = 1;
                    }
                }
            });

            image.UnlockBits(bitmapData);

            return result;
        }

        public static Volume2D<TK> AllocateStorage<T, TK>(this Volume2D<T> volume)
        {
            return new Volume2D<TK>(volume.DimX, volume.DimY, volume.SpacingX, volume.SpacingY, volume.Origin, volume.Direction);
        }

        public static Volume2D<ushort> ToUShortVolume2D(this Volume2D<byte> volume)
        {
            var result = volume.AllocateStorage<byte, ushort>();

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = volume[i];
            }

            return result;
        }

        public static Volume2D<T> Duplicate<T>(this Volume2D<T> volume)
        {
            var result = volume.AllocateStorage<T, T>();

            Array.Copy(volume.Array, result.Array, volume.Array.Length);

            return result;
        }

        // Converts a volume2d into a 3d by creating one slice in Z
        public static Volume3D<T> ToVolume3DInXY<T>(this Volume2D<T> volume, int numberOfZSlices = 1)
        {
            var result = new Volume3D<T>(volume.DimX, volume.DimY, numberOfZSlices, volume.SpacingX, volume.SpacingY, 1);
            Array.Copy(volume.Array, result.Array, volume.Array.Length);
            return result;
        }

        public static Volume2D<T> Duplicate<T>(this ReadOnlyVolume2D<T> volume)
        {
            var result = volume.AllocateStorage<T, T>();

            for (int i = 0; i < volume.Length; i++)
            {
                result[i] = volume[i];
            }

            return result;
        }

        /// <summary>
        /// Returns the Point3D corresponding to the specified index in the specified volume.
        /// Does not check that the index is in range.
        /// </summary>
        public static Point3D GetPoint3D<T>(this Volume3D<T> volume, int index)
        {
            int indexXY, refX, refY, refZ;
            refZ = Math.DivRem(index, volume.DimXY, out indexXY);
            refY = Math.DivRem(indexXY, volume.DimX, out refX);
            return new Point3D(refX, refY, refZ);
        }

        /// <summary>
        /// Sets x, y and z to the coordinates represented by index in volume. Does not check that the index is in range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetXYZ<T>(this Volume3D<T> volume, int index, out int x, out int y, out int z)
        {
            int indexXY;
            z = Math.DivRem(index, volume.DimXY, out indexXY);
            y = Math.DivRem(indexXY, volume.DimX, out x);
        }

        public static void ComputeBinaryMask(this Volume3D<float> computationDistanceVolume, Volume3D<byte> output, float cutLevel = 0, byte foreground = 1, byte background = 0)
        {
            Parallel.For(0, output.DimZ, z =>
            {
                var index = output.DimXY * z;

                for (var i = index; i < output.DimXY + index; i++)
                {
                    output[i] = computationDistanceVolume[i] <= cutLevel ? foreground : background;
                }
            });
        }

        public static void RefineSmoothingWithBrushes(
           Volume<byte> brushes,
           Volume3D<byte> output,
           Region3D<int> region,
           byte foreground,
           byte foregroundBrush)
        {
            Parallel.For(region.MinimumZ, region.MaximumZ + 1, z =>
            {
                for (int y = 0; y < output.DimY; y++)
                {
                    for (int x = 0; x < output.DimX; x++)
                    {
                        var i = output.GetIndex(x, y, z);
                        if (region.MinimumX > x || region.MinimumY > y || region.MaximumX < x || region.MaximumY < y)
                        {
                            continue;
                        }
                        else if (brushes[i] == foregroundBrush)
                        {
                            output[i] = foreground;
                        }
                    }
                }
            });
        }

        public static void ComputeBinaryMask(this Volume3D<float> computationDistanceVolume,
            Volume3D<byte> output,
            Region3D<int> region,
            float cutLevel = 0,
            byte foreground = 1,
            byte background = 0)
        {
            Parallel.For(region.MinimumZ, region.MaximumZ + 1, z =>
            {
                for (int y = 0; y < output.DimY; y++)
                {
                    for (int x = 0; x < output.DimX; x++)
                    {
                        var i = output.GetIndex(x, y, z);
                        byte newValue;
                        if (region.MinimumX > x || region.MinimumY > y || region.MaximumX < x || region.MaximumY < y)
                        {
                            newValue = background;
                        }
                        else
                        {
                            newValue = computationDistanceVolume[i] < cutLevel ? foreground : background;
                        }

                        output[i] = newValue;
                    }
                }
            });
        }

        /// <summary>
        /// Checks if the directory structure for the given file path already exists.
        /// If not, the directory will be created.
        /// </summary>
        /// <param name="filePath"></param>
        public static void CreateFolderStructureIfNotExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Computes the integral of the present volume. The result must
        /// use a 64bit format because integrals of large volumes can easily exceed
        /// 32bit integer ranges.
        /// </summary>
        /// <param name="input">The input volume.</param>
        public static Volume3D<long> IntegralImage(this Volume3D<byte> input)
        {
            var output = input.CreateSameSize<long>();
            int dimX = output.DimX;
            int dimY = output.DimY;
            int dimZ = output.DimZ;
            int dimXy = output.DimXY;

            // Point value at origin.
            output[0] = input[0];

            // Values in lines along each axis, with the other two coordinates held at zero.
            for (int x = 1; x < dimX; x++)
            {
                output[x] = input[x] + output[x - 1];
            }

            for (int y = 1; y < dimY; y++)
            {
                output[y * dimX] = input[y * dimX] + output[(y - 1) * dimX];
            }

            for (int z = 1; z < dimZ; z++)
            {
                output[z * dimXy] = input[z * dimXy] + output[(z - 1) * dimXy];
            }

            // Values in bottom plane. The trick here can be visualized as follows:
            //
            // +-----------------------+-+
            // |           B           |D|
            // +-----------------------+-+
            // |                       | |
            // |           A           |C|
            // |                       | |
            // |                       | |
            // O-----------------------+-+
            //
            // Area A extends to (x-1,y-1) from the origin O. B is of size (x-1,1), C is of size (1,x-1),
            // and D is (1,1). U, the union of all four areas extends from the origin to (x,y). So S(U),
            // the sum of intensities in U, is S(A)+S(B)+S(C)+S(D). We have
            //    output[x-1, y-1, 0] = S(A)
            //    output[x-1, y, 0]   = S(A)+S(B)
            //    output[x, y-1, 0]   = S(A)+S(C)
            //    input[x, y, 0]      = S(D)
            // and so S(U) = S(A)+S(B)+S(C)+S(D)
            //             = S(A)+S(B) + S(A)+S(C) + S(D) - S(A)
            //             = output[x-1, y, 0] + output[x, y-1, 0] + input[x, y, 0] - output[x-1, y-1, 0]
            for (int y = 1; y < dimY; y++)
            {
                for (int x = 1; x < dimX; x++)
                {
                    output[x, y, 0] = input[x, y, 0] + output[x - 1, y, 0] + output[x, y - 1, 0] - output[x - 1, y - 1, 0];
                }
            }

            // The same trick as above, but in the plane y=0 rather than z=0.
            for (int z = 1; z < dimZ; z++)
            {
                for (int x = 1; x < dimX; x++)
                {
                    output[x, 0, z] = input[x, 0, z] + output[x - 1, 0, z] + output[x, 0, z - 1] - output[x - 1, 0, z - 1];
                }
            }

            // The same trick as above, but in the plane x=0 rather than z=0.
            for (int z = 1; z < dimZ; z++)
            {
                for (int y = 1; y < dimY; y++)
                {
                    output[0, y, z] = input[0, y, z] + output[0, y - 1, z] + output[0, y, z - 1] - output[0, y - 1, z - 1];
                }
            }

            // Essentially the same trick again, but in three dimensions.
            for (int z = 1; z < dimZ; z++)
            {
                for (int y = 1; y < dimY; y++)
                {
                    for (int x = 1; x < dimX; x++)
                    {
                        output[x, y, z] = input[x, y, z] + output[x, y, z - 1] + output[x, y - 1, z] - output[x, y - 1, z - 1] + output[x - 1, y, z] - output[x - 1, y, z - 1] - output[x - 1, y - 1, z] + output[x - 1, y - 1, z - 1];
                    }
                }
            }

            return output;

        }

        /// <summary>
        /// Creates a volume that contains the differences between a volume and its sagittal mirror volume.
        /// That is, for a voxel on the left side, compute the difference to the mirror voxel on the right side.
        /// This assumes that sagitall mirroring means mirroring across the X dimension of the volume.
        /// </summary>
        /// <param name="input">The input volume.</param>
        /// <returns></returns>
        public static Volume3D<byte> CreateSagittalSymmetricAbsoluteDifference(this Volume3D<byte> input)
        {
            var output = input.CreateSameSize<byte>(0);

            var dimX = output.DimX;
            var dimY = output.DimY;
            var dimZ = output.DimZ;
            var dimXY = output.DimXY;
            var rangeX = dimX / 2;
            var dimX_Minus_1 = dimX - 1;
            for (var z = 0; z < dimZ; ++z)
            {
                for (var y = 0; y < dimY; ++y)
                {
                    for (var x = 0; x < rangeX; ++x)
                    {
                        var index = x + y * dimX + z * dimXY;
                        var mirror = dimX_Minus_1 - x + y * dimX + z * dimXY;
                        var value = (byte)Math.Abs(input[index] - input[mirror]);
                        output[index] = value;
                        output[mirror] = value;
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Creates an output volume that contains the given region of the input volume.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <param name="region">The region of the input image that should be in the returned image.</param>
        /// <returns></returns>
        public static Volume3D<T> Crop<T>(this Volume3D<T> image, Region3D<int> region)
        {
            if (region.IsEmpty())
            {
                throw new ArgumentException("The cropping region must be non-empty.", nameof(region));
            }

            if (!region.InsideOf(image.GetFullRegion()))
            {
                throw new ArgumentException("The cropping region must be fully inside the image.", nameof(region));
            }

            var startX = region.MinimumX;
            var startY = region.MinimumY;
            var startZ = region.MinimumZ;
            var dimX = region.LengthX();
            var dimY = region.LengthY();
            var dimZ = region.LengthZ();
            var origin = image.Transform.PixelToPhysical(new Point3D(startX, startY, startZ));
            var output = new Volume3D<T>(dimX, dimY, dimZ,
                image.SpacingX, image.SpacingY, image.SpacingZ,
                origin, image.Direction);
            var inputBufferArray = image.Array;
            var outputBufferArray = output.Array;

            // Copy the data, line by line
            for (var z = 0; z < dimZ; ++z)
            {
                var inputPage = (z + startZ) * image.DimXY;
                var outputPage = z * output.DimXY;
                for (var y = 0; y < dimY; ++y)
                {
                    var inputLine = inputPage + (y + startY) * image.DimX + startX;
                    var outputLine = outputPage + y * output.DimX;
                    Array.Copy(inputBufferArray, inputLine, outputBufferArray, outputLine, dimX);
                }
            }
            return output;

        }

        /// <summary>
        /// Computes the boundary of the structure.
        /// </summary>
        /// <param name="structure">A structure: values are positive for voxels in the structure, otherwise zero</param>
        /// <param name="withEdges">Whether voxels on the edges and sides of the region are to be set, i.e.
        /// whether we assume the structure does not go beyond the boundaries of the space.</param>
        /// <returns>A volume of the same size as the input, with values set to 1 for voxels on the boundary of the inputImage, 0 otherwise.</returns>
        public static Volume3D<byte> MaskBoundaries(this Volume3D<byte> structure, bool withEdges)
        {
            int dimX = structure.DimX;
            int dimY = structure.DimY;
            int dimZ = structure.DimZ;
            var output = structure.CreateSameSize<byte>();
            Parallel.For(0, dimZ, (int z) =>
            {
                for (int y = 0; y < dimY; ++y)
                {
                    for (int x = 0; x < dimX; ++x)
                    {
                        // Voxels on the edge are not checked as they are considered as boundary by default when edges are being considered
                        if (structure.IsEdgeVoxel(x, y, z))
                        {
                            if (withEdges)
                            {
                                output[x, y, z] = (byte)(structure[x, y, z] > 0 ? 1 : 0);
                            }
                        }
                        else
                        {
                            output[x, y, z] = (byte)(IsBoundaryVoxel(structure, x, y, z) ? 1 : 0);
                        }
                    }
                }
            });
            return output;
        }

        /// <summary>
        /// Returns whether (x,y,z) (which must not be on the edge of the space itself) is on the boundary of
        /// the structure. This is the case when intensity is positive at (x,y,z) and intensity is non-positive at any
        /// of the immediate 26 neighbours of (x,y,z). x, y and z must all be in the interior of the region.
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="x">between 1 and structure.DimX - 1</param>
        /// <param name="y">between 1 and structure.DimY - 1</param>
        /// <param name="z">between 1 and structure.DimZ - 1</param>
        /// <returns>Whether the voxel at (x,y,z) is on the boundary of the structure</returns>
        private static bool IsBoundaryVoxel(Volume3D<byte> structure, int x, int y, int z)
        {
            if (structure[x, y, z] <= 0)
            {
                return false;
            }
            for (int zz = z - 1; zz <= z + 1; zz++)
            {
                for (int yy = y - 1; yy <= y + 1; yy++)
                {
                    for (int xx = x - 1; xx <= x + 1; xx++)
                    {
                        if (structure[xx, yy, zz] <= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Performs a convolution on the volume using a median filter. A Median filter inspects a neighborhood of given size,
        /// computes the median value of those, and replaces the voxel with the median. The neighborhood
        /// is [x-radius, x+radius], same across y and z dimension. A radius of 1 will create a neighborhood
        /// of size 27 voxels.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input">The volume to smooth.</param>
        /// <param name="radius">The size of the neighborhood, radius >= 0</param>
        /// <param name="merge2Values">A function that merges 2 instances of the volume's datatype,
        /// when computing the median of an even number of voxels.</param>
        /// <returns></returns>
        public static Volume3D<T> MedianSmooth<T>(this Volume3D<T> input, int radius, Func<T, T, T> merge2Values)
        {
            var output = input.CreateSameSize<T>();

            int nbhoodLen = 2 * radius + 1;
            int nbhoodSize = nbhoodLen * nbhoodLen * nbhoodLen;

            int dimX = input.DimX;
            int dimY = input.DimY;
            int dimZ = input.DimZ;
            int dimXY = input.DimXY;
            int xlim = dimX - radius;
            int ylim = dimY - radius;
            int zlim = dimZ - radius;

            // Loop through all voxels in image and find median of neighboorhood for each.
            Parallel.For(0, dimX, x =>
            {
                // x-limits of neighborhood to lie inside image.
                int xxmin = x >= radius ? x - radius : 0;
                int xxmax = x < xlim ? x + radius : dimX - 1;

                // The voxel values in the neighborhood. The maximum number of voxels is nbhoodSize,
                // but can be less when working near volume boundaries.
                var nbhoodVals = new T[nbhoodSize];

                for (int y = 0; y < dimY; y++)
                {
                    // y-limits of neighborhood to lie inside image.
                    int yymin = y >= radius ? y - radius : 0;
                    int yymax = y < ylim ? y + radius : dimY - 1;

                    for (int z = 0; z < dimZ; z++)
                    {
                        // z-limits of neighborhood to lie inside image.
                        int zzmin = z >= radius ? z - radius : 0;
                        int zzmax = z < zlim ? z + radius : dimZ - 1;

                        // Index of current voxel.
                        var index = x + y * dimX + z * dimXY;

                        // Loop through neighborhood voxels and store intensitites in nbhoodVals.
                        // When loop exits, nbhoodIndex equals to the effective nbhoodVals size.
                        int nbhoodIndex = 0;
                        for (int xx = xxmin; xx <= xxmax; xx++)
                            for (int yy = yymin; yy <= yymax; yy++)
                                for (int zz = zzmin; zz <= zzmax; zz++)
                                    nbhoodVals[nbhoodIndex++] = input[xx + yy * dimX + zz * dimXY];

                        // Sort neighborhood intensities and assign median to current voxel.
                        Array.Sort(nbhoodVals, 0, nbhoodIndex);
                        int mid = nbhoodIndex / 2;
                        T median;
                        if (nbhoodIndex % 2 == 1)
                        {
                            // size of neighborhood is odd: Take middle value
                            median = nbhoodVals[mid];
                        }
                        else
                        {
                            median = merge2Values(nbhoodVals[mid - 1], nbhoodVals[mid]);
                        }
                        output[index] = median;
                    }
                }
            });
            return output;
        }

        /// <summary>
        /// Performs a convolution on the volume using a median filter. Median filter inspects a neighborhood of given size,
        /// computes the median value of those, and replaces the voxel with the median. The neighborhood
        /// is [x-radius, x+radius], same across y and z dimension. A radius of 1 will create a neighborhood
        /// of size 27 voxels.
        /// </summary>
        public static Volume3D<byte> MedianSmooth(this Volume3D<byte> input, int radius)
        {
            // Average 2 values via direct cast here. The two values to average will always be integral,
            // the average of the two will be either already integral (average of 2 and 4 is 3)
            // or the average is exactly between two integers. Hence, rounding up or down does not make a difference.
            return input.MedianSmooth(radius, (b1, b2) => (byte)(((float)b1 + b2) / 2));
        }

        /// <summary>
        /// Performs a convolution on the volume using a median filter. A Median filter inspects a neighborhood of given size,
        /// computes the median value of those, and replaces the voxel with the median. The neighborhood
        /// is [x-radius, x+radius], same across y and z dimension. A radius of 1 will create a neighborhood
        /// of size 27 voxels.
        /// </summary>
        public static Volume3D<short> MedianSmooth(this Volume3D<short> input, int radius)
        {
            return input.MedianSmooth(radius, (b1, b2) => (short)(((float)b1 + b2) / 2));
        }

        /// <summary>
        /// Performs a convolution on the volume using a median filter. A Median filter inspects a neighborhood of given size,
        /// computes the median value of those, and replaces the voxel with the median. The neighborhood
        /// is [x-radius, x+radius], same across y and z dimension. A radius of 1 will create a neighborhood
        /// of size 27 voxels.
        /// </summary>
        public static Volume3D<float> MedianSmooth(this Volume3D<float> input, int radius)
        {
            return input.MedianSmooth(radius, (b1, b2) => (b1 + b2) / 2);
        }

        /// <summary>
        /// Creates a new volume of the same size as the present volume, by mapping each voxel through the
        /// given function. Processing can be done on multiple CPU threads.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="volume"></param>
        /// <param name="func"></param>
        /// <param name="maxThreads">The maximum number of CPU threads that will be used for processing. If null,
        /// do not use parallel processing.</param>
        /// <returns></returns>
        public static Volume3D<TOut> Map<TIn, TOut>(this Volume3D<TIn> volume, int? maxThreads, Func<TIn, TOut> func)
        {
            var output = volume.CreateSameSize<TOut>();
            volume.Array.MapToArray(output.Array, maxThreads, func);
            return output;
        }

        /// <summary>
        /// Creates a new volume of the same size as the present volume, by mapping each voxel through the
        /// given function.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="volume"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Volume2D<TOut> Map<TIn, TOut>(this Volume2D<TIn> volume, Func<TIn, TOut> func)
        {
            var output = volume.CreateSameSize<TOut>();
            volume.Array.MapToArray(output.Array, null, func);
            return output;
        }

        /// <summary>
        /// Creates a new volume of the same size as the present volume, by mapping each voxel through the
        /// given function. Processing is done on as many CPU threads as given by <see cref="Environment.ProcessorCount"/>.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="volume"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static Volume3D<TOut> Map<TIn, TOut>(this Volume3D<TIn> volume, Func<TIn, TOut> func)
            => volume.Map(Environment.ProcessorCount, func);

        /// <summary>
        /// Creates a new volume of the same size as the present volume, by mapping each voxel through the
        /// given function. The function arguments are the voxel value and its index in the input and output volume.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="volume"></param>
        /// <param name="func"></param>
        /// <param name="maxThreads">The maximum number of CPU threads that will be used for processing. If null,
        /// do not use parallel processing.</param>
        /// <returns></returns>
        public static Volume3D<TOut> MapIndexed<TIn, TOut>(this Volume3D<TIn> volume, int? maxThreads, Func<TIn, int, TOut> func)
        {
            var output = volume.CreateSameSize<TOut>();
            volume.Array.MapToArrayIndexed(output.Array, maxThreads, func);
            return output;
        }

        /// <summary>
        /// Creates a new volume of the same size as the present volume, by mapping each voxel through the
        /// given function. The function arguments are the voxel value and its index in the input and output volume.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="volume"></param>
        /// <param name="func"></param>
        /// <param name="maxThreads">The maximum number of CPU threads that will be used for processing. If null,
        /// do not use parallel processing.</param>
        /// <returns></returns>
        public static Volume2D<TOut> MapIndexed<TIn, TOut>(this Volume2D<TIn> volume, Func<TIn, int, TOut> func)
        {
            var output = volume.CreateSameSize<TOut>();
            volume.Array.MapToArrayIndexed(output.Array, null, func);
            return output;
        }

        /// <summary>
        /// Converts a floating point volume that represents a class posterior, with
        /// voxel values between 0.0 and 1.0, to a byte volume with voxel values between 0 and 255.
        /// An input voxel value of 1.0 would be mapped to 255 in the output.
        /// Any values at or below 0.0 would become 0, anything exceeding 1.0 in the input will become 255.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="maxThreads">The maximum number of CPU threads that will be used for processing. If null,
        /// do not use parallel processing.</param>
        /// <returns></returns>
        public static Volume3D<byte> PosteriorToByte(this Volume3D<float> volume, int? maxThreads)
        {
            return volume.Map(maxThreads, Converters.PosteriorToByte);
        }

        /// <summary>
        /// Creates a region that covers the full area of the input image.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Region3D<int> GetFullRegion<T>(this Volume3D<T> image)
        {
            return new Region3D<int>(0, 0, 0, image.DimX - 1, image.DimY - 1, image.DimZ - 1);
        }

        /// <summary>
        /// Returns a Region3D object encompassing the foreground voxels in the union of
        /// masks volumeA and volumeB
        /// </summary>
        /// <param name="wanted1">Mask to be encompassed by bounds</param>
        /// <param name="wanted2">Mask to be encompassed by bounds</param>
        public static Region3D<int> GetCombinedForegroundRegion(this Volume3D<byte> volumeA, Volume3D<byte> volumeB)
        {
            if (volumeA == null)
            {
                throw new ArgumentNullException(nameof(volumeA));
            }
            else if (volumeB == null)
            {
                throw new ArgumentNullException(nameof(volumeB));
            }
            else
            {
                // get the foreground regions for each of the volumes
                var interestedRegionVolumeA = volumeA.GetInterestRegion();
                var interestedRegionVolumeB = volumeB.GetInterestRegion();

                // return the region that covers both of the ROI bounds
                return new Region3D<int>(
                    minimumX: Math.Min(interestedRegionVolumeA.MinimumX, interestedRegionVolumeB.MinimumX),
                    minimumY: Math.Min(interestedRegionVolumeA.MinimumY, interestedRegionVolumeB.MinimumY),
                    minimumZ: Math.Min(interestedRegionVolumeA.MinimumZ, interestedRegionVolumeB.MinimumZ),
                    maximumX: Math.Max(interestedRegionVolumeA.MaximumX, interestedRegionVolumeB.MaximumX),
                    maximumY: Math.Max(interestedRegionVolumeA.MaximumY, interestedRegionVolumeB.MaximumY),
                    maximumZ: Math.Max(interestedRegionVolumeA.MaximumZ, interestedRegionVolumeB.MaximumZ));
            }
        }

        /// <summary>
        /// Computes the start index and number of pixels to copy when copying from a source image
        /// onto a destination image, when the source image should be place at position "start" in the
        /// destination image.
        /// </summary>
        /// <param name="sourceDim">The width of the source image</param>
        /// <param name="destDim">The width of the destination image</param>
        /// <param name="start">The start position, as a position in the destination image.</param>
        /// <returns>The start position in the source image, and the number of pixels/voxels to copy.</returns>
        private static Tuple<int, int> GetAdjustedPositions(int start, int sourceDim, int destDim)
        {
            if (start > 0)
            {
                // DDDDDDDDD     // Destination image to copy into
                //      SSSSSSS  // Source image to copy from
                //      X        // Start position, relative to destination image
                // |       |     // Boundaries of the destination image
                // DDDDDSSSS     // Paste result
                return Tuple.Create(0, Math.Min(sourceDim, destDim - start));
            }
            //      DDDDDDDDD     // Destination image to copy into
            //  SSSSSSS           // Source image to copy from
            //  X                 // Start position, relative to destination image
            //      |       |     // Boundaries of the destination image
            //      SSSDDDDDD     // Paste result
            return Tuple.Create(-start, Math.Min(sourceDim, destDim + start));
        }

        /// <summary>
        /// Copies the contents of the present (source) image into the destination image at the given position.
        /// The target position is specified as coordinates inside the destination image: The point (0, 0, 0)
        /// of the source image will be placed at (startX, startY, startZ) in the destination image.
        /// The target position can be outside of the destination image.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="startZ"></param>
        public static void PasteOnto(this Volume3D<byte> source, Volume3D<byte> destination, int startX, int startY, int startZ)
        {
            // Start position in the source image, and length of region to copy out.
            var posX = GetAdjustedPositions(startX, source.DimX, destination.DimX);
            var posY = GetAdjustedPositions(startY, source.DimY, destination.DimY);
            var posZ = GetAdjustedPositions(startZ, source.DimZ, destination.DimZ);

            var srcBufferArray = source.Array;
            var destBufferArray = destination.Array;

            foreach (var z in Enumerable.Range(posZ.Item1, posZ.Item2))
            {
                var srcPage = z * source.DimXY;
                var destPage = (z + startZ) * destination.DimXY;
                foreach (var y in Enumerable.Range(posY.Item1, posY.Item2))
                {
                    var srcLine = srcPage + y * source.DimX + posX.Item1;
                    var destLine = destPage + (y + startY) * destination.DimX + posX.Item1 + startX;
                    Array.Copy(srcBufferArray, srcLine, destBufferArray, destLine, posX.Item2);
                }
            }
        }

        /// <summary>
        /// Converts a Float32 volume to an Int16 volume, if all voxel values are in the correct range for Int16, and if
        /// they appear to contain integer values after rounding to 5 digits. An input value of 1.0000001 would be considered
        /// OK, and converted to (short)1. If any of the input voxels is outside the Int16 range, or appears to be a fractional
        /// value, throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="maxThreads">The maximum number of CPU threads that will be used for processing. If null,
        /// do not use parallel processing.</param>
        /// <returns></returns>
        public static Volume3D<short> TryConvertToInt16(this Volume3D<float> source, int? maxThreads)
        {
            return source.MapIndexed(maxThreads, Converters.TryConvertToInt16);
        }

        /// <summary>
        /// Creates a volume that contains the sagittal mirror image of the present volume
        /// (i.e., swapping left and right).
        /// This assumes that sagitall mirroring means mirroring across the X dimension of the volume.
        /// </summary>
        /// <param name="input">The volume to mirror.</param>
        /// <returns></returns>
        public static void SagittalMirroringInPlace<T>(this Volume3D<T> image)
        {
            if (image == null)
            {
                return;
            }

            var dimX = image.DimX;
            var dimY = image.DimY;
            var dimZ = image.DimZ;
            var dimXy = image.DimXY;
            var rangeX = dimX / 2;
            var dimXMinus1 = dimX - 1;
            Parallel.For(0, dimZ,
                z =>
                {
                    var page = z * dimXy;
                    for (var y = 0; y < dimY; ++y)
                    {
                        var line = page + y * dimX;
                        for (var x = 0; x < rangeX; ++x)
                        {
                            var index = x + line;
                            var mirrorIndex = dimXMinus1 - x + line;
                            var temp = image[mirrorIndex];
                            image[mirrorIndex] = image[index];
                            image[index] = temp;
                        }
                    }
                });
        }

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
        public static Volume2D<TK> AllocateSlice<T, TK>(this Volume3D<T> volume, SliceType sliceType)
            => ExtractSlice.AllocateSlice<T,TK>(volume, sliceType);

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
        public static Volume2D<T> Slice<T>(this Volume3D<T> volume, SliceType sliceType, int index)
            => ExtractSlice.Slice(volume, sliceType, index);

        /// <summary>
        /// Multiplies the present volume by a constant, such that if the input image 
        /// has range [0, 255], the result image will have voxel values in the range [0, <paramref name="maxOutputRange"/>].
        /// </summary>
        /// <param name="image"></param>
        /// <param name="geodesicGamma">The maximum value that voxels can have in the result image.</param>
        /// <returns></returns>
        public static Volume3D<float> ScaleToFloatRange(this Volume3D<byte> image, float maxOutputRange)
        {
            if (maxOutputRange <= 0)
            {
                throw new ArgumentOutOfRangeException("The parameter must be larger than zero.", nameof(maxOutputRange));
            }

            var scale = maxOutputRange / byte.MaxValue;
            return image.Map(value => scale * value);
        }

        /// <summary>
        /// Returns a volume that is the voxel-wise subtraction of the arguments.
        /// result[i] == vol1[i] - vol2[i]
        /// </summary>
        /// <param name="vol1"></param>
        /// <param name="vol2"></param>
        /// <returns></returns>
        public static Volume3D<float> Subtract(this Volume3D<float> vol1, Volume3D<float> vol2)
        {
            return vol1.MapIndexed(null, (value, index) => value - vol2[index]);
        }

        /// <summary>
        /// Returns the index in the given sequence at which the maximum value
        /// is attained. Indexing starts at 0 for the first element of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static (int Index, T Maximum) ArgMax<T>(this IEnumerable<T> values)
            where T: IComparable
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var argMax = 0;
            T maxValue = default;
            bool any = false;
            int index = 0;
            foreach (var value in values)
            {
                if (any)
                {
                    if (value.CompareTo(maxValue) > 0)
                    {
                        maxValue = value;
                        argMax = index;
                    }
                }
                else
                {
                    any = true;
                    maxValue = value;
                    argMax = 0;
                }
                index++;
            }

            return any ? (argMax, maxValue) : throw new ArgumentException("The input sequence was empty.");
        }

        /// <summary>
        /// Returns the Z-slice that contains the highest number of pixels that have the 
        /// given foreground value.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="foregroundValue"></param>
        /// <returns></returns>
        public static (int Z, int ForegroundPixels) SliceWithMostForeground(
            this Volume3D<byte> volume, 
            byte foregroundValue = ModelConstants.MaskForegroundIntensity)
        {
            if (volume == null)
            {
                throw new ArgumentNullException(nameof(volume));
            }

            var foregroundPerZ = new int[volume.DimZ];
            volume.ParallelIterateSlices(position =>
            {
                var (x, y, z) = position;
                if (volume[x, y, z] == foregroundValue)
                {
                    foregroundPerZ[z] += 1;
                }
            });

            return foregroundPerZ.ArgMax();
        }

        /// <summary>
        /// Gets the sequence of voxel with their values in an image, that fall inside the mask (that is, the 
        /// mask value that corresponds to the voxels is not equal to zero).
        /// This is the implementation called when both image and map are not null.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <param name="mask">The binary mask that specifies which voxels of the image should be returned.
        /// Cannot be null.</param>
        /// <returns></returns>
        private static IEnumerable<T> VoxelsInsideMaskWhenImageAndMaskAreNotNull<T>(T[] imageArray, byte[] maskArray)
        {
            if (maskArray.Length != imageArray.Length)
            {
                throw new ArgumentException("The image and the mask must have the same number of voxels.", nameof(maskArray));
            }

            // Plain vanilla loop is the fastest way of iterating, 4x faster than Array.Indices()
            for (var index = 0; index < imageArray.Length; index++)
            {
                if (maskArray[index] > 0)
                {
                    yield return imageArray[index];
                }
            }
        }

    }
}