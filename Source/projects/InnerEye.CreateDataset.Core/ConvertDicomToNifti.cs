///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using MedLib.IO;
    using InnerEye.CreateDataset.Common;
    using InnerEye.CreateDataset.Volumes;
    using itk.simple;
    using InnerEye.CreateDataset.Data;
    using InnerEye.CreateDataset.Math;
    using MoreLinq;

    public static class ConvertDicomToNifti
    {
        /// <summary>
        /// The name of a file that is written during dataset creation, and contains info about how dataset
        /// creation was done.
        /// </summary>
        public const string DatasetCreationStatusFile = "info.txt";

        /// <summary>
        /// Converts a dataset from DICOM to Nifti.
        /// Each channel in the dataset will be turned into Int16 Nifti files, each segmented
        /// structure will be turned into a binary mask stored as a Byte Nifti file.
        /// After this method finishes, the converted dataset will be in folder <paramref name="dataRoot"/>.
        /// </summary>
        /// <param name="dataRoot">The folder whichh contains the DICOM dataset.</param>
        /// <param name="options">The commandline options that guide the dataset creation.</param>
        /// <returns></returns>
        public static void CreateDataset(LocalFileSystem dataRoot, CommandlineCreateDataset options)
        {

            var datasetPath = StreamsFromFileSystem.JoinPath(dataRoot.RootDirectory, options.NiftiDirectory);
            Directory.CreateDirectory(datasetPath);
            var datasetRoot = new LocalFileSystem(datasetPath, false);

            // TODO: Get metadata on DICOM dataset
            //PrintDatasetMetadata(metadata);
            //if (metadata.DatasetSize == 0)
            //{
            //    throw new InvalidDataException("The dataset is empty.");
            //}

            var dataLoader = new DatasetLoader(StreamsFromFileSystem.JoinPath(dataRoot.RootDirectory, options.DicomDirectory));
            var datasetAsVolumeAndMetadata = dataLoader.LoadAllDicomSeries();
            var datasetAsVolumes = datasetAsVolumeAndMetadata.SelectMany(itemsPerSubject =>
                {
                    if (AreDatasetItemsValid(itemsPerSubject, options.DiscardInvalidSubjects))
                    {
                        return
                            new List<List<VolumeAndStructures>>() { itemsPerSubject
                            .Select(item => VolumeAndStructures.FromMedicalVolume(item, isLowerCaseConversionEnabled: true, dropRepeats: true))
                            .ToList() };
                    }
                    return new List<List<VolumeAndStructures>>();
                });

            var writer = new DatasetWriter(datasetRoot, NiftiCompression.GZip);
            var message =
                writer.WriteDatasetToFolder(datasetAsVolumes,
                    itemsPerSubject => ConvertSingleSubject(itemsPerSubject, options));
            var datasetCsvString = VolumeWriteInfo.BuildDatasetCsvFile(writer.WrittenVolumes());
            writer.WriteText(DatasetReader.DatasetCsvFile, datasetCsvString);
            var status = new StringBuilder(options.SettingsOverview());
            status.AppendLine("Per-subject status information:");
            status.AppendLine(message);
            writer.WriteText(DatasetCreationStatusFile, status.ToString());
        }

        /// <summary>
        /// Performs all dataset creation options on the data for a single subject:
        /// * Registration on a reference volume
        /// * Renaming structures
        /// * Making structures mutually exclusive
        /// * Geometric normalization
        /// * Compute derived structures
        /// </summary>
        /// <param name="itemsPerSubject">All volumes and their associated structures for the subject.</param>
        /// <param name="options">The commandline options that guide dataset creation.</param>
        /// <returns></returns>
        public static IEnumerable<VolumeAndStructures> ConvertSingleSubject(IReadOnlyList<VolumeAndStructures> itemsPerSubject,
            CommandlineCreateDataset options)
        {
            var volumes = RegisterSubjectVolumes(itemsPerSubject, options.RegisterVolumesOnReferenceChannel).ToList();
            if (volumes.Count == 0)
            {
                // We silently drop a subject with no volumes at all, even if options.RequireAllGroundTruthStructures is set.
                return volumes;
            }
            // By this point we should have at most one volume with structures attached. If we don't find one, we take the
            // first volume as the one that will eventually receive structures.
            var mainVolume = volumes.FirstOrDefault(volume => volume.Structures.Count > 0) ?? volumes[0];
            var subjectId = mainVolume.Metadata.SubjectId;
            var renamingOK = mainVolume.Rename(options.NameMappings, allowNameClashes: options.AllowNameClashes,
                throwIfInvalid: !options.DiscardInvalidSubjects);
            if (!renamingOK)
            {
                // then discard all the volumes
                return new List<VolumeAndStructures>();
            }
            mainVolume.AddEmptyStructures(options.CreateIfMissing);
            bool structuresAreGood = true;
            if (options.GroundTruthDescendingPriority != null && options.GroundTruthDescendingPriority.Any())
            {
                var namesInPriorityOrder = options.GroundTruthDescendingPriority.ToArray();
                var namesToRemove = MakeStructuresMutuallyExclusiveInPlace(mainVolume.Structures, namesInPriorityOrder, subjectId);
                foreach (var name in namesToRemove)
                {
                    Trace.TraceInformation($"Subject {subjectId}: removing structure named {name}");
                    mainVolume.Remove(name);
                }
                if (options.RequireAllGroundTruthStructures)
                {
                    var namesToFind = new HashSet<string>(namesInPriorityOrder.Select(name => name.TrimStart(new[] { '+' })));
                    var namesPresent = mainVolume.Structures.Select(structure => structure.Key).ToHashSet();
                    var namesMissing = namesToFind.Except(namesPresent).ToList();
                    if (!namesMissing.IsNullOrEmpty())
                    {
                        var message = $"Subject {subjectId}: Error: no structure(s) named " + string.Join(", ", namesMissing);
                        if (options.DiscardInvalidSubjects)
                        {
                            Trace.TraceInformation(message);
                            structuresAreGood = false;
                        }
                        else
                        {
                            throw new InvalidOperationException(message);
                        }
                    }
                }
            }
            if (!structuresAreGood)
            {
                // then discard all the volumes
                return new List<VolumeAndStructures>();
            }
            var spacing = options.GeometricNormalizationSpacingMillimeters?.ToArray();
            volumes = volumes.Select(volume => volume.GeometricNormalization(spacing)).ToList();
            if (options.DerivedStructures != null)
            {
                options.DerivedStructures.ForEach(derived => AddDerivedStructures(volumes, derived));
            }
            Trace.TraceInformation($"Subject {subjectId}: has all required structures");
            return volumes;
        }

        /// <summary>
        /// Performs consistency checks on the structures that are available for a subject. 
        /// In particular, structure names must be unique after conversion to lower case, across
        /// all channels for the subject. Returns true if no problems are found. If any
        /// problems are found, either returns false (if discardInvalidSubjects is true) or throws an
        /// <see cref="InvalidOperationException"/>. Details of the errors are output to Trace.
        /// </summary>
        /// <param name="itemsPerSubject">A list of channels for a single subject.</param>
        /// <param name="discardInvalidSubjects">whether to drop problematic subjects (rather than throwing)</param>
        public static bool AreDatasetItemsValid(IReadOnlyList<VolumeAndMetadata> itemsPerSubject, bool discardInvalidSubjects)
        {
            var errorMessages = new List<string>();
            var warningMessages = new List<string>();
            var text = new StringBuilder();
            // Maps from a structure name in lower case to the series ID that contained that structure
            var structureNamesLowerCase = new Dictionary<string, VolumeMetadata>();
            string channelWithStructures = null;
            foreach (var item in itemsPerSubject)
            {
                var contourNames = string.Join(", ", item.Volume.Struct.Contours.Select(c => c.StructureSetRoi.RoiName));
                text.AppendLine($"Subject {item.Metadata.SubjectId}: series {item.Metadata.SeriesId}, channel '{item.Metadata.Channel}': {contourNames}");
                // We don't currently handle subjects that have structures attached to multiple different channels.
                if (item.Volume.Struct.Contours.Count == 0)
                {
                    continue;
                } else if (channelWithStructures != null)
                {
                    errorMessages.Append($"Series {item.Metadata.SeriesId} has structures on multiple channels: {channelWithStructures} and {item.Metadata.Channel}.");
                } else
                {
                    channelWithStructures = item.Metadata.Channel;
                }
                foreach (var contour in item.Volume.Struct.Contours)
                {
                    var contourNameLowerCase = contour.StructureSetRoi.RoiName.ToLowerInvariant();
                    if (structureNamesLowerCase.TryGetValue(contourNameLowerCase, out var otherVolume))
                    {
                        var thisSeries = item.Metadata.SeriesId;
                        var otherSeries = otherVolume.SeriesId;
                        var message = new StringBuilder($"Subject {item.Metadata.SubjectId}: after conversion to lower case, there is more than one structure with name '{contourNameLowerCase}'");
                        if (otherSeries == thisSeries)
                        {
                            message.Append($" in series {thisSeries}");
                        }
                        else
                        {
                            message.Append($". Affected series are {thisSeries} and {otherSeries} ");
                        }
                        warningMessages.Add(message.ToString());
                    }
                    else
                    {
                        structureNamesLowerCase.Add(contourNameLowerCase, item.Metadata);
                    }
                }
            }

            Trace.TraceInformation(text.ToString());
            warningMessages.ForEach(message => Trace.TraceWarning(message));
            if (errorMessages.Count > 0)
            {
                errorMessages.ForEach(message => Trace.TraceError(message));
                if (!discardInvalidSubjects)
                {
                    throw new InvalidOperationException("The dataset contains invalid structures. Inspect the console for details. First error: " + errorMessages.First());
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Computes a derived structure by applying an operator as specified in <paramref name="derived"/>, and saves 
        /// that to the first element of the argument <paramref name="itemsPerSubject"/>.
        /// </summary>
        /// <param name="itemsPerSubject"></param>
        /// <param name="derived"></param>
        public static void AddDerivedStructures(IReadOnlyList<VolumeAndStructures> itemsPerSubject, DerivedStructure derived)
        {
            Volume3D<byte> find(string name)
            {
                var result =
                    itemsPerSubject
                    .SelectMany(v => v.Structures)
                    .Where(s => s.Key == name)
                    .Select(v => v.Value)
                    .SingleOrDefault();
                if (result == null)
                {
                    throw new KeyNotFoundException($"There is no structure with name '{name}', which is required to compute the derived structure '{derived.Result}'");
                }

                return result;
            }
            var left = find(derived.LeftSide);
            var right = find(derived.RightSide);
            itemsPerSubject[0].Add(derived.Result, ComputeDerivedStructure(left, right, derived));
        }

        /// <summary>
        /// Creates a derived structure, with the given left and right sides of the operator. The operator 
        /// itself is consumed from <paramref name="derived"/>.Operator
        /// </summary>
        /// <param name="left">The binary mask that is the left side of the operator.</param>
        /// <param name="right">The binary mask that is the right side of the operator.</param>
        /// <param name="derived"></param>
        /// <returns></returns>
        public static Volume3D<byte> ComputeDerivedStructure(Volume3D<byte> left, Volume3D<byte> right, DerivedStructure derived)
        {
            var leftSize = Volume3DDimensions.Create(left);
            var rightSize = Volume3DDimensions.Create(right);
            if (!leftSize.Equals(rightSize))
            {
                throw new InvalidOperationException($"Structure for '{derived.LeftSide}' has size {leftSize}, but '{derived.RightSide}' has size {rightSize}");
            }

            Volume3D<byte> result;
            switch (derived.Operator)
            {
                case DerivedStructureOperator.Except:
                    result = left.MapIndexed(null, (leftValue, index) =>
                        leftValue == ModelConstants.MaskBackgroundIntensity
                        ? ModelConstants.MaskBackgroundIntensity
                        : right[index] == ModelConstants.MaskBackgroundIntensity
                        ? ModelConstants.MaskForegroundIntensity
                        : ModelConstants.MaskBackgroundIntensity
                    );
                    break;
                case DerivedStructureOperator.Union:
                    result = left.MapIndexed(null, (leftValue, index) =>
                        (leftValue != ModelConstants.MaskBackgroundIntensity
                        || right[index] != ModelConstants.MaskBackgroundIntensity)
                        ? ModelConstants.MaskForegroundIntensity
                        : ModelConstants.MaskBackgroundIntensity
                    );
                    break;
                default:
                    throw new NotImplementedException($"There is no implementation for operator '{derived.Operator}'");
            }

            return result;
        }

        /// <summary>
        /// Given structure names in descending priority order, create mutually exclusive masks such that at each voxel position,
        /// whenever a higher-priority structure has a foreground voxel, ensure that all lower-priority structures have background.
        /// The two sets of structure names do not have to be equal. If a structure name in the priority order is not present in
        /// the volumes, it is ignored; conversely, if a structure has a name not present in the priority order, it is not touched,
        /// but its name is included in the returned result. If a structure name in the priority order is prefixed with "+", e.g.
        /// "+external", then mutual exclusion is not enforced between that structure and any other, but the name is not included in
        /// the result, so such structures are not deleted from the volume.
        /// </summary>
        /// <param name="groundTruthStructures">the set of ground truth structures to enforce mutual exclusion on</param>
        /// <param name="structureNamesInDescendingPriority">Descending priority order for ground truth structures to be used to enforce
        /// mutual exclusion in dataset creation</param>
        /// <returns>The names of any structures in the volumes that do not occur in the
        /// priority order</returns>
        public static IEnumerable<string> MakeStructuresMutuallyExclusiveInPlace(
            IReadOnlyDictionary<string, Volume3D<byte>> groundTruthStructures,
            string[] structureNamesInDescendingPriority, int subjectId)
        {
            // Filter out any names in the priority list that do not occur in the structure dictionary. Names starting
            // with "+" are automatically filtered out here.
            var prioritySequenceToUse = structureNamesInDescendingPriority.Where(name => groundTruthStructures.ContainsKey(name) || name == "*").ToList();
            if (prioritySequenceToUse.Count > 0)
            {
                // Create an array of volumes in priority order.
                var volumesInOrder = prioritySequenceToUse.Select(name => groundTruthStructures[name]).ToArray();
                // Assume all volumes have same size
                var volumeLength = volumesInOrder[0].Length;
                // Record maskings-out so we can report them.
                var maskingCounts = new int[volumesInOrder.Length - 1][];
                for (int volumeIndex1 = 0; volumeIndex1 < volumesInOrder.Length - 1; volumeIndex1++)
                {
                    maskingCounts[volumeIndex1] = new int[volumesInOrder.Length];
                }
                for (int voxelIndex = 0; voxelIndex < volumeLength; voxelIndex++)
                {
                    // Find the first structure in the priority order that has a foreground voxel.
                    // Make the rest of structures after that 0 for the current voxel.
                    for (int volumeIndex1 = 0; volumeIndex1 < volumesInOrder.Length - 1; volumeIndex1++)
                    {
                        var volume = volumesInOrder[volumeIndex1];
                        if (volume[voxelIndex] == ModelConstants.MaskForegroundIntensity)
                        {
                            for (int volumeIndex2 = volumeIndex1 + 1; volumeIndex2 < volumesInOrder.Length; volumeIndex2++)
                            {
                                if (volumesInOrder[volumeIndex2][voxelIndex] != ModelConstants.MaskBackgroundIntensity)
                                {
                                    volumesInOrder[volumeIndex2][voxelIndex] = ModelConstants.MaskBackgroundIntensity;
                                    maskingCounts[volumeIndex1][volumeIndex2]++;
                                }
                            }
                            break;
                        }
                    }
                }
                for (int volumeIndex1 = 0; volumeIndex1 < volumesInOrder.Length - 1; volumeIndex1++)
                {
                    for (int volumeIndex2 = volumeIndex1 + 1; volumeIndex2 < volumesInOrder.Length; volumeIndex2++)
                    {
                        var count = maskingCounts[volumeIndex1][volumeIndex2];
                        if (count > 0)
                        {
                            var total = 0;
                            var vol = volumesInOrder[volumeIndex2];
                            for (int i = 0; i < vol.DimXY * vol.DimZ; i++)
                            {
                                if (vol[i] != ModelConstants.MaskBackgroundIntensity)
                                {
                                    total += 1;
                                }
                            }
                            Trace.TraceInformation($"Subject {subjectId}: {count} voxels of {prioritySequenceToUse[volumeIndex2]} were masked out by {prioritySequenceToUse[volumeIndex1]}, leaving {total}");
                        }
                    }
                }
            }
            // Augment the list of "good" structure names with any names that were prefixed with "+" in the original list.
            prioritySequenceToUse.AddRange(structureNamesInDescendingPriority.Where(name => name.StartsWith("+")).Select(name => name.Substring(1)));
            // Return any structure names not mentioned in the augmented list.
            return groundTruthStructures.Keys.Where(name => !prioritySequenceToUse.Contains(name)).ToArray();
        }

        /// <summary>
        /// Consumes a list of volumes for different imaging channels, all for the same subject. All the images
        /// and structures will be registered against the reference volume given in <paramref name="registerOnChannel"/>,
        /// and returned as instances of <see cref="Volume3D{T}"/>.
        /// If no reference channel is given, the subject volumes are not registered.
        /// If registration is carried, the first element of the result belongs to the reference channel,
        /// followed by all other channels.
        /// </summary>
        /// <param name="subjectVolumes">A list of medical volumes for different channels.</param>
        /// <param name="registerOnChannel">The channel that should be used as the reference channel for registration.
        /// Set to null or an empty string to not register at all.</param>
        /// <returns></returns>
        public static IReadOnlyList<VolumeAndStructures> RegisterSubjectVolumes(
            IReadOnlyList<VolumeAndStructures> subjectVolumes,
            string registerOnChannel)
        {
            if (subjectVolumes == null || subjectVolumes.Count == 0)
            {
                throw new ArgumentException("The subject data must be non-empty", nameof(subjectVolumes));
            }

            if (string.IsNullOrEmpty(registerOnChannel))
            {
                return subjectVolumes;
            }

            var perChannel = new Dictionary<string, VolumeAndStructures>();
            foreach (var item in subjectVolumes)
            {
                var channel = item.Metadata.Channel;
                if (perChannel.ContainsKey(channel))
                {
                    throw new ArgumentException($"Data contains multiple volumes for channel '{channel}'", nameof(subjectVolumes));
                }

                perChannel.Add(channel, item);
            }

            if (!perChannel.ContainsKey(registerOnChannel))
            {
                throw new ArgumentException($"Data does not contain the reference channel '{registerOnChannel}'", nameof(subjectVolumes));
            }

            var reference = perChannel[registerOnChannel];
            var otherChannels =
                perChannel
                .Where(keyValue => keyValue.Value.Metadata.Channel != registerOnChannel)
                .Select(keyValue => keyValue.Value)
                .ToList();
            var registered = ResampleVolumesToReference(reference, otherChannels);
            var result = new List<VolumeAndStructures>
            {
                perChannel[registerOnChannel]
            };
            result.AddRange(registered);
            return result;
        }

        /// <summary>
        /// Takes a list of volumes and structures per channel, and resamples them to match the geometry of the reference
        /// volume. The metadata information for each channel is updated to contain information about resampling and the 
        /// source series: If the channel provided is "flair", and the reference channel is "t1", then the output
        /// will contain a channel called "flair_onto_t1". The names of anatomical structures will remain
        /// as in the input, though.
        /// The returned enumerable has as many items as there are in <paramref name="otherChannels"/>.
        /// If resampling produces voxel values that are outside the value range of the input image, the offending
        /// voxels are clipped to be at the input image range.
        /// </summary>
        /// <param name="reference">The reference volume. The returned volumes and structures will all have
        /// the same geometry as the reference volume.</param>
        /// <param name="otherChannels">Volumes and structure for different channels other than the reference channel.</param>
        /// <param name="writeResultsToTempFolder">If true, the reference volume and the registration results
        /// will be written in Nifti format to the user's temp folder.</param>
        /// <returns></returns>
        public static IEnumerable<VolumeAndStructures> ResampleVolumesToReference(VolumeAndStructures reference,
            List<VolumeAndStructures> otherChannels,
            bool writeResultsToTempFolder = false)
        {
            if (otherChannels.Count == 0)
            {
                yield break;
            }

            var subjectId = reference.Metadata.SubjectId;
            var referenceProperties = Volume3DProperties.Create(reference.Volume);
            var referenceChannel = reference.Metadata.Channel;
            var tempFolder = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}_subject{subjectId}");

            // SimpleItk only understands Nifti Gzip compression or uncompressed. Using uncompressed
            // because it is faster to use.
            var niftiExtension = MedIO.GetNiftiExtension(NiftiCompression.Uncompressed);
            if (writeResultsToTempFolder)
            {
                Trace.TraceInformation($"Subject {subjectId}: writing temporary Nifti files to folder {tempFolder}");
                Directory.CreateDirectory(tempFolder);
            }
            string CreateNiftiFileName(string channel) => Path.Combine(tempFolder, channel + niftiExtension);
            void WriteIfNeeded(Image image, string channel)
            {
                if (writeResultsToTempFolder)
                {
                    var nifti = CreateNiftiFileName(referenceChannel);
                    SimpleITK.WriteImage(image, nifti);
                }
            }
            void CheckProperties(Volume3DProperties properties, string channel)
            {
                try
                {
                    Volume3DProperties.CheckAllPropertiesEqual(new[] { referenceProperties, properties });
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException($"Registration for channel '{channel}' created size mismatch: {ex.Message}");
                }
            }
            var referenceImage = ItkImageFromManaged.FromVolume(reference.Volume);
            PrintImageOrientation(referenceImage.Image, "Reference image for registration:");
            WriteIfNeeded(referenceImage.Image, referenceChannel);
            var interpolationForVolume = InterpolatorEnum.sitkBSpline;
            var interpolationForStructures = InterpolatorEnum.sitkNearestNeighbor;
            string ChannelSuffixFromMethod(InterpolatorEnum method) => "_" + method.ToString().Substring(4);
            var knownChannels = new HashSet<string> { referenceChannel };
            foreach (var item in otherChannels)
            {
                var channel = item.Metadata.Channel;
                if (!knownChannels.Add(channel))
                {
                    throw new ArgumentException($"Subject {subjectId}: channel '{channel}' is present multiple times.", nameof(otherChannels));
                }
                Trace.TraceInformation($"Subject {subjectId}: starting to register volumes and structures for channel '{channel}'");
                var image = ItkImageFromManaged.FromVolume(item.Volume);
                var voxelRange = item.Volume.GetMinMax();
                PrintImageOrientation(image.Image, "Subject {subjectId}: image orientation for this channel:");
                WriteIfNeeded(image.Image, channel);
                Trace.TraceInformation($"Subject {subjectId}: registering the scan");
                var imageResampled = ResampleImage(referenceImage, image, interpolationForVolume);
                WriteIfNeeded(imageResampled, channel + ChannelSuffixFromMethod(interpolationForVolume));
                var volumeResampled = SimpleItkConverters.ImageToVolumeShort(imageResampled);
                CheckProperties(Volume3DProperties.Create(volumeResampled), channel);
                volumeResampled.ClipToRangeInPlace(voxelRange);
                var structuresResampled = new Dictionary<string, Volume3D<byte>>();
                foreach (var structure in item.Structures)
                {
                    var name = structure.Key;
                    Trace.TraceInformation($"Subject {subjectId}: registering structure '{name}'");
                    if (!knownChannels.Add(name))
                    {
                        throw new ArgumentException($"Subject {subjectId}: channel '{name}' is present multiple times.", nameof(otherChannels));
                    }
                    var maskImage = ItkImageFromManaged.FromVolume(structure.Value);
                    var maskImageResampled = ResampleImage(referenceImage, maskImage, interpolationForStructures);
                    WriteIfNeeded(maskImageResampled, name + ChannelSuffixFromMethod(interpolationForStructures));
                    var maskResampled = SimpleItkConverters.ImageToVolumeByte(maskImageResampled);
                    CheckProperties(Volume3DProperties.Create(maskResampled), channel);
                    structuresResampled.Add(name, maskResampled);
                }

                var newChannelName = $"{item.Metadata.Channel}_onto_{referenceChannel}";
                var updatedMetadata =
                    item.Metadata
                    .UpdateSeriesId($"Resampling on '{referenceChannel}' via '{interpolationForVolume}' and '{interpolationForStructures}' of ")
                    .UpdateChannel(newChannelName);
                yield return new VolumeAndStructures(volumeResampled, structuresResampled, updatedMetadata);
            }
        }

        /// <summary>
        /// Writes human readable information about the image to Trace, including a header line.
        /// The information includes the voxel type, the origin vector and the orientation vector.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="loggingHeader"></param>
        public static void PrintImageOrientation(Image image, string loggingHeader)
        {
            string PrintVector(VectorDouble vector) => $"({string.Join(", ", vector.Select(v => v.ToString("0.00")))})";
            var text = new StringBuilder();
            text.AppendLine(loggingHeader);
            text.AppendLine($"Voxel type: {image.GetPixelIDTypeAsString()}");
            text.AppendLine($"Origin {PrintVector(image.GetOrigin())}");
            text.AppendLine($"Direction {PrintVector(image.GetDirection())}");
            text.AppendLine($"Spacing {PrintVector(image.GetSpacing())}");
            Trace.TraceInformation(text.ToString());
        }

        /// <summary>
        /// For a 3-dimensional image, gets the 8 voxel values in the 8 corners of the image.
        /// This is only implemented for images with pixel type Int16 (short) and UInt8 (byte).
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static IEnumerable<double> ValuesInCorners(Image image)
        {
            Debug.Assert(image.GetDimension() == 3);
            var size = image.GetSize();
            var valuesInCorners = new List<double>();
            Func<VectorUInt32, double> pixelGetter;
            if (image.GetPixelID() == PixelIDValueEnum.sitkInt16)
            {
                pixelGetter = vector => image.GetPixelAsInt16(vector);
            }
            else
            {
                if (image.GetPixelID() == PixelIDValueEnum.sitkUInt8)
                {
                    pixelGetter = vector => image.GetPixelAsUInt8(vector);
                }
                else
                {
                    throw new ArgumentException($"Unsupported pixel type {image.GetPixelIDTypeAsString()}", nameof(image));
                }
            }

            foreach (var dim0 in new uint[] { 0, size[0] - 1 })
            {
                foreach (var dim1 in new uint[] { 0, size[1] - 1 })
                {
                    foreach (var dim2 in new uint[] { 0, size[2] - 1 })
                    {
                        var index = new VectorUInt32(new[] { dim0, dim1, dim2 });
                        valuesInCorners.Add(pixelGetter(index));
                    }
                }
            }
            return valuesInCorners;
        }

        /// <summary>
        /// Maps an image onto a reference image, and resamples the area of overlap to the geometry of the 
        /// reference image. Areas where the reference image has voxels, but the <paramref name="otherImage"/> does 
        /// not, will be filled with a default value taken from the 8 corners of the <paramref name="otherImage"/>.
        /// </summary>
        /// <param name="referenceImage">The reference image. The returned image will have the same geometry
        /// as the reference.</param>
        /// <param name="otherImage">The image to map onto the reference image.</param>
        /// <param name="interpolationMethod">The SimpleITK interpolation method that should be used for resampling.</param>
        /// <returns></returns>
        public static Image ResampleImage(ItkImageFromManaged referenceImage, ItkImageFromManaged otherImage, InterpolatorEnum interpolationMethod)
        {
            var resampler = new ResampleImageFilter();
            resampler.SetReferenceImage(referenceImage.Image);
            resampler.SetInterpolator(interpolationMethod);
            // When resampling a smaller image onto a larger one, gaps need to be filled
            // with a default pixel value. A reasonable guess is that the corners of a typical
            // scan represent air. Take the average of those, and use as the default fill value.
            resampler.SetDefaultPixelValue(ValuesInCorners(otherImage.Image).Average());
            return resampler.Execute(otherImage.Image);
        }

        /// <summary>
        /// Writes the dataset metadata in the argument to Trace in human readable form, including
        /// size of the dataset, channels, and SQL queries.
        /// </summary>
        /// <param name="dataset">The dataset to print out.</param>
        //public static void PrintDatasetMetadata(AdminAPI.Client.Dataset dataset)
        //{
        //    var text = new StringBuilder();
        //    text.AppendLine($"Dataset Name: {dataset.DatasetName}");
        //    text.AppendLine($"Dataset created on {dataset.CreatedDateTime.ToUniversalTime().ToString("s")} UTC by {dataset.UserName}");
        //    var structures = dataset.UniqueStructureNames.ToList();
        //    text.AppendLine($"Dataset contains {dataset.DatasetSize} entries with {structures.Count} unique structures: {string.Join(", ", structures)}");
        //    foreach (var query in dataset.SqlQueries)
        //    {
        //        text.AppendLine($"Query to get data for channel '{query.ChannelName}':");
        //        text.AppendLine(query.SqlQuery);
        //    }
        //    Trace.TraceInformation(text.ToString());

        //    if (dataset.Warnings != null && dataset.Warnings.Count > 0)
        //    {
        //        text.Clear();
        //        text.AppendLine($"When creating the dataset, a total of {dataset.Warnings.Count} warnings were written:");
        //        foreach (var warning in dataset.Warnings)
        //        {
        //            text.AppendLine(warning);
        //        }
        //        Trace.TraceWarning(text.ToString());
        //    }
        //}

        /// <summary>
        /// Runs dataset creation as specified by the commandline options given.
        /// </summary>
        /// <param name="options"></param>
        public static void CreateDataset(CommandlineCreateDataset options)
        {
            var dataRoot = new LocalFileSystem(options.DatasetRootDirectory, false);
            var isDatasetFolderAvailable = dataRoot.DirectoryExists(options.NiftiDirectory);

            if (isDatasetFolderAvailable)
            {
                throw new InvalidOperationException($"There is already a dataset with name '{options.NiftiDirectory}' in the dataset directory.");
            }
            try
            {
                CreateDataset(dataRoot, options);
            }
            catch (Exception ex)
            {
                string error = $"Cannot convert dataset '{options.DicomDirectory}'. Ensure that the folder '{options.NiftiDirectory}' is deleted before re-trying! Error: {ex.Message}";
                Trace.TraceError(error);
                throw new InvalidOperationException(error, ex);
            }

        }
    }
}
