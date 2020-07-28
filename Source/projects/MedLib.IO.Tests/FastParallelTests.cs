///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using MedLib.IO;
    using NUnit.Framework;

    [TestFixture]
    public class FastParallelTests
    {
        [Test]
        // maxThreads == null should run a plain vanilla for loop.
        [TestCase(null)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(10)]
        public void FastParallelLoop(int? maxThreads)
        {
            FastParallel.Loop(0, maxThreads, _ => Assert.Fail("When count is empty, no action should be called"));
            foreach (var count in Enumerable.Range(1, 20))
            {
                var executed = new ConcurrentBag<int>();
                FastParallel.Loop(count, maxThreads, index =>
                {
                    if (executed.Contains(index))
                    {
                        Assert.Fail($"Action for index {index} has been called already.");
                    }
                    executed.Add(index);
                });
                var expected = Enumerable.Range(0, count).ToArray();
                var actual = executed.ToArray();
                Array.Sort(actual);
                Assert.AreEqual(expected, actual, $"count = {count}: The set of executed actions is wrong");
            }
        }

        [Test]
        // maxThreads == null should run a plain vanilla for loop.
        public void FastParallelLoopErrors()
        {
            Assert.Throws<ArgumentException>(() => FastParallel.Loop(0, 0, i => { }));
            Assert.Throws<ArgumentNullException>(() => FastParallel.Loop(0, 0, null));
        }

        [Test]
        // maxThreads == null should run a plain vanilla for loop.
        [TestCase(null, 0)]
        [TestCase(null, 1)]
        [TestCase(1, 0)]
        [TestCase(1, 10)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(10, 0)]
        [TestCase(10, 1)]
        [TestCase(10, 30)]
        public void FastParallelMapToArray(int? maxThreads, int count)
        {
            var inArray = new int[count];
            foreach (var index in Enumerable.Range(0, count))
            {
                inArray[index] = index;
            }
            var outArray = new int[count];
            FastParallel.MapToArray(inArray, outArray, maxThreads, value => value + 1);
            foreach (var index in Enumerable.Range(0, count))
            {
                Assert.AreEqual(index, inArray[index]);
                Assert.AreEqual(inArray[index] + 1, outArray[index]);
            }
        }

        [Test]
        public void FastParallelMapToArrayErrors()
        {
            var a = new int[1];
            var b = new int[2];
            Assert.Throws<ArgumentNullException>(() => FastParallel.MapToArray<int,int>(null, a, null, i => i));
            Assert.Throws<ArgumentNullException>(() => FastParallel.MapToArray(a, null, null, i => i));
            Assert.Throws<ArgumentNullException>(() => FastParallel.MapToArray(a, a, null, null));
            Assert.Throws<ArgumentException>(() => FastParallel.MapToArray(a, a, 0, i => i));
            Assert.Throws<ArgumentException>(() => FastParallel.MapToArray(a, b, 0, i => i));
        }

        [Test]
        // maxThreads == null should run a plain vanilla for loop.
        [TestCase(null, 0)]
        [TestCase(null, 1)]
        [TestCase(1, 0)]
        [TestCase(1, 10)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(10, 0)]
        [TestCase(10, 1)]
        [TestCase(10, 30)]
        public void FastParallelMapToArrayIndexed(int? maxThreads, int count)
        {
            var inArray = new int[count];
            foreach (var index in Enumerable.Range(0, count))
            {
                inArray[index] = index + 1;
            }
            var outArray = new int[count];
            FastParallel.MapToArrayIndexed(inArray, outArray, maxThreads, (value, index) => value + index * 2);
            foreach (var index in Enumerable.Range(0, count))
            {
                Assert.AreEqual(index + 1, inArray[index]);
                Assert.AreEqual(inArray[index] + index * 2, outArray[index]);
            }
        }

        [Test]
        public void FastParallelMapToArrayIndexedErrors()
        {
            var a = new int[1];
            var b = new int[2];
            Assert.Throws<ArgumentNullException>(() => FastParallel.MapToArrayIndexed<int, int>(null, a, null, (i,index) => i));
            Assert.Throws<ArgumentNullException>(() => FastParallel.MapToArrayIndexed(a, null, null, (i, index) => i));
            Assert.Throws<ArgumentNullException>(() => FastParallel.MapToArrayIndexed(a, a, null, null));
            Assert.Throws<ArgumentException>(() => FastParallel.MapToArrayIndexed(a, a, 0, (i, index) => i));
            Assert.Throws<ArgumentException>(() => FastParallel.MapToArrayIndexed(a, b, 0, (i, index) => i));
        }

        [Test]
        // No item to process
        [TestCase(0, 0, 1, 0, -1)]
        // Single item, processing in 1 batch: Loop from 0 to 0
        [TestCase(1, 0, 1, 0, 0)]
        // Single item, processing in 2 batch: first batch processes the item, second batch has nothing to do.
        [TestCase(1, 0, 2, 0, 0)]
        [TestCase(1, 1, 2, 0, -1)]
        // Three items: Testing computation of batch size. Batch size should be rounded upwards,
        // giving the first batch from 0 to 1, second from 2 to 2.
        [TestCase(3, 0, 2, 0, 1)]
        [TestCase(3, 1, 2, 2, 2)]
        public void FastParallelBatchBoundaries(int count, int currentBatch, int totalBatches, int expectedStart, int expectedEnd)
        {
            var (firstIndex, lastIndex) = FastParallel.BatchBoundaries(count, currentBatch, totalBatches);
            Assert.AreEqual(expectedStart, firstIndex, "firstIndex");
            Assert.AreEqual(expectedEnd, lastIndex, "lastIndex");
        }

        [Test]
        [TestCase(-1, 0, 1)]
        [TestCase(0, 0, 0)]
        [TestCase(0, 1, 1)]
        [TestCase(0, -1, 1)]
        public void FastParallelBatchBoundariesErrors(int count, int currentBatch, int totalBatches)
        {
            Assert.Throws<ArgumentException>(() => FastParallel.BatchBoundaries(count, currentBatch, totalBatches));
        }
    }
}
