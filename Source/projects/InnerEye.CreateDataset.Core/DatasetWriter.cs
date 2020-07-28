///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿
namespace InnerEye.CreateDataset.Core
{
    using System.Collections.Concurrent;
    using InnerEye.CreateDataset.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MoreLinq;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;
    using MedLib.IO;
    using System.IO;


    /// <summary>
    /// Handles the writing a dataset to a folder.
    /// </summary>
    public class DatasetWriter
    {
        private LocalFileSystem _datasetRoot;
        private NiftiCompression _niftiCompression;
        private ConcurrentBag<VolumeWriteInfo> _writtenVolumes = new ConcurrentBag<VolumeWriteInfo>();

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="datasetRoot">The folder to which the dataset should be written.</param>
        /// <param name="niftiCompression">The Nifti compression level that should be used to write 
        /// all volumes.</param>
        public DatasetWriter(LocalFileSystem datasetRoot, NiftiCompression niftiCompression)
        {
            _datasetRoot = datasetRoot;
            _niftiCompression = niftiCompression;
        }

        /// <summary>
        /// Writes text to a file.
        /// </summary>
        /// <param name="fileName">The file name to to write to within the dataset folder</param>
        /// <param name="text">The text to write.</param>
        public void WriteText(string fileName, string text)
        {
            _datasetRoot.WriteAllText(fileName, text);
        }

        /// <summary>
        /// Writes a full dataset to the dataset folder.
        /// datasets. Returns a human readable string with diagnostic information.
        /// </summary>
        /// <param name="dataset">The individual items of the dataset, one item per subject.</param>
        public string WriteDatasetToFolder(IEnumerable<IReadOnlyList<VolumeAndStructures>> dataset,
            Func<IReadOnlyList<VolumeAndStructures>, IEnumerable<VolumeAndStructures>> converter)
        {
            var foundStructures = new ConcurrentDictionary<string, int>();
            var _counterLock = new object();
            var subjectCount = 0;
            Parallel.ForEach(
                dataset,
                new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                itemsPerSubject =>
                {
                    lock (_counterLock)
                    {
                        subjectCount++;
                    }
                    try
                    {
                        converter(itemsPerSubject)
                        .ForEach(channel =>
                        {
                            WriteVolumeAndStructuresToFolder(channel);
                            foreach (var structure in channel.Structures)
                            {
                                foundStructures.AddOrUpdate(structure.Key, 1, (_, y) => y + 1);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        var subject = itemsPerSubject.First().Metadata.SubjectId;
                        var series = itemsPerSubject.First().Metadata.SeriesId;
                        throw new Exception($"Subject {subject} (series {series}) failed: {ex.Message}", ex);
                    }
                });

            // Return a string with diagnostic information
            var text = new StringBuilder();
            foreach (var item in foundStructures)
            {
                text.AppendLine($"Structure '{item.Key}' was present in {item.Value} out of {subjectCount} (subject, channel) pairs.");
            }
            return text.ToString();
        }

        /// <summary>
        /// Writes a single dataset item (scan and structures) to the dataset folder.
        /// </summary>
        /// <param name="volumeAndStructures">The dataset item to write.</param>
        public void WriteVolumeAndStructuresToFolder(VolumeAndStructures volumeAndStructures)
        {
            // write the image volume 
            WriteVolume(volumeAndStructures.Volume, volumeAndStructures.Metadata);

            // for each of the ground truth structures associated with the image volume 
            // clone the volume metadata and update the associated channel name with the GT structure name as defined in the metadata before writing
            volumeAndStructures.Structures.ForEach(x =>
            {
                var meta = volumeAndStructures.Metadata.UpdateChannel(x.Key);
                WriteVolume(x.Value, meta);
            });
        }

        /// <summary>
        /// Writes an instance of <see cref="Volume3D"/> to the dataset folder. The volume will
        /// be written in Nifti format, with the compression level set by the dataset writer. The file name
        /// will be automatically created based on the volume metadata. Returns the information about 
        /// where and how the file was written.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume">The volume to write.</param>
        /// <param name="volumeMetadata">The information about subject and channel to which the volume belongs.</param>
        /// <returns></returns>
        public VolumeWriteInfo WriteVolume(Volume3D<short> volume, VolumeMetadata volumeMetadata)
        {
            var bytes = volume.SerializeToNiftiBytes(_niftiCompression);
            return WriteVolumeAsBytes(bytes, volumeMetadata);
        }

        /// <summary>
        /// Writes an instance of <see cref="Volume3D"/> to the dataset folder. The volume will
        /// be written in Nifti format, with the compression level set by the dataset writer. The file name
        /// will be automatically created based on the volume metadata. Returns the information about 
        /// where and how the file was written.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume">The volume to write.</param>
        /// <param name="volumeMetadata">The information about subject and channel to which the volume belongs.</param>
        /// <param name="isEmptyVolume">If true, the volume is actually empty, and was created because no the
        /// requested ground truth channel was not available for this subject.</param>
        /// <returns></returns>
        public VolumeWriteInfo WriteVolume(Volume3D<byte> volume, VolumeMetadata volumeMetadata)
        {
            var bytes = volume.SerializeToNiftiBytes(_niftiCompression);
            return WriteVolumeAsBytes(bytes, volumeMetadata);
        }

        /// <summary>
        /// Writes a <see cref="Volume3D{T}"/> instance to the dataset folder, when the volume
        /// has already be converted to a byte array in Nifti format. The file name
        /// will be automatically created based on the volume metadata. Returns the information about 
        /// where and how the file was written.
        /// </summary>
        /// <param name="volume">The volume to write.</param>
        /// <param name="volumeMetadata">The information about subject and channel to which the volume belongs.</param>
        /// <returns></returns>
        private VolumeWriteInfo WriteVolumeAsBytes(byte[] bytes, VolumeMetadata volumeMetadata)
        {
            var fileName = VolumeWriteInfo.CreateFileName(volumeMetadata, _niftiCompression);
            var info = WriteBytes(fileName, bytes, volumeMetadata);
            _writtenVolumes.Add(info);
            return info;
        }

        /// <summary>
        /// Writes a medical volume to the dataset folder.
        /// </summary>
        /// <param name="fileName">The file name to use. The folder prefix for the dataset will 
        /// be added automatically.</param>
        /// <param name="dataToUpload">The bytes to write.</param>
        /// <param name="volumeMetadata">The information about subject and channel to which the volume belongs.</param>
        /// <returns></returns>
        private VolumeWriteInfo WriteBytes(string fileName,
            byte[] dataToUpload,
            VolumeMetadata volumeMetadata)
        {
            WriteBytes(fileName, dataToUpload);
            return new VolumeWriteInfo(volumeMetadata, fileName);
        }

        /// <summary>
        /// Writes a byte array to a file.
        /// </summary>
        /// <param name="fileName">The file name to use. The folder prefix for the dataset will 
        /// be added automatically.</param>
        /// <param name="dataToUpload">The bytes to write.</param>
        private void WriteBytes(string fileName, byte[] dataToUpload)
        {
            var filePath = StreamsFromFileSystem.JoinPath(_datasetRoot.RootDirectory, fileName);
            LocalFileSystem.CreateDirectoryIfNeeded(filePath);
            File.WriteAllBytes(filePath, dataToUpload);
        }

        /// <summary>
        /// Gets the information about all instances of <see cref="Volume3D{T}"/> that were written
        /// in the present object since its creation via <see cref="WriteVolumeAsBytes(byte[], VolumeMetadata)"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<VolumeWriteInfo> WrittenVolumes() => _writtenVolumes.ToList();
    }
}