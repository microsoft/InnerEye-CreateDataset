///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using InnerEye.CreateDataset.Data;

    public static class DatasetReader
    {
        /// <summary>
        /// The name of a dataset file.
        /// </summary>
        public const string DatasetCsvFile = "dataset.csv";

        /// <summary>
        /// Delimiter used to separate tags associated with a series.
        /// </summary>
        public const char SeriesTagsDelimiter = ';';

        /// <summary>
        /// Creates a dataset structure from a dataset file read from disk.
        /// </summary>
        /// <param name="fileIO">The IO abstraction that handles file reading.</param>
        /// <returns></returns>
        /// <param name="runDatasetConsistencyCheck">If true, check whether the loaded dataset is consistent. Set to
        /// false if you want to reduce the number of IO operations carried out.</param>
        public static Dataset LoadDatasetFromCsvFile(VolumeIO fileIO, bool runDatasetConsistencyCheck = true)
        {
            if (!fileIO.Raw.FileExists(DatasetCsvFile))
            {
                throw new FileNotFoundException("The dataset file must exist in the file system provided.");
            }
            var files = ParseDatasetCsv(fileIO.ReadAllTextFromRaw(DatasetCsvFile));
            return LoadDataset(fileIO, files, runDatasetConsistencyCheck);
        }

        /// <summary>
        /// Parses the full contents of a dataset.csv file, and returns the files that make up the dataset.
        /// </summary>
        /// <param name="datasetCsvLines"></param>
        /// <returns></returns>
        public static IEnumerable<DatasetFile> ParseDatasetCsv(string datasetCsvLines)
        {
            var rows =
                Dataset.TextToLines(datasetCsvLines)
                .Skip(1) // Skip the header. We expect header always
                .ToList();
            Console.WriteLine($"The dataset contains a total of {rows.Count} data lines.");
            var files = new List<DatasetFile>();
            foreach (var row in rows.Where(row => !String.IsNullOrWhiteSpace(row)))
            {
                files.Add(ParseDatasetRow(row));
            }
            return files;
        }

        /// <summary>
        /// Parses a single row of a dataset.csv file into an instance of <see cref="DatasetFile"/>.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static DatasetFile ParseDatasetRow(string row)
        {
            var columns = row.Split(',');
            if (columns.Length < 3)
            {
                throw new FormatException($"Invalid csv file: found a row with {columns.Length} columns must be at least of length 3 [imageid,path,channelid,...,...]");
            }
            // required
            int imageId = Convert.ToInt32(columns[0].Trim());
            string fullPath = columns[1].TrimStart('\\', '/').Trim();
            string channelId = columns[2].Trim();

            // optional
            string seriesId = columns.Length > 3 ? columns[3].Trim() : null;
            string institutionId = columns.Length > 4 ? columns[4].Trim() : null;
            string imageFilePath = columns.Length > 5 ? columns[5].Trim() : null;
            string groundTruthFilePath = columns.Length > 6 ? columns[6].Trim() : null;
            string[] tags = null;
            if (columns.Length > 7) {
                var tagsString = string.Join(",", columns.Skip(7)).Trim(new[] { '"' });
                tags = tagsString.Replace("[", "").Replace("]", "").Trim().Split(SeriesTagsDelimiter);
            }

            return DatasetFile.CreateRaw(imageId, fullPath, channelId, seriesId, institutionId, imageFilePath, groundTruthFilePath, tags);
        }

        /// <summary>
        /// Creates a dataset structure from a list of rows that were read from a dataset.csv file. Each row is expected to
        /// contain 3 comma-separated files.
        /// </summary>
        /// <param name="fileIO">The IO abstraction that handles file reading.</param>
        /// <param name="filePath">The path of the dataset file, expected to be inside of the raw image repository in the IO abstraction.</param>
        /// <returns></returns>
        /// <param name="runDatasetConsistencyCheck">If true, check whether the loaded dataset is consistent. Set to
        /// false if you want to reduce the number of IO operations carried out.</param>
        public static Dataset LoadDataset(VolumeIO fileIO, IEnumerable<DatasetFile> datasetRows, bool runDatasetConsistencyCheck)
        {
            var missingFiles = new List<string>();
            if (runDatasetConsistencyCheck)
            {
                foreach (var file in datasetRows)
                {
                    if (!fileIO.IsRawFileAvailable(file.FilePath))
                    {
                        missingFiles.Add(file.FilePath);
                    }
                }
                if (missingFiles.Count > 0)
                {
                    Trace.TraceWarning($"The following {missingFiles.Count} files are referenced in the dataset, but are not available on disk:");
                    foreach (var file in missingFiles)
                    {
                        Trace.TraceWarning($"Missing file: {file}");
                    }
                    throw new FileNotFoundException("The dataset contains files that are not available on disk. Please inspect the logs for details.");
                }
            }
            var dataset = Dataset.Create(datasetRows);
            Console.WriteLine($"Finished loading data for {dataset.Count} subjects.");
            return dataset;
        }
    }
}