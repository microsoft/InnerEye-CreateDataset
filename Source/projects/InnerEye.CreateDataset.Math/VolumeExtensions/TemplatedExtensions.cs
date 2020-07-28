///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿/*
	==> AUTO GENERATED FILE, edit CommonExtensions.tt instead <==
*/
namespace InnerEye.CreateDataset.Math
{
	using System;
    using System.Linq;
    using System.Threading.Tasks;
	using System.Collections.Generic;
	using InnerEye.CreateDataset.Volumes;

	public static partial class Converters
	{
	    /// <summary>
        /// Converts a floating point value to a byte value, using rounding. If the value is outside of the 
        /// valid range for byte, the returned value attains the minimum/maximum value for byte.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns></returns>
        public static byte ClampToByte(double value)
        {
            if (value < byte.MinValue)
            {
                return byte.MinValue;
            }

            if (value > byte.MaxValue)
            {
                return byte.MaxValue;
            }

            return (byte)Math.Round(value, MidpointRounding.AwayFromZero);
        }

	    /// <summary>
        /// Converts a floating point value to a byte value, using rounding. If the value is outside of the 
        /// valid range for byte, the returned value attains the minimum/maximum value for byte.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns></returns>
        public static byte ClampToByte(float value)
        {
            if (value < byte.MinValue)
            {
                return byte.MinValue;
            }

            if (value > byte.MaxValue)
            {
                return byte.MaxValue;
            }

            return (byte)Math.Round(value, MidpointRounding.AwayFromZero);
        }

	    /// <summary>
        /// Converts a floating point value to a short value, using rounding. If the value is outside of the 
        /// valid range for short, the returned value attains the minimum/maximum value for short.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns></returns>
        public static short ClampToInt16(double value)
        {
            if (value < short.MinValue)
            {
                return short.MinValue;
            }

            if (value > short.MaxValue)
            {
                return short.MaxValue;
            }

            return (short)Math.Round(value, MidpointRounding.AwayFromZero);
        }

	    /// <summary>
        /// Converts a floating point value to a short value, using rounding. If the value is outside of the 
        /// valid range for short, the returned value attains the minimum/maximum value for short.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns></returns>
        public static short ClampToInt16(float value)
        {
            if (value < short.MinValue)
            {
                return short.MinValue;
            }

            if (value > short.MaxValue)
            {
                return short.MaxValue;
            }

            return (short)Math.Round(value, MidpointRounding.AwayFromZero);
        }

	}

	public static class CommonExtensions
	{        /// <summary>
        /// Gets the region of the volume that contains all voxel values that are
		/// larger or equal than the interestId.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="interestId">The voxel values to search for. Foreground is
		/// considered to be all voxels with a value larger or equal to the interestId.</param>
        /// <returns></returns>
		public static Region3D<int> GetInterestRegion(this Volume3D<int> volume, int interestId)
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
                maximumX[z] = int.MinValue;
                maximumY[z] = int.MinValue;
                maximumZ[z] = int.MinValue;

                for (int y = 0; y < volume.DimY; y++)
                {
                    var delta = y * volume.DimX + z * volume.DimXY;

                    for (int x = 0; x < volume.DimX; x++)
                    {
                        if (volume[x + delta] >= interestId)
                        {
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
                }
            });

            var region = new Region3D<int>(minimumX.Min(), minimumY.Min(), minimumZ.Min(), maximumX.Max(), maximumY.Max(), maximumZ.Max());
            
			// If no foreground values are found, the region minimum will be Int.MaxValue, maximum will be Int.MinValue.
			// When accidentally doing operations on that region, it will most likely lead to numerical
			// overflow or underflow. Instead, return an empty region that has less troublesome boundary values.
			return region.IsEmpty() ? RegionExtensions.EmptyIntRegion() : region;
        }

