///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;

    using InnerEye.CreateDataset.Common;
    using InnerEye.CreateDataset.Volumes;
    using InnerEye.CreateDataset.Data;
    using InnerEye.CreateDataset.Common.Models;
    using System.IO;
    using System.Threading.Tasks;

    public class OutlierResults
    {

        public List<string> Detailed { get; }

        public List<string> ConditionCounts { get; }
        public List<string> PatientCounts { get; }

        public OutlierResults(List<string> detailed, List<string> conditionCounts, List<string> patientCounts)
        {
            Detailed = detailed;
            ConditionCounts = conditionCounts;
            PatientCounts = patientCounts;
        }
    }

    public static class DatasetAnalysisFromConvertedDataset
    {
        // Before calculating statistics, we ensure every value in the (CT) image is at least -1000,
        // the value for air. Sometimes we get a smaller value for locations outside the scan altogether.
        private const short MinimumHUValue = -1000;

        // (Base) names of files to which results will be output.
        // Each line in this file will be "subject,statistic,structure1,structure2,value".
        private const string StatisticsFileName = "statistics.csv";
        // Each line in this file will be "subject,statistic,structure1,structure2,value,HI|LO,numberOfQuartiles".
        private const string DetailedOutliersFileName = "detailed_outliers.csv";
        // Each line in this file will be "statistic,structure1,structure2,nLowOutliers,nHighOutliers,nTotalOutliers".
        private const string ConditionOutlierCountsFileName = "condition_outlier_counts.csv";
        // Each line in this file will be "subject,nOutliers".
        private const string PatientOutlierCountsFileName = "patient_outlier_counts.csv";

        /// <summary>
        /// Analyzes a converted NIFTI dataset (CT image and structures; MR if any is ignored).
        /// This method assumes there is a dataset in the with the name given in options.DatasetFolder
        /// </summary>
        /// <param name="options">The command line options that guide the dataset analysis.</param>
        /// <returns></returns>
        public static void AnalyzeDataset(CommandlineAnalyzeDataset options)
        {
            var rawData = new LocalFileSystem(options.DatasetFolder, true);
            var subfolder = LocalFileSystem.JoinPath(options.DatasetFolder, options.StatisticsFolder);
            Directory.CreateDirectory(subfolder);
            var filesystemForResults = new LocalFileSystem(subfolder, false);
            var fileIO = new VolumeIO(filesystemForResults, rawData);
            var dataset = DatasetReader.LoadDatasetFromCsvFile(fileIO);
            HashSet<int> subjectIdsToAnalyze = SubjectSetToAnalyze(options.SubjectsToAnalyze);
            int subjectArraySize = dataset.Select(pair => pair.Key).Max() + 1;
            var outputLineArray = new List<StatisticsCalculator.StatisticValue>[subjectArraySize];
            Parallel.For(0, subjectArraySize, subject =>
            {
                try
                {
                    if (subjectIdsToAnalyze == null || subjectIdsToAnalyze.Contains(subject))
                    {
                        var subjectFiles = dataset[subject];
                        outputLineArray[subject] = AnalyzeSingleSubject(subjectFiles, fileIO, options);
                    }
                } catch (KeyNotFoundException)
                {
                    // no action
                }
            });
            var outputLines = new List<string>();
            for (int subject = 0; subject < outputLineArray.Length; subject++)
            {
                var stats = outputLineArray[subject];
                if (stats != null)
                {
                    foreach (var stat in stats)
                    {
                        outputLines.Add(stat.CsvRow(subject));
                    }
                }
            }
            filesystemForResults.WriteAllLines(StatisticsFileName, outputLines);
            var outliers = CalculateOutliers(outputLineArray);
            filesystemForResults.WriteAllLines(DetailedOutliersFileName, outliers.Detailed);
            filesystemForResults.WriteAllLines(ConditionOutlierCountsFileName, outliers.ConditionCounts);
            filesystemForResults.WriteAllLines(PatientOutlierCountsFileName, outliers.PatientCounts);
            Console.WriteLine($"Statistics written to {filesystemForResults.RootDirectory}");
        }

        public static OutlierResults CalculateOutliers(List<StatisticsCalculator.StatisticValue>[] statistics)
        {
            var data = ConstructDataForOutliers(statistics);
            // Number of outliers for each patient
            var patientCounts = new Dictionary<int, int>();
            // List of condition-count lines, with sorting key equal to minus the count so the Sort() method does what we want.
            var conditionCounts = new List<Tuple<int, string>>();
            // List of detailed-outliers lines, with sorting key equal to minus the strength of the outlier so the Sort() method does what we want.
            var detailed = new List<Tuple<double, string>>();
            foreach (var key in data.Keys)
            {
                var outliers = GetOutliers(data[key]);
                var numberOfLowOutliers = outliers.Where(outlier => !outlier.IsHigh).Count();
                conditionCounts.Add(Tuple.Create(-outliers.Count, $"{key},{numberOfLowOutliers},{outliers.Count - numberOfLowOutliers},{outliers.Count}"));
                foreach (var outlier in outliers)
                {
                    IncrementCount(patientCounts, outlier.Subject);
                    var quartiles = outlier.Quartiles;
                    var hilo = outlier.IsHigh ? "HI" : "LO";
                    detailed.Add(Tuple.Create(-Math.Abs(quartiles), $"{outlier.Subject},{key},{outlier.Value},{hilo},{quartiles}"));
                }
            }
            detailed.Sort();
            conditionCounts.Sort();
            var patientCountPairs = patientCounts.Select(kvp => Tuple.Create(-kvp.Value, kvp.Key)).ToList();
            patientCountPairs.Sort();
            return new OutlierResults(
                detailed.Select(pair => pair.Item2).ToList(),
                conditionCounts.Select(pair => pair.Item2).ToList(),
                patientCountPairs.Select(pair => $"{pair.Item2},{-pair.Item1}").ToList());
        }

        private static void IncrementCount(Dictionary<int, int> counts, int key)
        {
            if (!counts.ContainsKey(key))
            {
                counts[key] = 1;
            }
            else
            {
                counts[key] += 1;
            }
        }

        /// <summary>
        /// A helper class representing an outlier in some sample.
        /// </summary>
        class Outlier
        {
            public double Value; // the value of the outlier
            public int Subject; // the subject the outlier is for
            public double Quartiles; // the distance of Value from the median divided by the distance from the corresponding quartile to the median
            public bool IsHigh; // whether this is a high or low outlier

            public Outlier(double value, int subject, double quartiles, bool isHigh)
            {
                Value = value;
                Subject = subject;
                Quartiles = quartiles;
                IsHigh = isHigh;
            }
        }

        /// <summary>
        /// Returns a tuple of low outliers and high outliers for a data set.
        /// </summary>
        /// <param name="pairs">A list of (value, subject) pairs</param>
        /// <param name="multiplier">The factor by which the threshold for an outlier should exceed the difference between the median and the relevant quartile</param>
        private static List<Outlier> GetOutliers(List<ValueAndSubject> pairs, double multiplier = 4.0)
        {
            // Sort pairs by value.
            pairs.Sort();
            var maxIndex = pairs.Count - 1;
            // Find the median and the low and high quartiles.
            var median = InterpolatedValue(pairs, maxIndex * 0.5);
            var quartile1 = InterpolatedValue(pairs, maxIndex * 0.25);
            var quartile3 = InterpolatedValue(pairs, maxIndex * 0.75);
            // lowMark is the threshold below which a value is to be considered a low outlier
            var lowStep = median - quartile1;
            var lowMark = median - multiplier * lowStep;
            // highMark is the threshold above which a value is to be considered a high outlier
            double highStep = quartile3 - median;
            var highMark = median + multiplier * highStep;
            var outliers = new List<Outlier>();
            foreach (var pair in pairs)
            {
                if (pair.Value < lowMark && lowStep > 0)
                {
                    outliers.Add(new Outlier(pair.Value, pair.Subject, (median - pair.Value) / lowStep, isHigh: false));
                } else if (pair.Value > highMark && highStep > 0)
                {
                    outliers.Add(new Outlier(pair.Value, pair.Subject, (pair.Value - median) / highStep, isHigh: true));
                }
            }
            return outliers;
        }

        private static double InterpolatedValue(List<ValueAndSubject> pairs, double doubleIndex)
        {
            var intIndex = (int)doubleIndex;
            var remainder = doubleIndex - intIndex;
            if (remainder == 0.0)
            {
                return pairs[intIndex].Value;
            }
            return pairs[intIndex].Value * (1 - remainder) + pairs[intIndex + 1].Value * remainder;
        }

        class ValueAndSubject : IComparable
        {
            public double Value;
            public int Subject;
            public ValueAndSubject(double value, int subject)
            {
                Value = value;
                Subject = subject;
            }

            public int CompareTo(object obj)
            {
                var vas = (ValueAndSubject)obj;
                if (vas == null)
                {
                    return -1;
                }
                return Value.CompareTo(vas.Value);
            }
        }

        private static Dictionary<string, List<ValueAndSubject>> ConstructDataForOutliers(List<StatisticsCalculator.StatisticValue>[] statistics)
        {
            var data = new Dictionary<string, List<ValueAndSubject>>();
            for (int subject = 0; subject < statistics.Length; subject++) {
                var statsForSubject = statistics[subject];
                if (statsForSubject != null)
                {
                    foreach (var stat in statsForSubject)
                    {
                        var value = stat.Value;
                        var key = $"{stat.Statistic},{stat.Structure1},{stat.Structure2}";
                        if (!data.ContainsKey(key))
                        {
                            data[key] = new List<ValueAndSubject>();
                        }
                        data[key].Add(new ValueAndSubject(value, subject));
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Given a string consisting of a comma-separated list of non-negative integers and
        /// hyphen-separated ranges of non-negative integers, return a set of the all the integer
        /// represented by the string. For example, "1,3,4-6" gives a set consisting of 1,3,4,5,6.
        /// However, if the string is empty, return null.
        /// </summary>
        private static HashSet<int> SubjectSetToAnalyze(string subjectsToAnalyze)
        {
            if (subjectsToAnalyze == "")
            {
                return null;
            }
            var result = new HashSet<int>();
            foreach (var term in subjectsToAnalyze.Split(new char[] { ',' }))
            {
                if (term.Contains("-"))
                {
                    var range = term.Split(new char[] { '-' });
                    var rangeMin = int.Parse(range[0]);
                    var rangeMax = int.Parse(range[1]);
                    for (var subj = rangeMin; subj <= rangeMax; subj++)
                    {
                        result.Add(subj);
                    }
                }
                else
                {
                    result.Add(int.Parse(term));
                }
            }
            return result;
        }

        /// <summary>
        /// Performs all dataset analysis options on the data for a single subject.
        /// </summary>
        /// <param name="itemsPerSubject">All volumes and their associated structures for the subject.</param>
        /// <param name="options">The commandline options that guide dataset analysis.</param>
        /// <returns></returns>
        public static List<StatisticsCalculator.StatisticValue> AnalyzeSingleSubject(SubjectFiles subjectFiles, VolumeIO fileIO, CommandlineAnalyzeDataset options)
        {
            Volume3D<short> imageData = null;
            var names = new List<string>();
            var binaries = new List<Volume3D<byte>>();
            foreach (var channel in subjectFiles.GetRawChannels())
            {
                DatasetFile channelFile = subjectFiles[channel];
                if (channel == "image" || channel == "ct")
                {
                    imageData = fileIO.LoadVolumeInt16(channelFile);
                    // Replace any values that are less than -1000 (which are probably outside the
                    // scanned volume) with -1000, the HU value for air.
                    for (int i = 0; i < imageData.DimXY * imageData.DimZ; i++)
                    {
                        imageData[i] = Math.Max(imageData[i], MinimumHUValue);
                    }
                }
                else if (channel != "labels")
                {
                    try
                    {
                        var volume = fileIO.LoadVolumeByte(channelFile);
                        names.Add(channel);
                        binaries.Add(volume);
                    }
                    catch (InvalidDataException)
                    {
                        // Ignore any non-label, non-CT volumes, as they are likely to be other images.
                    }
                }
            }
            return AnalyzeSubjectVolumes(imageData, names, binaries, options);
        }

        /// <summary>
        /// Calculates and returns the lines (destined for statistics.csv) for a given subject.
        /// </summary>
        /// <param name="imageData">The CT image</param>
        /// <param name="names">Names of structures, each for the corresponding member of "binaries"</param>
        /// <param name="binaries">Masks for the given structure and subject</param>
        /// <param name="options">command line options</param>
        /// <returns>A list of csv lines, each of the form "subject,statisticName,structure1,structure2,value"</returns>
        public static List<StatisticsCalculator.StatisticValue> AnalyzeSubjectVolumes(
            Volume3D<short> imageData, List<string> names, List<Volume3D<byte>> binaries, CommandlineAnalyzeDataset options)
        {
            var outputLines = new List<StatisticsCalculator.StatisticValue>();
            var overlapCount = new int[3, binaries.Count, binaries.Count];
            var missingLayerCount = new int[3, binaries.Count];
            var topBottomCount = new int[3, binaries.Count];
            var flatness = new double[3, binaries.Count, 2];
            // First non-null binary
            Volume3D<byte> first = binaries.FirstOrDefault(s => s != null);
            if (first != null)
            {
                // copy in from MultiLabelCreator and just keep stats part
                var mm3 = first.SpacingX * first.SpacingY * first.SpacingZ;
                var dimensions = new[] { first.DimX, first.DimY, first.DimZ };
                for (var dimIndex = 0; dimIndex < dimensions.Length; dimIndex++)
                {
                    StatisticsCalculator.CalculateLayerStatistics(dimIndex, dimensions[dimIndex], binaries, first, overlapCount, missingLayerCount, topBottomCount, flatness);
                }
                outputLines.AddRange(StatisticsCalculator.BuildSizeStatistics(
                    names, overlapCount, missingLayerCount, topBottomCount, flatness, mm3));
                // Calculate and record lots of other statistics about the structures.
                // We hard-code to calculate only an approximate boundary ROC, as the difference is negligible and
                // the speedup is considerable.
                outputLines.AddRange(StatisticsCalculator.CalculateStatisticValues(
                    binaries, imageData, names, exactBoundaryRoc: false, pairwiseExternal: options.PairwiseExternal));
            }
            return outputLines;
        }
    }
}