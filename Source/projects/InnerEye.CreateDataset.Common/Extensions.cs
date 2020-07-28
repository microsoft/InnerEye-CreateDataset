///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public static class Extensions
    {
        /// <summary>
        /// Applies the provided function to all items in the dictionary in place
        /// </summary>
        public static void MapInPlace<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, Func<TVal, TVal> func)
        {
            foreach (var k in dictionary.Keys.ToList())
            {
                dictionary[k] = func(dictionary[k]);
            }
        }

        /// <summary>
        /// Maps the provided value to be between [min,max] range
        /// </summary>
        public static double Clamp(this double val, double min, double max)
        {
            if(max < min)
            {
                throw new ArgumentException($"Could not clamp {val}, to bounds [{min},{max}] as max < min");
            }
            else
            {
                return val < min ? min : (val > max ? max : val);
            }
        }
            

        /// <summary>
        /// Enumerates 2 sequences at the same time, and calls the action on matching sequence elements.
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="action"></param>
        public static void Pairwise<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Action<TFirst, TSecond> action)
        {
            using (var e1 = first.GetEnumerator())
            {
                using (var e2 = second.GetEnumerator())
                {
                    while (e1.MoveNext() && e2.MoveNext())
                    {
                        action.Invoke(e1.Current, e2.Current);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an enumerable that contains the argument as its single element.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<T> Singleton<T>(T obj)
        {
            yield return obj;
        }

        /// <summary>
        /// Returns an enumerable of indices of the array or enumerable, starting at 0.
        /// CAUTION: The method creates some performance overhead.
        /// Do not use for large arrays, like those inside Volume3D. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<int> Indices<T>(this IEnumerable<T> seq)
        {
            if (seq is Array array)
            {
                var count = array.Length;
                for (var index = 0; index < count; index++)
                {
                    yield return index;
                }
            }
            else
            {
                var position = 0;
                foreach (var _ in seq)
                {
                    yield return position;
                    position++;
                }
            }
        }

        /// <summary>
        /// Returns true if the array is either null or has no elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array == null || array.Length == 0;
        }

        /// <summary>
        /// Returns true if the list is either null or has no elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>
        /// Permutes the array in-place, using the given random number generator, by swapping
        /// array elements at random positions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="rng"></param>
        public static void Shuffle<T>(this T[] array, Random rng)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }


        public static void PartialShuffle<T>(this T[] array, Random rng, int lastIndex)
        {
            int i = -1;
            int n = array.Length;
            int index = lastIndex < n ? lastIndex : (n - 1);

            while (i < index)
            {
                int k = rng.Next(++i, n);
                T temp = array[i];
                array[i] = array[k];
                array[k] = temp;
            }
        }

        /// <summary>
        /// Runs a parallel 'for' loop over the input array. The given function is called with an array
        /// element and its index as arguments. The returned array contains the function results
        /// across the whole loop.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="array"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static TOut[] ParallelSelect<TIn,TOut>(this TIn[] array, Func<TIn,int,TOut> func)
        {
            var result = new TOut[array.Length];
            Parallel.For(
                0,
                array.Length,
                index =>
                {
                    result[index] = func(array[index], index);
                });
            return result;
        }

        /// <summary>
        /// Runs a parallel 'for' loop over the input array. The given function is called with an
        /// element of the input array. The returned array contains the function results
        /// across the whole loop.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="array"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static TOut[] ParallelSelect<TIn, TOut>(this TIn[] array, Func<TIn, TOut> func)
        {
            var result = new TOut[array.Length];
            Parallel.For(
                0,
                array.Length,
                index =>
                {
                    result[index] = func(array[index]);
                });
            return result;
        }

        /// <summary>
        /// Creates a clone of the present object using JSON serialization and de-serialization.
        /// This method is not fast. The object in question must be serializable using JSON.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepClone<T>(this T obj)
        {
            var serialized = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        /// <summary>
        /// Returns an emumerable with all values that are contained more than 1 time in the input sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> values) =>
            (values ?? throw new ArgumentNullException(nameof(values)))
            .GroupBy(value => value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
    }
}