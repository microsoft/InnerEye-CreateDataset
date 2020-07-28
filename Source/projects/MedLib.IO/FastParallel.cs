///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Contains method for running parallel loops, that are optimized for running over large indexing ranges.
    /// </summary>
    public static class FastParallel
    {
        /// <summary>
        /// Get the starting index and end index (inclusive) when dividing a set of <paramref name="count"/> items
        /// into roughly equal sized batches (+- 1), and processing the batch with index given in <paramref name="currentBatch"/>.
        /// If there are more batches than items, return (0, -1) for the batches that have nothing to do.
        /// </summary>
        /// <param name="count">The total number of items to process. Valid indices are from 0 to (items - 1).</param>
        /// <param name="currentBatch">The currently processed batch. Valid batch numbers are 0 to 
        /// (totalBatches - 1).</param>
        /// <param name="totalBatches">The total number of batches.</param>
        /// <returns></returns>
        public static (int FirstIndex, int LastIndex) BatchBoundaries(int count, int currentBatch, int totalBatches)
        {
            if (count < 0)
            {
                throw new ArgumentException("The number of items must be 0 or more", nameof(count));
            }

            if (totalBatches < 1)
            {
                throw new ArgumentException("The number of batches must be 1 or more", nameof(totalBatches));
            }

            if (currentBatch < 0 || currentBatch >= totalBatches)
            {
                throw new ArgumentException("The current batch index must be in the range (0, total number of batches - 1).", nameof(currentBatch));
            }

            var maxIndex = count - 1;
            // In double arithmetic, this would be (int)Math.Ceiling((float)count / totalBatches);
            var batchSize = (count + totalBatches - 1) / totalBatches;
            var firstIndex = currentBatch * batchSize;
            if (firstIndex <= maxIndex)
            {
                var lastIndex = Math.Min(maxIndex, firstIndex + batchSize - 1);
                return (firstIndex, lastIndex);
            }
            return (0, -1);
        }

        /// <summary>
        /// Applies a function to each element of the input array, writing the result to the corresponding output array
        /// (that is, outArray[i] = func(inArray[i]))
        /// Processing is done in parallel threads, using at most <paramref name="maxThreads"/> threads.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inArray">The array with the input values.</param>
        /// <param name="outArray">The array where the results should be written to.</param>
        /// <param name="func">The function that transforms an input value into an output value.</param>
        /// <param name="maxThreads">The maximum number of parallel threads to use. If null, a plain vanilla for loop
        /// is used instead of a parallel for loop.</param>
        public static void MapToArray<TIn, TOut>(this TIn[] inArray, TOut[] outArray, int? maxThreads, Func<TIn, TOut> func)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(nameof(inArray));
            }

            if (outArray == null)
            {
                throw new ArgumentNullException(nameof(outArray));
            }

            if (outArray.Length != inArray.Length)
            {
                throw new ArgumentException("The output array must have the same size as the input array.", nameof(outArray));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (maxThreads == null)
            {
                for (var i = 0; i < outArray.Length; ++i)
                {
                    outArray[i] = func(inArray[i]);
                }
            }
            else
            {
                if (maxThreads.Value < 1)
                {
                    throw new ArgumentException("The number of threads must be 1 or more", nameof(maxThreads));
                }
                Parallel.For(0, maxThreads.Value, thread =>
                {
                    var (firstIndex, lastIndex) = BatchBoundaries(inArray.Length, thread, maxThreads.Value);
                    for (var i = firstIndex; i <= lastIndex; i++)
                    {
                        outArray[i] = func(inArray[i]);
                    }
                });
            }
        }


        /// <summary>
        /// Applies a function to each element of the input array, writing the result to the corresponding output array
        /// (that is, outArray[i] = func(inArray[i], i)). The function takes an array element and its index as arguments.
        /// Processing is done in parallel threads, using at most <paramref name="maxThreads"/> threads.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inArray">The array with the input values.</param>
        /// <param name="outArray">The array where the results should be written to.</param>
        /// <param name="func">The function that transforms an input value into an output value.</param>
        /// <param name="maxThreads">The maximum number of parallel threads to use. If null, a plain vanilla for loop
        /// is used instead of a parallel for loop.</param>
        public static void MapToArrayIndexed<TIn, TOut>(this TIn[] inArray, TOut[] outArray, int? maxThreads, Func<TIn, int, TOut> func)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(nameof(inArray));
            }

            if (outArray == null)
            {
                throw new ArgumentNullException(nameof(outArray));
            }

            if (outArray.Length != inArray.Length)
            {
                throw new ArgumentException("The output array must have the same size as the input array.", nameof(outArray));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (maxThreads == null)
            {
                for (var i = 0; i < outArray.Length; ++i)
                {
                    outArray[i] = func(inArray[i], i);
                }
            }
            else
            {
                if (maxThreads.Value < 1)
                {
                    throw new ArgumentException("The number of threads must be 1 or more", nameof(maxThreads));
                }
                Parallel.For(0, maxThreads.Value, thread =>
                {
                    var (firstIndex, lastIndex) = BatchBoundaries(inArray.Length, thread, maxThreads.Value);
                    for (var i = firstIndex; i <= lastIndex; i++)
                    {
                        outArray[i] = func(inArray[i], i);
                    }
                });
            }
        }

        /// <summary>
        /// Applies an action to each element in an index sequence, from 0 to (count - 1).
        /// Processing is done in parallel threads, using at most <paramref name="maxThreads"/> threads.
        /// </summary>
        /// <param name="count">Total actions to perform</param>
        /// <param name="action">The action to call for each element in the index sequence.</param>
        /// <param name="maxThreads">The maximum number of parallel threads to use. If null, a plain vanilla for loop
        /// is used instead of a parallel for loop.</param>
        public static void Loop(int count, int? maxThreads, Action<int> action)
        {
            BatchLoop(count, maxThreads, DefaultBatchLoop(action));
        }

        /// <summary>
        /// Break the range [0, count-1] up into n contiguous regions, calling batchAction for each region. batcAction will be 
        /// called n times each (potentially) on a seperate thread with the inclusive range [firstIndex, lastIndex], where n
        /// is at most <paramref name="maxThreads"/>
        /// </summary>
        /// <remarks>
        /// Exceptions thrown from batchAction will immediately terminate parallel processing and raise the exceptions
        /// with the caller
        /// </remarks>
        /// <param name="count">The count.</param>
        /// <param name="maxThreads">The maximum threads to use or null to call batchAction on the current thread.</param>
        /// <param name="batchAction">The action to execute over a contiguous inclusive range of indices [firstIndex, lastIndex]</param>
        /// <exception cref="ArgumentNullException">batchAction</exception>
        /// <exception cref="ArgumentException">The number of threads must be 1 or more - maxThreads</exception>
        public static void BatchLoop(int count, int? maxThreads, Action<int, int> batchAction)
        {
            batchAction = batchAction ?? throw new ArgumentNullException(nameof(batchAction));

            if (maxThreads == null)
            {
                batchAction(0, count - 1);
            }
            else
            {
                if (maxThreads.Value < 1)
                {
                    throw new ArgumentException("The number of threads must be 1 or more", nameof(maxThreads));
                }
                Parallel.For(0, maxThreads.Value, thread =>
                {
                    var (firstIndex, lastIndex) = BatchBoundaries(count, thread, maxThreads.Value);
                    batchAction(firstIndex, lastIndex);
                });
            }
        }

        /// <summary>
        /// A default batch action that will call elementAction on each index in the range [firstIndex, lastIndex]. Use 
        /// in conjunction with BatchLoop for parallel processing of elementAction over a range of contiguous values
        /// </summary>
        /// <param name="elementAction">Action to apply to the value at the given array index.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">elementAction</exception>
        public static Action<int, int> DefaultBatchLoop(Action<int> elementAction)
        {
            if (elementAction == null)
            {
                throw new ArgumentNullException(nameof(elementAction));
            }

            return (firstIndex, lastIndex) =>
            {
                for (var i = firstIndex; i <= lastIndex; i++)
                {
                    elementAction(i);
                }
            };
        }
    }
}
