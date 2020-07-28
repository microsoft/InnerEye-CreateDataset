///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    using CommandLine;
    
    [Verb("analyze", HelpText = "Analyzes a converted dataset in NIFTI format by deriving statistics.")]
    public class CommandlineAnalyzeDataset : CommandlineShared
    {
        /// <summary>
        /// Gets or sets the un-processed commandline arguments that have been passed into the CreateDataset runner.
        /// </summary>
        public string[] RawCommandlineArguments { get; set; }

        /// <summary>
        /// The full path to the dataset folder to be analyzed.
        /// </summary>
        [Option('d', "datasetFolder", HelpText = "Location of the nifti dataset to be analyzed")]
        public string DatasetFolder { get; set; }

        [Option('s', "statisticsFolder", Default="statistics", HelpText = "Name of subfolder to receive statistics files (must not already exist)")]
        public string StatisticsFolder { get; set; }

        /// <summary>
        /// Include "external" (if present) in the pairwise comparisons.
        /// </summary>
        [Option('e', "includePairwiseExternal", Default = false, HelpText = "Whether to calculate pairwise statistics involving the \"external\" structure (time consuming!)")]
        public bool PairwiseExternal { get; set; }

        [Option('a', "subjectsToAnalyze", Default = "", HelpText = "Comma-separated list of subject IDs and ranges to analyze, e.g. 3,13,17-20")]
        public string SubjectsToAnalyze { get; set; }

        /// <summary>
        /// Creates a new command line option instance, with all properties set to their default values.
        /// </summary>
        public CommandlineAnalyzeDataset() { }
    }
}
