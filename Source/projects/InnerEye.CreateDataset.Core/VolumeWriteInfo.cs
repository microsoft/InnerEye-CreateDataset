///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    using MedLib.IO;
    using InnerEye.CreateDataset.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Holds the information about how and where a volume was written, when creating
    /// a dataset.
    /// </summary>
    public class VolumeWriteInfo
    {
        public VolumeWriteInfo(VolumeMetadata metadata, string pathRelativeToDatasetFolder)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            if (string.IsNullOrWhiteSpace(pathRelativeToDatasetFolder))
            {
                throw new ArgumentException(nameof(pathRelativeToDatasetFolder));
            }
            UploadPathRelativeToDatasetFolder = pathRelativeToDatasetFolder;
        }

        /// <summary>
        /// Gets or sets the information about the subject, channel, and other metadata.
        /// </summary>
        public VolumeMetadata Metadata { get; }

        /// <summary>
        /// Stores the file path where the volume was written to. The path should not contain the
        /// root folder for the dataset (i.e., if the file was written to /dataset_1234/9/ct.nii.gz,
        /// this property should contain 9/ct.nii.gz)
        /// </summary>
        public string UploadPathRelativeToDatasetFolder { get; }

        /// <summary>
        /// Creates a file name under which the given volume should be written to disk. The file name
        /// will depend on the Nifti compression level.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="compression"></param>
        /// <returns></returns>
        public static string CreateFileName(VolumeMetadata volume, NiftiCompression compression)
        {
            var extension = MedIO.GetNiftiExtension(compression);
            var file = volume.Channel + extension;
            return VolumeIO.JoinPath(volume.SubjectId.ToString(), file);
        }

        /// <summary>
        /// Converts the present object to the format required by the dataset reader code in
        /// <see cref="DatasetReader"/>
        /// </summary>
        /// <returns></returns>
        public string ToDatasetCsvLine()
        {
            return $"{Metadata.SubjectId}," +
                $"{UploadPathRelativeToDatasetFolder}," +
                $"{Metadata.Channel}," +
                $"{Metadata.SeriesId}";
        }

        /// <summary>
        /// Consumes a list of information about individual volumes that had been written to disk,
        /// and turns them into the format used by the dataset reader.
        /// Returns a multi line string with a column header and one line for each volume.
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public static string BuildDatasetCsvFile(IEnumerable<VolumeWriteInfo> files)
        {
            // The column header is discarded, its contents does not matter.
            var headerLine = "subject,filePath,channel,seriesId";
            var text = new StringBuilder();
            text.AppendLine(headerLine);
            foreach (var file in files)
            {
                text.AppendLine(file.ToDatasetCsvLine());
            }
            return text.ToString();
        }
    }
}
