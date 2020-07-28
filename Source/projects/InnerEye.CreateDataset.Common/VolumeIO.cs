///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Common
{
    using System;
    using System.IO;

    using InnerEye.CreateDataset.Data;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// A base class that contains all core IO operations that dataset analysis run needs to perform.
    /// </summary>
    public class VolumeIO
    {
        /// <summary>
        /// Gets the object that is used the access the raw dataset.
        /// </summary>
        public StreamsFromFileSystem Raw { get; }

        /// <summary>
        /// Gets the object that is used to access the intermediate files that a run creates.
        /// </summary>
        public StreamsFromFileSystem Intermediate { get; }

        /// <summary>
        /// Creates a new instance of the class, taking 2 objects that access the raw subject data and the
        /// intermediate data, respectively.
        /// </summary>
        /// <param name="intermediate">The object that is used to access the run intermediate files.</param>
        /// <param name="raw">The object used to access the raw datasets.</param>
        public VolumeIO(StreamsFromFileSystem intermediate, StreamsFromFileSystem raw)
        {
            Raw = raw;
            Intermediate = intermediate;
        }

        /// <summary>
        /// Gets whether an input file of the given name exists in the storage system.
        /// This should be used to check for existence of the original CT or ground truth labelling files.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool IsRawFileAvailable(string fileName)
            => Raw.FileExists(fileName);

        /// <summary>
        /// Loads an image from the file storage system, where the image is expected to 
        /// contain byte voxel values.
        /// An <see cref="InvalidDataException"/> is thrown if that is not the case.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private Volume3D<byte> LoadVolumeByte(string fileName)
            => Intermediate.LoadImageInByteFormat(fileName);

        /// <summary>
        /// Loads an image from the file storage system, where the image is expected to 
        /// contain short (16bit signed integer) voxel values.
        /// An <see cref="InvalidDataException"/> is thrown if that is not the case.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private Volume3D<short> LoadVolumeInt16(string fileName)
            => Intermediate.LoadImageInShortFormat(fileName);

        /// <summary>
        /// Loads a raw medical image from a permanent storage system.
        /// This is meant to be used for the unprocessed ground truth labelling.
        /// The image is expected to contain byte voxel values.
        /// An <see cref="InvalidDataException"/> is thrown if that is not the case.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataType"></param>
        private Volume3D<byte> LoadRawVolumeByte(string fileName)
            => Raw.LoadImageInByteFormat(fileName);

        /// <summary>
        /// Loads a raw medical image from a permanent storage system.
        /// This is meant to be used for the unprocessed medical scan.
        /// The image is expected to contain short (signed 16bit integer) voxel values.
        /// An <see cref="InvalidDataException"/> is thrown if that is not the case.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataType"></param>
        private Volume3D<short> LoadRawVolumeInt16(string fileName)
            => Raw.LoadImageInShortFormat(fileName);

        /// <summary>
        /// The string that separates directories in a path.
        /// </summary>
        public const char DirectorySeparator = StreamsFromFileSystem.DirectorySeparator;

        /// <summary>
        /// Joins two path strings via the directory separator.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static string JoinPath(string path1, string path2)
        {
            return StreamsFromFileSystem.JoinPath(path1, path2);
        }

        /// <summary>
        /// Reads all text that is available in the file of given name, when the file is
        /// residing in the repository of raw subject data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string ReadAllTextFromRaw(string fileName)
            => Raw.ReadAllText(fileName);

        public Volume3D<T> LoadVolume<T>(DatasetFile file, Func<string, Volume3D<T>> raw, Func<string, Volume3D<T>> intermediate)
        {
            try
            {
                return raw(file.FilePath);
            }
            catch (InvalidDataException ex)
            {
                var message = $"Error loading volume from {file.FilePath}: {ex.Message}";
                throw new InvalidDataException(message, ex);
            }
        }

        public Volume3D<byte> LoadVolumeByte(DatasetFile file)
        {
            return LoadVolume(file, LoadRawVolumeByte, LoadVolumeByte);
        }

        public Volume3D<short> LoadVolumeInt16(DatasetFile file)
        {
            return LoadVolume(file, LoadRawVolumeInt16, LoadVolumeInt16);
        }
    }
}
