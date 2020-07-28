///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System;

    public static class GenericExtensions
    {
        /// <summary>
        /// Sets all entries in the array to the given value.
        /// </summary>
        public static void Fill<T>(this T[] array, T value)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        /// <summary>
        /// Creates an array of the given size, and fills all elements with
        /// the provided value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="size">The length of the array that should be returned.</param>
        /// <param name="value">The value to use for each element in the returned array.</param>
        /// <returns></returns>
        public static T[] CreateArray<T>(int size, T value)
        {
            if (size < 0)
            {
                throw new ArgumentException("The array size must be non-negative.", nameof(size));
            }

            var array = new T[size];
            Fill(array, value);
            return array;
        }

        /// <summary>
        /// Copies all values from an array to the destination.
        /// </summary>
        public static void CopyTo<T>(this T[] array, T[] destination)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (array.Length != destination.Length)
            {
                throw new ArgumentException("Both arrays need to have the same length.");
            }

            for (int i = 0; i < array.Length; i++)
            {
                destination[i] = array[i];
            }
        }
    }
}
