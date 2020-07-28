///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    using System;
    using MedLib.IO.Models;

    /// <summary>
    /// Stores information about a channel inside a subject's data.
    /// </summary>
    public class VolumeMetadata
    {
        /// <summary>
        /// Creates a new instance of <see cref="VolumeMetadata"/> with the given properties.
        /// </summary>
        /// <param name="seriesId"></param>
        /// <param name="subjectId"></param>
        /// <param name="channel"></param>
        public VolumeMetadata(
            string seriesId,
            int subjectId,
            string channel)
        {
            SeriesId = seriesId ?? subjectId.ToString();
            SubjectId = subjectId;

            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException(nameof(channel));
            }
            Channel = channel;
        }

        /// <summary>
        /// Gets or sets the series id associated with the subject. This ID is unique accross all datasets.
        /// </summary>
        public string SeriesId { get; }

        /// <summary>
        /// Gets or sets the numeric ID of the subject. That ID is specific to the dataset,
        /// and does not transfer across datasets that contain the same subject.
        /// </summary>
        public int SubjectId { get; }

        /// <summary>
        /// Gets or sets the channel that the volume belongs to.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Creates a clone of the present object, with the <see cref="Channel"/> property
        /// in the result set to the argument.
        /// </summary>
        /// <param name="updatedChannel"></param>
        /// <returns></returns>
        public VolumeMetadata UpdateChannel(string updatedChannel)
        {
            return new VolumeMetadata(SeriesId, SubjectId, updatedChannel);
        }

        /// <summary>
        /// Creates a clone of the present object, where the <see cref="SeriesId"/> property
        /// is getting an additional prefix (suffix is the <see cref="SeriesId"/> in the present object).
        /// </summary>
        /// <param name="seriesIdPrefix"></param>
        /// <returns></returns>
        public VolumeMetadata UpdateSeriesId(string seriesIdPrefix)
        {
            return new VolumeMetadata(
                seriesIdPrefix + SeriesId,
                SubjectId,
                Channel);
        }
    }

    /// <summary>
    /// Stores both a medical volume, and its associated metadata (subject, channel, etc.)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VolumeAndMetadata
    {
        /// <summary>
        /// Creates a new instance of the class with the given properties.
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="volume"></param>
        public VolumeAndMetadata(VolumeMetadata metadata, MedicalVolume volume)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            Volume = volume;
        }

        /// <summary>
        /// Gets the volume metadata.
        /// </summary>
        public VolumeMetadata Metadata { get; }

        /// <summary>
        /// Gets the volume information.
        /// </summary>
        public MedicalVolume Volume { get; }
    }
}