		/// <summary>
		/// Returns the maximum and minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static MinMax<int> GetMinMax(this int[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = int.MaxValue;			
			var maxValue = int.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}

				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return new MinMax<int>() { Maximum = maxValue, Minimum = minValue };
		}

		/// <summary>
		/// Returns the difference between maximum and minimum in the given range.
		/// </summary>
		public static int Range(this MinMax<int> minMax)
		{
			if (minMax.Minimum > minMax.Maximum)
			{
				throw new ArgumentException("The minimum must not be larger than the maximum", nameof(minMax));
			}

			return (int)(minMax.Maximum - minMax.Minimum);
		}

		/// <summary>
		/// If the given value falls inside the range stored in <paramref name="minMax"/>, return the
		/// value unchanged. If the value is smaller than the range minimum, return the minimum.
		/// If the value is larger than the range maximum, return the maximum.
		/// </summary>
		public static int Clamp(this MinMax<int> minMax, int value)
		{
			if (value < minMax.Minimum)
			{
				return minMax.Minimum;
			}

			if (value > minMax.Maximum)
			{
				return minMax.Maximum;
			}

			return value;
		}

		/// <summary>
		/// Returns the minimum and maximum voxel value in the volume.
		/// Throws an ArgumentNullException if the volume is null.
		/// </summary>
		public static MinMax<int> GetMinMax(this Volume<int> volume)
		{
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}

			return volume.Array.GetMinMax();
		}

		/// <summary>
		/// Returns the minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static int Minimum(this int[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = int.MaxValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}
			}

			return minValue;
		}

		/// <summary>
		/// Returns the maximum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static int Maximum(this int[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var maxValue = int.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return maxValue;
		}

        /// <summary>
        /// Computes the minimum and the maximum of a sequence of values, in a single pass through the data.
        /// Returns null if the input sequence is empty.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MinMax<int>? GetRange(this IEnumerable<int> stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var max = int.MinValue;
            var min = int.MaxValue;
            var any = false;
            foreach (var d in stream)
            {
                any = true;
                if (d > max)
                {
                    max = d;
                }

                if (d < min)
                {
                    min = d;
                }
            }

            if (!any)
            {
                return null;
            }

            return MinMax.Create(min, max);
        }

        /// <summary>
        /// Checks whether all of the voxel values in the input volume are inside of the given <see cref="range"/>.
        /// If a voxel value is below the given minimum, it is changed to be exactly at the minimum.
        /// If a voxel value is above the given maximum, it is changed to be exactly at the maximum.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="range"></param>
        public static void ClipToRangeInPlace(this Volume3D<int> image, MinMax<int> range)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

			if (range.Minimum > range.Maximum)
			{
				throw new ArgumentException($"Invalid range provided: Minimum = {range.Minimum}, Maximum = {range.Maximum}", nameof(range));
			}

            var array = image.Array;
            for (var index = 0; index < array.Length; index++)
            {
                var value = array[index];
                if (value < range.Minimum)
                {
                    array[index] = range.Minimum;
                }
                else if (value > range.Maximum)
                {
                    array[index] = range.Maximum;
                }
            }
        }

        /// <summary>
        /// Creates a binary mask by thresholding the present volume. All voxels that have
        /// a value larger or equal than the threshold will be set to 1
		/// (<see cref="ModelConstants.MaskForegroundIntensity">) in the result,
		/// all other voxels will be set to 0 (<see cref="ModelConstants.MaskBackgroundIntensity">)
		/// </summary>
        /// <param name="geoDistanceByte">A volume that stores geodesic distances.</param>
        /// <param name="geoThreshold">The minimum distance that is required for a voxel to be considered foreground.</param>
        /// <returns></returns>
        public static Volume3D<byte> Threshold(
            this Volume3D<int> volume, 
            int threshold)
        {
            return volume.Map(value =>
                value >= threshold
                ? ModelConstants.MaskForegroundIntensity
                : ModelConstants.MaskBackgroundIntensity);
        }

	        /// <summary>
        /// Gets the region of the volume that contains all voxel values that are
		/// larger or equal than the interestId.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="interestId">The voxel values to search for. Foreground is
		/// considered to be all voxels with a value larger or equal to the interestId.</param>
        /// <returns></returns>
		public static Region3D<int> GetInterestRegion(this Volume3D<double> volume, double interestId)
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
                maximumX[z] = int.MinValue;
                maximumY[z] = int.MinValue;
                maximumZ[z] = int.MinValue;

                for (int y = 0; y < volume.DimY; y++)
                {
                    var delta = y * volume.DimX + z * volume.DimXY;

                    for (int x = 0; x < volume.DimX; x++)
                    {
                        if (volume[x + delta] >= interestId)
                        {
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
                }
            });

            var region = new Region3D<int>(minimumX.Min(), minimumY.Min(), minimumZ.Min(), maximumX.Max(), maximumY.Max(), maximumZ.Max());
            
			// If no foreground values are found, the region minimum will be Int.MaxValue, maximum will be Int.MinValue.
			// When accidentally doing operations on that region, it will most likely lead to numerical
			// overflow or underflow. Instead, return an empty region that has less troublesome boundary values.
			return region.IsEmpty() ? RegionExtensions.EmptyIntRegion() : region;
        }

		/// <summary>
		/// Returns the maximum and minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static MinMax<double> GetMinMax(this double[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = double.MaxValue;			
			var maxValue = double.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}

				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return new MinMax<double>() { Maximum = maxValue, Minimum = minValue };
		}

		/// <summary>
		/// Returns the difference between maximum and minimum in the given range.
		/// </summary>
		public static double Range(this MinMax<double> minMax)
		{
			if (minMax.Minimum > minMax.Maximum)
			{
				throw new ArgumentException("The minimum must not be larger than the maximum", nameof(minMax));
			}

			return (double)(minMax.Maximum - minMax.Minimum);
		}

		/// <summary>
		/// If the given value falls inside the range stored in <paramref name="minMax"/>, return the
		/// value unchanged. If the value is smaller than the range minimum, return the minimum.
		/// If the value is larger than the range maximum, return the maximum.
		/// </summary>
		public static double Clamp(this MinMax<double> minMax, double value)
		{
			if (value < minMax.Minimum)
			{
				return minMax.Minimum;
			}

			if (value > minMax.Maximum)
			{
				return minMax.Maximum;
			}

			return value;
		}

		/// <summary>
		/// Returns the minimum and maximum voxel value in the volume.
		/// Throws an ArgumentNullException if the volume is null.
		/// </summary>
		public static MinMax<double> GetMinMax(this Volume<double> volume)
		{
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}

			return volume.Array.GetMinMax();
		}

		/// <summary>
		/// Returns the minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static double Minimum(this double[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = double.MaxValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}
			}

			return minValue;
		}

		/// <summary>
		/// Returns the maximum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static double Maximum(this double[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var maxValue = double.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return maxValue;
		}

        /// <summary>
        /// Computes the minimum and the maximum of a sequence of values, in a single pass through the data.
        /// Returns null if the input sequence is empty.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MinMax<double>? GetRange(this IEnumerable<double> stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var max = double.MinValue;
            var min = double.MaxValue;
            var any = false;
            foreach (var d in stream)
            {
                any = true;
                if (d > max)
                {
                    max = d;
                }

                if (d < min)
                {
                    min = d;
                }
            }

            if (!any)
            {
                return null;
            }

            return MinMax.Create(min, max);
        }

        /// <summary>
        /// Checks whether all of the voxel values in the input volume are inside of the given <see cref="range"/>.
        /// If a voxel value is below the given minimum, it is changed to be exactly at the minimum.
        /// If a voxel value is above the given maximum, it is changed to be exactly at the maximum.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="range"></param>
        public static void ClipToRangeInPlace(this Volume3D<double> image, MinMax<double> range)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

			if (range.Minimum > range.Maximum)
			{
				throw new ArgumentException($"Invalid range provided: Minimum = {range.Minimum}, Maximum = {range.Maximum}", nameof(range));
			}

            var array = image.Array;
            for (var index = 0; index < array.Length; index++)
            {
                var value = array[index];
                if (value < range.Minimum)
                {
                    array[index] = range.Minimum;
                }
                else if (value > range.Maximum)
                {
                    array[index] = range.Maximum;
                }
            }
        }

        /// <summary>
        /// Creates a binary mask by thresholding the present volume. All voxels that have
        /// a value larger or equal than the threshold will be set to 1
		/// (<see cref="ModelConstants.MaskForegroundIntensity">) in the result,
		/// all other voxels will be set to 0 (<see cref="ModelConstants.MaskBackgroundIntensity">)
		/// </summary>
        /// <param name="geoDistanceByte">A volume that stores geodesic distances.</param>
        /// <param name="geoThreshold">The minimum distance that is required for a voxel to be considered foreground.</param>
        /// <returns></returns>
        public static Volume3D<byte> Threshold(
            this Volume3D<double> volume, 
            double threshold)
        {
            return volume.Map(value =>
                value >= threshold
                ? ModelConstants.MaskForegroundIntensity
                : ModelConstants.MaskBackgroundIntensity);
        }

	        /// <summary>
        /// Gets the region of the volume that contains all voxel values that are
		/// larger or equal than the interestId.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="interestId">The voxel values to search for. Foreground is
		/// considered to be all voxels with a value larger or equal to the interestId.</param>
        /// <returns></returns>
		public static Region3D<int> GetInterestRegion(this Volume3D<short> volume, short interestId)
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
                maximumX[z] = int.MinValue;
                maximumY[z] = int.MinValue;
                maximumZ[z] = int.MinValue;

                for (int y = 0; y < volume.DimY; y++)
                {
                    var delta = y * volume.DimX + z * volume.DimXY;

                    for (int x = 0; x < volume.DimX; x++)
                    {
                        if (volume[x + delta] >= interestId)
                        {
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
                }
            });

            var region = new Region3D<int>(minimumX.Min(), minimumY.Min(), minimumZ.Min(), maximumX.Max(), maximumY.Max(), maximumZ.Max());
            
			// If no foreground values are found, the region minimum will be Int.MaxValue, maximum will be Int.MinValue.
			// When accidentally doing operations on that region, it will most likely lead to numerical
			// overflow or underflow. Instead, return an empty region that has less troublesome boundary values.
			return region.IsEmpty() ? RegionExtensions.EmptyIntRegion() : region;
        }

		/// <summary>
		/// Returns the maximum and minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static MinMax<short> GetMinMax(this short[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = short.MaxValue;			
			var maxValue = short.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}

				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return new MinMax<short>() { Maximum = maxValue, Minimum = minValue };
		}

		/// <summary>
		/// Returns the difference between maximum and minimum in the given range.
		/// </summary>
		public static short Range(this MinMax<short> minMax)
		{
			if (minMax.Minimum > minMax.Maximum)
			{
				throw new ArgumentException("The minimum must not be larger than the maximum", nameof(minMax));
			}

			return (short)(minMax.Maximum - minMax.Minimum);
		}

		/// <summary>
		/// If the given value falls inside the range stored in <paramref name="minMax"/>, return the
		/// value unchanged. If the value is smaller than the range minimum, return the minimum.
		/// If the value is larger than the range maximum, return the maximum.
		/// </summary>
		public static short Clamp(this MinMax<short> minMax, short value)
		{
			if (value < minMax.Minimum)
			{
				return minMax.Minimum;
			}

			if (value > minMax.Maximum)
			{
				return minMax.Maximum;
			}

			return value;
		}

		/// <summary>
		/// Returns the minimum and maximum voxel value in the volume.
		/// Throws an ArgumentNullException if the volume is null.
		/// </summary>
		public static MinMax<short> GetMinMax(this Volume<short> volume)
		{
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}

			return volume.Array.GetMinMax();
		}

		/// <summary>
		/// Returns the minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static short Minimum(this short[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = short.MaxValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}
			}

			return minValue;
		}

		/// <summary>
		/// Returns the maximum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static short Maximum(this short[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var maxValue = short.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return maxValue;
		}

        /// <summary>
        /// Computes the minimum and the maximum of a sequence of values, in a single pass through the data.
        /// Returns null if the input sequence is empty.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MinMax<short>? GetRange(this IEnumerable<short> stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var max = short.MinValue;
            var min = short.MaxValue;
            var any = false;
            foreach (var d in stream)
            {
                any = true;
                if (d > max)
                {
                    max = d;
                }

                if (d < min)
                {
                    min = d;
                }
            }

            if (!any)
            {
                return null;
            }

            return MinMax.Create(min, max);
        }

        /// <summary>
        /// Checks whether all of the voxel values in the input volume are inside of the given <see cref="range"/>.
        /// If a voxel value is below the given minimum, it is changed to be exactly at the minimum.
        /// If a voxel value is above the given maximum, it is changed to be exactly at the maximum.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="range"></param>
        public static void ClipToRangeInPlace(this Volume3D<short> image, MinMax<short> range)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

			if (range.Minimum > range.Maximum)
			{
				throw new ArgumentException($"Invalid range provided: Minimum = {range.Minimum}, Maximum = {range.Maximum}", nameof(range));
			}

            var array = image.Array;
            for (var index = 0; index < array.Length; index++)
            {
                var value = array[index];
                if (value < range.Minimum)
                {
                    array[index] = range.Minimum;
                }
                else if (value > range.Maximum)
                {
                    array[index] = range.Maximum;
                }
            }
        }

        /// <summary>
        /// Creates a binary mask by thresholding the present volume. All voxels that have
        /// a value larger or equal than the threshold will be set to 1
		/// (<see cref="ModelConstants.MaskForegroundIntensity">) in the result,
		/// all other voxels will be set to 0 (<see cref="ModelConstants.MaskBackgroundIntensity">)
		/// </summary>
        /// <param name="geoDistanceByte">A volume that stores geodesic distances.</param>
        /// <param name="geoThreshold">The minimum distance that is required for a voxel to be considered foreground.</param>
        /// <returns></returns>
        public static Volume3D<byte> Threshold(
            this Volume3D<short> volume, 
            short threshold)
        {
            return volume.Map(value =>
                value >= threshold
                ? ModelConstants.MaskForegroundIntensity
                : ModelConstants.MaskBackgroundIntensity);
        }

	        /// <summary>
        /// Gets the region of the volume that contains all voxel values that are
		/// larger or equal than the interestId.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="interestId">The voxel values to search for. Foreground is
		/// considered to be all voxels with a value larger or equal to the interestId.</param>
        /// <returns></returns>
		public static Region3D<int> GetInterestRegion(this Volume3D<ushort> volume, ushort interestId)
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
                maximumX[z] = int.MinValue;
                maximumY[z] = int.MinValue;
                maximumZ[z] = int.MinValue;

                for (int y = 0; y < volume.DimY; y++)
                {
                    var delta = y * volume.DimX + z * volume.DimXY;

                    for (int x = 0; x < volume.DimX; x++)
                    {
                        if (volume[x + delta] >= interestId)
                        {
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
                }
            });

            var region = new Region3D<int>(minimumX.Min(), minimumY.Min(), minimumZ.Min(), maximumX.Max(), maximumY.Max(), maximumZ.Max());
            
			// If no foreground values are found, the region minimum will be Int.MaxValue, maximum will be Int.MinValue.
			// When accidentally doing operations on that region, it will most likely lead to numerical
			// overflow or underflow. Instead, return an empty region that has less troublesome boundary values.
			return region.IsEmpty() ? RegionExtensions.EmptyIntRegion() : region;
        }

		/// <summary>
		/// Returns the maximum and minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static MinMax<ushort> GetMinMax(this ushort[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = ushort.MaxValue;			
			var maxValue = ushort.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}

				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return new MinMax<ushort>() { Maximum = maxValue, Minimum = minValue };
		}

		/// <summary>
		/// Returns the difference between maximum and minimum in the given range.
		/// </summary>
		public static ushort Range(this MinMax<ushort> minMax)
		{
			if (minMax.Minimum > minMax.Maximum)
			{
				throw new ArgumentException("The minimum must not be larger than the maximum", nameof(minMax));
			}

			return (ushort)(minMax.Maximum - minMax.Minimum);
		}

		/// <summary>
		/// If the given value falls inside the range stored in <paramref name="minMax"/>, return the
		/// value unchanged. If the value is smaller than the range minimum, return the minimum.
		/// If the value is larger than the range maximum, return the maximum.
		/// </summary>
		public static ushort Clamp(this MinMax<ushort> minMax, ushort value)
		{
			if (value < minMax.Minimum)
			{
				return minMax.Minimum;
			}

			if (value > minMax.Maximum)
			{
				return minMax.Maximum;
			}

			return value;
		}

		/// <summary>
		/// Returns the minimum and maximum voxel value in the volume.
		/// Throws an ArgumentNullException if the volume is null.
		/// </summary>
		public static MinMax<ushort> GetMinMax(this Volume<ushort> volume)
		{
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}

			return volume.Array.GetMinMax();
		}

		/// <summary>
		/// Returns the minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static ushort Minimum(this ushort[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = ushort.MaxValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}
			}

			return minValue;
		}

		/// <summary>
		/// Returns the maximum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static ushort Maximum(this ushort[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var maxValue = ushort.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return maxValue;
		}

        /// <summary>
        /// Computes the minimum and the maximum of a sequence of values, in a single pass through the data.
        /// Returns null if the input sequence is empty.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MinMax<ushort>? GetRange(this IEnumerable<ushort> stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var max = ushort.MinValue;
            var min = ushort.MaxValue;
            var any = false;
            foreach (var d in stream)
            {
                any = true;
                if (d > max)
                {
                    max = d;
                }

                if (d < min)
                {
                    min = d;
                }
            }

            if (!any)
            {
                return null;
            }

            return MinMax.Create(min, max);
        }

        /// <summary>
        /// Checks whether all of the voxel values in the input volume are inside of the given <see cref="range"/>.
        /// If a voxel value is below the given minimum, it is changed to be exactly at the minimum.
        /// If a voxel value is above the given maximum, it is changed to be exactly at the maximum.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="range"></param>
        public static void ClipToRangeInPlace(this Volume3D<ushort> image, MinMax<ushort> range)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

			if (range.Minimum > range.Maximum)
			{
				throw new ArgumentException($"Invalid range provided: Minimum = {range.Minimum}, Maximum = {range.Maximum}", nameof(range));
			}

            var array = image.Array;
            for (var index = 0; index < array.Length; index++)
            {
                var value = array[index];
                if (value < range.Minimum)
                {
                    array[index] = range.Minimum;
                }
                else if (value > range.Maximum)
                {
                    array[index] = range.Maximum;
                }
            }
        }

        /// <summary>
        /// Creates a binary mask by thresholding the present volume. All voxels that have
        /// a value larger or equal than the threshold will be set to 1
		/// (<see cref="ModelConstants.MaskForegroundIntensity">) in the result,
		/// all other voxels will be set to 0 (<see cref="ModelConstants.MaskBackgroundIntensity">)
		/// </summary>
        /// <param name="geoDistanceByte">A volume that stores geodesic distances.</param>
        /// <param name="geoThreshold">The minimum distance that is required for a voxel to be considered foreground.</param>
        /// <returns></returns>
        public static Volume3D<byte> Threshold(
            this Volume3D<ushort> volume, 
            ushort threshold)
        {
            return volume.Map(value =>
                value >= threshold
                ? ModelConstants.MaskForegroundIntensity
                : ModelConstants.MaskBackgroundIntensity);
        }

	        /// <summary>
        /// Gets the region of the volume that contains all voxel values that are
		/// larger or equal than the interestId.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="interestId">The voxel values to search for. Foreground is
		/// considered to be all voxels with a value larger or equal to the interestId.</param>
        /// <returns></returns>
		public static Region3D<int> GetInterestRegion(this Volume3D<float> volume, float interestId)
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
                maximumX[z] = int.MinValue;
                maximumY[z] = int.MinValue;
                maximumZ[z] = int.MinValue;

                for (int y = 0; y < volume.DimY; y++)
                {
                    var delta = y * volume.DimX + z * volume.DimXY;

                    for (int x = 0; x < volume.DimX; x++)
                    {
                        if (volume[x + delta] >= interestId)
                        {
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
                }
            });

            var region = new Region3D<int>(minimumX.Min(), minimumY.Min(), minimumZ.Min(), maximumX.Max(), maximumY.Max(), maximumZ.Max());
            
			// If no foreground values are found, the region minimum will be Int.MaxValue, maximum will be Int.MinValue.
			// When accidentally doing operations on that region, it will most likely lead to numerical
			// overflow or underflow. Instead, return an empty region that has less troublesome boundary values.
			return region.IsEmpty() ? RegionExtensions.EmptyIntRegion() : region;
        }

		/// <summary>
		/// Returns the maximum and minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static MinMax<float> GetMinMax(this float[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = float.MaxValue;			
			var maxValue = float.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}

				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return new MinMax<float>() { Maximum = maxValue, Minimum = minValue };
		}

		/// <summary>
		/// Returns the difference between maximum and minimum in the given range.
		/// </summary>
		public static float Range(this MinMax<float> minMax)
		{
			if (minMax.Minimum > minMax.Maximum)
			{
				throw new ArgumentException("The minimum must not be larger than the maximum", nameof(minMax));
			}

			return (float)(minMax.Maximum - minMax.Minimum);
		}

		/// <summary>
		/// If the given value falls inside the range stored in <paramref name="minMax"/>, return the
		/// value unchanged. If the value is smaller than the range minimum, return the minimum.
		/// If the value is larger than the range maximum, return the maximum.
		/// </summary>
		public static float Clamp(this MinMax<float> minMax, float value)
		{
			if (value < minMax.Minimum)
			{
				return minMax.Minimum;
			}

			if (value > minMax.Maximum)
			{
				return minMax.Maximum;
			}

			return value;
		}

		/// <summary>
		/// Returns the minimum and maximum voxel value in the volume.
		/// Throws an ArgumentNullException if the volume is null.
		/// </summary>
		public static MinMax<float> GetMinMax(this Volume<float> volume)
		{
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}

			return volume.Array.GetMinMax();
		}

		/// <summary>
		/// Returns the minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static float Minimum(this float[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = float.MaxValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}
			}

			return minValue;
		}

		/// <summary>
		/// Returns the maximum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static float Maximum(this float[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var maxValue = float.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return maxValue;
		}

        /// <summary>
        /// Computes the minimum and the maximum of a sequence of values, in a single pass through the data.
        /// Returns null if the input sequence is empty.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MinMax<float>? GetRange(this IEnumerable<float> stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var max = float.MinValue;
            var min = float.MaxValue;
            var any = false;
            foreach (var d in stream)
            {
                any = true;
                if (d > max)
                {
                    max = d;
                }

                if (d < min)
                {
                    min = d;
                }
            }

            if (!any)
            {
                return null;
            }

            return MinMax.Create(min, max);
        }

        /// <summary>
        /// Checks whether all of the voxel values in the input volume are inside of the given <see cref="range"/>.
        /// If a voxel value is below the given minimum, it is changed to be exactly at the minimum.
        /// If a voxel value is above the given maximum, it is changed to be exactly at the maximum.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="range"></param>
        public static void ClipToRangeInPlace(this Volume3D<float> image, MinMax<float> range)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

			if (range.Minimum > range.Maximum)
			{
				throw new ArgumentException($"Invalid range provided: Minimum = {range.Minimum}, Maximum = {range.Maximum}", nameof(range));
			}

            var array = image.Array;
            for (var index = 0; index < array.Length; index++)
            {
                var value = array[index];
                if (value < range.Minimum)
                {
                    array[index] = range.Minimum;
                }
                else if (value > range.Maximum)
                {
                    array[index] = range.Maximum;
                }
            }
        }

        /// <summary>
        /// Creates a binary mask by thresholding the present volume. All voxels that have
        /// a value larger or equal than the threshold will be set to 1
		/// (<see cref="ModelConstants.MaskForegroundIntensity">) in the result,
		/// all other voxels will be set to 0 (<see cref="ModelConstants.MaskBackgroundIntensity">)
		/// </summary>
        /// <param name="geoDistanceByte">A volume that stores geodesic distances.</param>
        /// <param name="geoThreshold">The minimum distance that is required for a voxel to be considered foreground.</param>
        /// <returns></returns>
        public static Volume3D<byte> Threshold(
            this Volume3D<float> volume, 
            float threshold)
        {
            return volume.Map(value =>
                value >= threshold
                ? ModelConstants.MaskForegroundIntensity
                : ModelConstants.MaskBackgroundIntensity);
        }

	        /// <summary>
        /// Gets the region of the volume that contains all voxel values that are
		/// larger or equal than the interestId.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="interestId">The voxel values to search for. Foreground is
		/// considered to be all voxels with a value larger or equal to the interestId.</param>
        /// <returns></returns>
		public static Region3D<int> GetInterestRegion(this Volume3D<byte> volume, byte interestId)
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
                maximumX[z] = int.MinValue;
                maximumY[z] = int.MinValue;
                maximumZ[z] = int.MinValue;

                for (int y = 0; y < volume.DimY; y++)
                {
                    var delta = y * volume.DimX + z * volume.DimXY;

                    for (int x = 0; x < volume.DimX; x++)
                    {
                        if (volume[x + delta] >= interestId)
                        {
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
                }
            });

            var region = new Region3D<int>(minimumX.Min(), minimumY.Min(), minimumZ.Min(), maximumX.Max(), maximumY.Max(), maximumZ.Max());
            
			// If no foreground values are found, the region minimum will be Int.MaxValue, maximum will be Int.MinValue.
			// When accidentally doing operations on that region, it will most likely lead to numerical
			// overflow or underflow. Instead, return an empty region that has less troublesome boundary values.
			return region.IsEmpty() ? RegionExtensions.EmptyIntRegion() : region;
        }

		/// <summary>
		/// Returns the maximum and minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static MinMax<byte> GetMinMax(this byte[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = byte.MaxValue;			
			var maxValue = byte.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}

				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return new MinMax<byte>() { Maximum = maxValue, Minimum = minValue };
		}

		/// <summary>
		/// Returns the difference between maximum and minimum in the given range.
		/// </summary>
		public static byte Range(this MinMax<byte> minMax)
		{
			if (minMax.Minimum > minMax.Maximum)
			{
				throw new ArgumentException("The minimum must not be larger than the maximum", nameof(minMax));
			}

			return (byte)(minMax.Maximum - minMax.Minimum);
		}

		/// <summary>
		/// If the given value falls inside the range stored in <paramref name="minMax"/>, return the
		/// value unchanged. If the value is smaller than the range minimum, return the minimum.
		/// If the value is larger than the range maximum, return the maximum.
		/// </summary>
		public static byte Clamp(this MinMax<byte> minMax, byte value)
		{
			if (value < minMax.Minimum)
			{
				return minMax.Minimum;
			}

			if (value > minMax.Maximum)
			{
				return minMax.Maximum;
			}

			return value;
		}

		/// <summary>
		/// Returns the minimum and maximum voxel value in the volume.
		/// Throws an ArgumentNullException if the volume is null.
		/// </summary>
		public static MinMax<byte> GetMinMax(this Volume<byte> volume)
		{
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}

			return volume.Array.GetMinMax();
		}

		/// <summary>
		/// Returns the minimum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static byte Minimum(this byte[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var minValue = byte.MaxValue;
			foreach (var currentValue in array)
			{
				if (currentValue < minValue)
				{
					minValue = currentValue;
				}
			}

			return minValue;
		}

		/// <summary>
		/// Returns the maximum of the array.
		/// Throws an ArgumentNullException if the array is null.
		/// Throws an ArgumentException if the array has no element.
		/// </summary>
		public static byte Maximum(this byte[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (array.Length == 0)
			{
				throw new ArgumentException("The array must have non-zero length");
			}

			var maxValue = byte.MinValue;
			foreach (var currentValue in array)
			{
				if (currentValue > maxValue)
				{
					maxValue = currentValue;
				}
			}

			return maxValue;
		}

        /// <summary>
        /// Computes the minimum and the maximum of a sequence of values, in a single pass through the data.
        /// Returns null if the input sequence is empty.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MinMax<byte>? GetRange(this IEnumerable<byte> stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var max = byte.MinValue;
            var min = byte.MaxValue;
            var any = false;
            foreach (var d in stream)
            {
                any = true;
                if (d > max)
                {
                    max = d;
                }

                if (d < min)
                {
                    min = d;
                }
            }

            if (!any)
            {
                return null;
            }

            return MinMax.Create(min, max);
        }

        /// <summary>
        /// Checks whether all of the voxel values in the input volume are inside of the given <see cref="range"/>.
        /// If a voxel value is below the given minimum, it is changed to be exactly at the minimum.
        /// If a voxel value is above the given maximum, it is changed to be exactly at the maximum.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="range"></param>
        public static void ClipToRangeInPlace(this Volume3D<byte> image, MinMax<byte> range)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

			if (range.Minimum > range.Maximum)
			{
				throw new ArgumentException($"Invalid range provided: Minimum = {range.Minimum}, Maximum = {range.Maximum}", nameof(range));
			}

            var array = image.Array;
            for (var index = 0; index < array.Length; index++)
            {
                var value = array[index];
                if (value < range.Minimum)
                {
                    array[index] = range.Minimum;
                }
                else if (value > range.Maximum)
                {
                    array[index] = range.Maximum;
                }
            }
        }

        /// <summary>
        /// Creates a binary mask by thresholding the present volume. All voxels that have
        /// a value larger or equal than the threshold will be set to 1
		/// (<see cref="ModelConstants.MaskForegroundIntensity">) in the result,
		/// all other voxels will be set to 0 (<see cref="ModelConstants.MaskBackgroundIntensity">)
		/// </summary>
        /// <param name="geoDistanceByte">A volume that stores geodesic distances.</param>
        /// <param name="geoThreshold">The minimum distance that is required for a voxel to be considered foreground.</param>
        /// <returns></returns>
        public static Volume3D<byte> Threshold(
            this Volume3D<byte> volume, 
            byte threshold)
        {
            return volume.Map(value =>
                value >= threshold
                ? ModelConstants.MaskForegroundIntensity
                : ModelConstants.MaskBackgroundIntensity);
        }

	
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<byte> volume, MinMax<byte> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<byte> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<short> volume, MinMax<short> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<short> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<float> volume, MinMax<float> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<float> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<double> volume, MinMax<double> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume3D<byte> ScaleToByteRange(this Volume3D<double> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<byte> volume, MinMax<byte> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<byte> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<short> volume, MinMax<short> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<short> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<float> volume, MinMax<float> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<float> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	     /// <summary>
        /// Converts the volume to byte range. Voxels that have a value at or below the 
		/// minimum that is given in <paramref name="minMax"/>.Minimum will be mapped
        /// to 0. Voxels that have a value at or above the maximum given in <paramref name="minMax"/>.Maximum
		/// will be mapped to <see cref="byte.MaxValue"/>. Voxel values in between are linearly scaled.
        /// If the given minimum is equal to the maximum, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="minMax">The minimum and maximum voxel values that are used for computing the linear mapping.</param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<double> volume, MinMax<double> minMax)
        {
			if (volume == null)
			{
				throw new ArgumentNullException(nameof(volume));
			}
            var range = minMax.Maximum - minMax.Minimum;
            if (range <= 0.0f)
            {
                return volume.CreateSameSize<byte>();
            }

            var scale = (double)byte.MaxValue / range;
            return volume.Map(value => Converters.ClampToByte(scale * (value - minMax.Minimum)));
        }
		
		/// <summary>
        /// Converts the volume to byte range. The minimum voxel value in the input volume will be mapped
        /// to 0, the maximum voxel value to <see cref="byte.MaxValue"/>.
        /// If all voxel values in the input image have the same value, the result is all zeroes.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static Volume2D<byte> ScaleToByteRange(this Volume2D<double> volume)
			=> ScaleToByteRange(volume, volume.GetMinMax());
	}
}