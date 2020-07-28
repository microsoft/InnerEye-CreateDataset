///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Stores the information about one bin entry of a histogram over <see cref="short"/> values.
    /// </summary>
    [DebuggerDisplay("Bin {Index}: {Count} values >= {MinimumInclusive}")]
    public class HistogramBin
    {
        /// <summary>
        /// Gets the index of bin inside the histogram. Index can have values from 0 to 
        /// (number of bins) - 1.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the minimum value that is counted as belonging to this bin.
        /// </summary>
        public short MinimumInclusive { get; }

        /// <summary>
        /// Gets or sets the number of values in this histogram bin.
        /// </summary>
        public int Count { get; set; } = 0;

        /// <summary>
        /// Creates a new histogram bin, with the count set to zero.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="minimumInclusive"></param>
        public HistogramBin(int index, short minimumInclusive)
        {
            Index = index;
            MinimumInclusive = minimumInclusive;
        }
    }

    /// <summary>
    /// Helper class for auto-windowing a volume.
    /// </summary>
    public static class AutoWindowLevelHelper
    {
        /// <summary>
        /// The number of keys in the histogram.
        /// </summary>
        private const int DefaultHistogramSize = 150;

        /// <summary>
        /// The number of keys in the histogram, converted to double.
        /// </summary>
        private const double DefaultHistogramSizeDouble = DefaultHistogramSize;

        /// <summary>
        /// The first N number of keys that will be ignored in the histogram.
        /// Currently set to ignore the first 7% of histogram values.
        /// This should be a value between 0-1.
        /// </summary>
        private const double CtMininmumPercentile = 0.07;

        /// <summary>
        /// This is a minimum threshold value when the provided volume is CT. Any value less than this
        /// will be threshold to this value. This is to avoid picking the wrong peak when artifical values
        /// have been used for background.
        /// </summary>
        private const short CtMinimumThreshold = -1000;

        /// <summary>
        /// This is a maximum threshold value when the provided volume is CT. Any value greater than this
        /// will be threshold to this value. This is to avoid picking the wrong peak when artifical values
        /// have been used for background.
        /// </summary>
        private const short CtMaximumThreshold = 2000;

        /// <summary>
        /// The window for a CT image will be at least this percentage further away from the chosen level value.
        /// </summary>
        private const double CtWindowMinimumRestrictionPercentage = 0.025;

        /// <summary>
        /// The window for a CT image will be at least this percentage close to the chosen level value.
        /// </summary>
        private const double CtWindowMaximumRestrictionPercentage = 0.15;

        /// <summary>
        /// The start percentage of histogram values that will be inspected when chosing a level value.
        /// </summary>
        private const double MrMinimumPercentile = 0.25;

        /// <summary>
        /// The finish percentage of histogram values that will be inspected when chosing a level value.
        /// </summary>
        private const double MrMaximumPercentile = 0.38;

        /// <summary>
        /// For MR we create a histogram and find the lowest histogram values in the first X% to Y% of values.
        /// The window is then set to be as far left as possible of the selected window value.
        /// </summary>
        /// <param name="volume">The volume to calculate the window/ level from.</param>
        /// <param name="volumeSkip">
        /// The number of items to skip over when computing the histogram (this is an optimisation to make the compute faster). 
        /// If set to 0, the histogram will look at every voxel in the volume when computing the auto/ window level.
        /// If set to 1, this histogram will look at every other voxel etc.
        /// </param>
        /// <returns>A tuple of the Window (item 1) and Level (item 2)</returns>
        /// <exception cref="ArgumentNullException">The provided volume was null.</exception>
        public static (int Window, int Level) ComputeMrAutoWindowLevel(short[] volume, uint volumeSkip = 5)
        {
            volume = volume ?? throw new ArgumentNullException(nameof(volume));
            var stopwatch = Stopwatch.StartNew();

            var minMax = FindMinMax(volume, volumeSkip);

            // Create a histogram, and find the lowest histogram value in the first X% to Y% of values.
            var levelKeyValue = 
                CreateHistogram(volume, minMax, volumeSkip)
                .Where(hist => hist.Index > MrMinimumPercentile * DefaultHistogramSizeDouble && hist.Index < MrMaximumPercentile * DefaultHistogramSizeDouble)
                .OrderBy(hist => hist.Count)
                .First();

            // Calculate the chosen level value from the histogram key.
            var minMaxDifference = minMax.Range();
            var level = levelKeyValue.MinimumInclusive;

            // For MR we create a wide window from the beginning of the voxel distribution to the level.
            var window = (level - minMax.Minimum) * 2;

            stopwatch.Stop();
            Console.WriteLine($"[{nameof(ComputeMrAutoWindowLevel)}] {stopwatch.ElapsedMilliseconds} milliseconds - Level: {level} Window: {window}");

            return (window, level);
        }

        /// <summary>
        /// For CT we aim to find the highest peak right on the histogram for the level.
        /// For the window we aim to the find lowest point left of the highest right peak within a restricted range.
        /// </summary>
        /// <param name="volume">The CT volume.</param>
        /// <param name="volumeSkip">
        /// The number of items to skip over when computing the histogram (this is an optimisation to make the compute faster). 
        /// If set to 0, the histogram will look at every voxel in the volume when computing the auto/ window level.
        /// If set to 1, this histogram will look at every other voxel etc.
        /// </param>
        /// <returns>A tuple of the Window (item 1) and Level (item 2)</returns>
        /// <exception cref="ArgumentNullException">The provided volume was null.</exception>
        public static (int Window, int Level) ComputeCtAutoWindowLevel(short[] volume, uint volumeSkip = 5)
        {
            volume = volume ?? throw new ArgumentNullException(nameof(volume));
            var stopwatch = Stopwatch.StartNew();

            // Compute min/ max and threshold values.
            var minMax = FindMinMax(volume, volumeSkip);
            minMax = MinMax.Create(Math.Max(minMax.Minimum, CtMinimumThreshold), Math.Min(minMax.Maximum, CtMaximumThreshold));

            // Create a histogram, and order based on values (skipping over the first x% of values).
            var ordered = 
                CreateHistogram(volume, minMax, volumeSkip)
                .Where(hist => hist.Index > CtMininmumPercentile * DefaultHistogramSizeDouble)
                .OrderByDescending(hist => hist.Count)
                .ToArray();

            // We now attempt to pick the highest peak right that is greater than a third of the highest histogram number.
            var levelKey =
                ordered
                .Where(x => x.Count > ordered[0].Count / 3)
                .OrderByDescending(x => x.Index)
                .FirstOrDefault();

            // Calculate the chosen level value from the histogram key.
            var level = (int)levelKey.MinimumInclusive;

            // Create window restrictions as a percentage of the histogram size.
            var windowMinimumRestriction = DefaultHistogramSizeDouble * CtWindowMinimumRestrictionPercentage;
            var windowMaximumRestriction = DefaultHistogramSizeDouble * CtWindowMaximumRestrictionPercentage;

            // Now find the lowest peak between the first item and current chosen level (note window value does not work on the top percentile).
            var windowKeyOrdered = 
                ordered
                .Where(x => x.Index < levelKey.Index - windowMinimumRestriction && x.Index > levelKey.Index - windowMaximumRestriction)
                .OrderBy(x => x.Count)
                .ToArray();

            // Check the window key exists and cacluate the window value.
            var windowLeft =
                windowKeyOrdered.Length > 0
                ? windowKeyOrdered[0].MinimumInclusive
                : minMax.Minimum;
            var window = (level - windowLeft) * 2;

            stopwatch.Stop();
            Console.WriteLine($"[{nameof(ComputeCtAutoWindowLevel)}] {stopwatch.ElapsedMilliseconds} milliseconds - Level: {level} Window: {window}");

            return (window, level);
        }

        /// <summary>
        /// From a histogram, discard bins for high values, such that the total amount of probability mass
        /// discarded does not exceed the value given in <paramref name="fractionOfValuesToDiscard"/>.
        /// For example, if the value given is 0.03, the bins for high values totalling no more than 3% of the
        /// total counts will be discarded. The remaining bins will be returned.
        /// </summary>
        /// <param name="histogram">The per-bin histogram. The histogram is expected to be sorted by bin position,
        /// low values coming first.</param>
        /// <param name="fractionOfValuesToDiscard">The total amount of probability mass to discard. Must be between
        /// 0 and 1.</param>
        /// <returns></returns>
        public static HistogramBin[] TrimHighValues(HistogramBin[] histogram, double fractionOfValuesToDiscard)
        {
            histogram = histogram ?? throw new ArgumentNullException(nameof(histogram));
            if (fractionOfValuesToDiscard< 0 || fractionOfValuesToDiscard >= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fractionOfValuesToDiscard), "The value must be in the range [0, 1].");
            }

            var totalCount = (double)histogram.Sum(bin => bin.Count);
            var elementsToRetain = histogram.Length;
            var massDiscarded = 0.0;
            for (var index = histogram.Length - 1; index > 0; index--)
            {
                var currentMass = histogram[index].Count / totalCount;
                if (massDiscarded + currentMass < fractionOfValuesToDiscard)
                {
                    massDiscarded += currentMass;
                    elementsToRetain--;
                }
            }
            var elementsDiscarded = histogram.Length - elementsToRetain;
            var oldMax = histogram[histogram.Length - 1].MinimumInclusive;
            var newMax = histogram[elementsToRetain - 1].MinimumInclusive;
            Console.WriteLine($"[{nameof(TrimHighValues)}] Discarded a total of {elementsDiscarded} bins, holding {massDiscarded:0.00%} of the total probability mass. Maximum bin started at {oldMax}, is now at {newMax}.");
            return histogram.Take(elementsToRetain).ToArray();
        }

        /// <summary>
        /// Estimates window and level for MR images, by fitting an exponential curve for the low 
        /// values, discarding those, and computing mean and standard deviation from the rest.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="volumeSkip"></param>
        /// <returns></returns>
        public static (int Window, int Level) ExponentialFitForMR(short[] volume, uint volumeSkip = 5)
        {
            volume = volume ?? throw new ArgumentNullException(nameof(volume));
            var stopwatch = Stopwatch.StartNew();

            // Compute min/ max and threshold values.
            var minMax = FindMinMax(volume, volumeSkip);
            var histogram = TrimHighValues(CreateHistogram(volume, minMax, volumeSkip), 0.03);
            var histogramCounts = histogram.Select(bin => (double)bin.Count).ToArray();
            var xRange = histogram.Select(bin => (double)bin.MinimumInclusive).ToArray();
            // The original histogram tends to have a massive peak aroun 0 as there are lots of dark regions(air etc)
            // Finding the first peak by fitting an exponential function
            var bestTotalDiff = double.MaxValue;
            var bestExpFunc = new double[xRange.Length];
            var currentExpFunc = new double[xRange.Length];
            var maxHistogramCount = histogram.Max(bin => bin.Count);
            IEnumerable<double> Range(double min, double increment, double max)
            {
                var current = min;
                while (current <= max)
                {
                    yield return current;
                    current += increment;
                }
            }
            void ComputeExponential(double scale, double gamma, double beta)
            {
                var beta2 = beta * beta;
                for (var index = 0; index < currentExpFunc.Length; index++)
                {
                    currentExpFunc[index] = scale * Math.Exp(-Math.Pow(xRange[index], gamma) / beta2);
                }
            }
            // ignoring the first 2 bins in the error of fit computation as very noisy
            var ignoreDelta = 2;
            foreach (var scaleMultiplier in Range(0.9, 0.1, 1.1))
            {
                var scale = scaleMultiplier * maxHistogramCount;
                foreach (var beta in Range(1, 0.5, 30))
                {
                    foreach (var gamma in Range(1, 0.05, 1.8))
                    {
                        ComputeExponential(scale, gamma, beta);
                        var delta =
                            histogramCounts
                            .Zip(currentExpFunc, (count, exp) => Math.Abs(count - exp))
                            .Skip(ignoreDelta)
                            .Sum();
                        if (delta < bestTotalDiff)
                        {
                            bestTotalDiff = delta;
                            currentExpFunc.CopyTo(bestExpFunc);
                        }
                    }
                }
            }

            // Now using the fitted exponential to find the point that separates the first from the second peak
            var thresholdBin = -1;
            var thresholdExpFunc = maxHistogramCount * 0.01;
            for (var index = 0; index < bestExpFunc.Length; index++)
            {
                if (bestExpFunc[index] < thresholdExpFunc)
                {
                    thresholdBin = index;
                    break;
                }
            }

            if (thresholdBin < 0)
            {
                throw new InvalidOperationException("The fitted exponential never went below the threshold.");
            }

            Console.WriteLine($"[{nameof(ExponentialFitForMR)}] Removing histogram up to bin at {xRange[thresholdBin]:0.0}");

            // Modify histogram counts in place to eliminate first peak
            for (var index = 0; index <= thresholdBin; index++)
            {
                histogramCounts[index] = 0;
            }

            // Compute best Window / Level for image automatically
            // by fitting a Gaussian to the transformed normalized histogram
            var sumHist = histogramCounts.Sum();
            var probHist = histogramCounts.Select(count => count / sumHist).ToArray();
            var mu = 
                probHist
                .Zip(xRange, (prob, x) => prob * x)
                .Sum();
            var variance =
                probHist
                .Zip(xRange, (prob, x) =>
                {
                    var delta = x - mu;
                    return delta * delta * prob;
                })
                .Sum();
            var std = Math.Sqrt(variance);
            // Antonio says that it is a little better to use the mode rather than the mean to set the Level.
            // However, with trimming high values the mean appears to be more stable.
            //var (maxIndex, _) = histogramCounts.ArgMax();
            //var level0 = xRange[maxIndex];
            var level0 = mu;
            // inverse contrast, lower for higher contrast of the final image.
            var inverseContrast = 1.7;
            var window0 = 2 * inverseContrast * std;
            Console.WriteLine($"[{nameof(ExponentialFitForMR)}] Mean: {mu:0.00} Standard deviation: {std:0.00}");
            Console.WriteLine($"[{nameof(ExponentialFitForMR)}] Before bounds checking: Level: {level0:0.00} Window: {window0:0.00}");
            var effectiveMin = (int)Math.Max(minMax.Minimum, level0 - window0 / 2);
            var effectiveMax = (int)Math.Min(minMax.Maximum, level0 + window0 / 2);
            var window = effectiveMax - effectiveMin;
            var level = (effectiveMax + effectiveMin) / 2;
            stopwatch.Stop();
            Console.WriteLine($"[{nameof(ExponentialFitForMR)}] {stopwatch.ElapsedMilliseconds} milliseconds - Level: {level} Window: {window}");
            return (window, level);
        }

        /// <summary>
        /// Creates a histogram from the provided array. 
        /// Note: the minimum/ maximum values can be thresholded and this will take this into consideration.
        /// </summary>
        /// <param name="volume">The volume array.</param>
        /// <param name="minMax">The minimum or maximum values in the array or minimum/ maximum values that have been thresholded.</param>
        /// <param name="volumeSkip">
        /// The number of items to skip over when computing the histogram (this is an optimisation to make the compute faster). 
        /// If set to 0, the histogram will look at every voxel when computing.
        /// If set to 1, this histogram will look at every other voxel etc.
        /// </param>
        /// <returns>The dictionary histogram.</returns>
        public static HistogramBin[] CreateHistogram(
            short[] volume,
            MinMax<short> minMax,
            uint volumeSkip = 0,
            int histogramSize = DefaultHistogramSize)
        {
            if (histogramSize < 1)
            {
                throw new ArgumentException("The histogram must have at least 1 bin.", nameof(histogramSize));
            }

            var histogram = new HistogramBin[histogramSize];
            var range = (double)minMax.Range();
            var binSize = range / histogramSize;
            for (var binIndex = 0; binIndex < histogramSize; binIndex++)
            {
                var minimumInclusive = Math.Ceiling(minMax.Minimum + binIndex * binSize);
                histogram[binIndex] = new HistogramBin(binIndex, (short)minimumInclusive);
            }

            if (range <= 0)
            {
                return histogram;
            }

            for (uint i = 0; i < volume.Length; i += volumeSkip + 1)
            {
                var binPosition = (volume[i] - minMax.Minimum) / binSize;
                var binIndex =
                    binPosition < 0
                    ? 0
                    : binPosition >= histogramSize
                    ? histogramSize - 1
                    : (int)binPosition;
                histogram[binIndex].Count++;
            }

            return histogram;
        }

        /// <summary>
        /// Finds the minimum and maximum values from a short array, possibly skipping some values to speed
        /// up computation.
        /// </summary>
        /// <param name="array">The short array.</param>
        /// <param name="volumeSkip">
        /// The number of items to skip over when computing the minimum and maximum (this is an optimisation to make the computation faster). 
        /// If set to 0, the minimum and maximum will look at every voxel when computing.
        /// If set to 1, this minimum and maximum will look at every other voxel etc.
        /// </param>
        /// <returns>The tuple of min/ max shorts.</returns>
        public static MinMax<short> FindMinMax(short[] array, uint volumeSkip)
        {
            var min = short.MaxValue;
            var max = short.MinValue;
            var current = short.MaxValue;

            for (uint i = 0; i < array.Length; i += volumeSkip + 1)
            {
                current = array[i];

                if (current < min)
                {
                    min = current;
                }

                if (current > max)
                {
                    max = current;
                }
            }

            return MinMax.Create(min, max);
        }
    }
}