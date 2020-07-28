///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO
{
    using Dicom;
    using MedLib.IO.Extensions;
    using InnerEye.CreateDataset.Volumes;
    using Models;
    using Models.DicomRt;
    using Readers;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Writers;
    using static MedLib.IO.NiftiIO.NiftiInternal;

    public struct VolumeLoaderResult
    {
        public VolumeLoaderResult(string seriesId, MedicalVolume volume, Exception error, IReadOnlyList<string> warnings)
        {
            SeriesUid = seriesId;
            Volume = volume;
            Error = error;
            Warnings = warnings;
        }

        /// <summary>
        /// The DICOM series UID that the volume belongs to
        /// </summary>
        public string SeriesUid { get; }

        /// <summary>
        /// The medical volume or null if Error != null
        /// </summary>
        public MedicalVolume Volume { get; }

        /// <summary>
        /// Contains the first exception that occurred attempting to load a volume => volume = null
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// A list of warnings that occured loading the volume => volume != null
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }
    }

    /// <summary>
    /// Contains methods to load and save different representations of medical volumes, working
    /// with Dicom, Nifti and HDF5 files.
    /// </summary>
    public class MedIO
    {
        /// <summary>
        /// The suffix for files that contain uncompressed Nifti data.
        /// </summary>
        public const string UncompressedNiftiSuffix = ".nii";

        /// <summary>
        /// The suffix for files that contain GZIP compressed Nifti data.
        /// </summary>
        public const string GZipCompressedNiftiSuffix = UncompressedNiftiSuffix + ".gz";

        /// <summary>
        /// The suffix for files that contain LZ4 compressed Nifti data.
        /// </summary>
        public const string LZ4CompressedNiftiSuffix = UncompressedNiftiSuffix + ".lz4";

        /// <summary>
        /// The suffix for files that contain uncompressed HDF5 data.
        /// </summary>
        public const string UncompressedHDF5Suffix = ".h5";

        /// <summary>
        /// The suffix for files that contain GZIP compressed HDF5 data.
        /// </summary>
        public const string GZipCompressedHDF5Suffix = UncompressedHDF5Suffix + ".gz";

        /// <summary>
        /// The suffix for files that contain SZIP compressed HDF5 data.
        /// </summary>
        public const string SZipCompressedHDF5Suffix = UncompressedHDF5Suffix + ".sz";

        /// <summary>
        /// Gets the type of compression that was applied to the given Nifti file,
        /// by looking at the file extension.
        /// If the given file name is neither a compressed nor an uncompressed Nifti file,
        /// return null.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static NiftiCompression? GetNiftiCompression(string path)
        {
            if (path.EndsWith(GZipCompressedNiftiSuffix))
            {
                return NiftiCompression.GZip;
            }
            if (path.EndsWith(LZ4CompressedNiftiSuffix))
            {
                return NiftiCompression.LZ4;
            }
            if (path.EndsWith(UncompressedNiftiSuffix))
            {
                return NiftiCompression.Uncompressed;
            }
            return null;
        }

        /// <summary>
        /// Returns true if the given file name identifies a Nifti file, either compressed 
        /// or uncompressed.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool IsNiftiFile(string fileName)
        {
            var compression = GetNiftiCompression(fileName);
            return compression != null;
        }

        /// <summary>
        /// Gets the type of compression that was applied to the given Nifti file,
        /// by looking at the file extension.
        /// If the given file name is neither a compressed nor an uncompressed Nifti file,
        /// an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static NiftiCompression GetNiftiCompressionOrFail(string path)
        {
            var compression = GetNiftiCompression(path);
            if (compression == null)
            {
                throw new ArgumentException($"NIFTI filenames must end with '{UncompressedNiftiSuffix}' or '{GZipCompressedNiftiSuffix}' or '{LZ4CompressedNiftiSuffix}' (case sensitive), but got: {path}");
            }
            return compression.Value;
        }

        /// <summary>
        /// Gets the extension that a Nifti file should have when using the compression format
        /// given in the argument.
        /// </summary>
        /// <param name="compression"></param>
        /// <returns></returns>
        public static string GetNiftiExtension(NiftiCompression compression)
        {
            switch (compression)
            {
                case NiftiCompression.GZip:
                    return GZipCompressedNiftiSuffix;
                case NiftiCompression.LZ4:
                    return LZ4CompressedNiftiSuffix;
                case NiftiCompression.Uncompressed:
                    return UncompressedNiftiSuffix;
                default:
                    throw new ArgumentException($"Unsupported compression {compression}", nameof(compression));
            }
        }

        /// <summary>
        /// Expects path to point to a folder containing exactly 1 volume.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="acceptanceTests"></param>
        /// <returns></returns>
        public static async Task<MedicalVolume> LoadSingleDicomSeriesAsync(string path, IVolumeGeometricAcceptanceTest acceptanceTests)
        {
            var attributes = File.GetAttributes(path);

            if ((attributes & FileAttributes.Directory) != FileAttributes.Directory)
            {
                throw new ArgumentException("Folder path was expected.");
            }

            var results = await LoadAllDicomSeriesInFolderAsync(path, acceptanceTests);

            if (results.Count != 1)
            {
                throw new Exception("Folder contained multiple series.");
            }

            if (results[0].Error != null)
            {
                throw new Exception("Error loading DICOM series.", results[0].Error);
            }

            return results[0].Volume;
        }

        /// <summary>
        /// Loads a medical volume from a Nifti file. The <see cref="MedicalVolume.Volume"/> property
        /// will be set to the volume in the Nifti file, the RT structures will be empty, empty
        /// Dicom identifiers.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static MedicalVolume LoadMedicalVolumeFromNifti(string path)
        {
            var volume = LoadNiftiAsShort(path);

            return new MedicalVolume(
                volume,
                new DicomIdentifiers[0],
                new[] { path },
                RadiotherapyStruct.CreateDefault(new[] { DicomIdentifiers.CreateEmpty() }));
        }

        /// <summary>
        /// Loads a medical volume from a Nifti file. The <see cref="MedicalVolume.Volume"/> property
        /// will be set to the volume in the Nifti file, the RT structures will be empty, empty
        /// Dicom identifiers.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<MedicalVolume> LoadMedicalVolumeFromNiftiAsync(string path)
        {
            return await Task.Run(() => LoadMedicalVolumeFromNifti(path));
        }

        public static Tuple<RadiotherapyStruct, string> LoadStruct(string rtfile, Transform3 dicomToData, string studyUId, string seriesUId)
        {
            try
            {
                var file = DicomFile.Open(rtfile);
                return RtStructReader.LoadContours(file.Dataset, dicomToData, seriesUId, studyUId, true);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"RT file {rtfile} cannot be loaded - {ex.Message}");
            }
        }

        /// <summary>
        /// Analyse all DICOM files in the given folder and attempt to construct a volume for the given seriesUID
        /// </summary>
        /// <param name="pathFolder">The absolute path to the folder containing the DICOM files</param>
        /// <param name="seriesUID">The DICOM Series UID you wish to load</param>
        /// <param name="acceptanceTests">An implementation of IVolumeGeometricAcceptanceTest defining the geometric constraints of your application</param>
        /// <param name="loadStructuresIfExists">True if rt-structures identified in the folder and referencing seriesUID should be loaded</param>
        /// <param name="supportLossyCodecs">If you wish to accept lossy encodings of image pixel data</param>
        /// <returns></returns>
        public static async Task<VolumeLoaderResult> LoadDicomSeriesInFolderAsync(
            string pathFolder, string seriesUID, IVolumeGeometricAcceptanceTest acceptanceTests, bool loadStructuresIfExists = true, bool supportLossyCodecs = true)
        {
            var dfc = await DicomFileSystemSource.Build(pathFolder);
            var pSeriesUID = DicomUID.Parse(seriesUID);

            return LoadDicomSeries(dfc, pSeriesUID, acceptanceTests, loadStructuresIfExists, supportLossyCodecs);
        }

        /// <summary>
        /// Analyse all DICOM files in the given folder and attempt to construct all volumes for CT and MR series therein.
        /// </summary>
        /// <param name="pathFolder">The absolute path to the folder containing the DICOM files</param>
        /// <param name="acceptanceTests">An implementation of IVolumeGeometricAcceptanceTest defining the geometric constraints of your application</param>
        /// <param name="loadStructuresIfExists">True if rt-structures identified in the folder and referencing a volume should be loaded</param>
        /// <param name="supportLossyCodecs">If you wish to accept lossy encodings of image pixel data</param>
        /// <returns>A list of volume loading results for the specified folder</returns>
        public static async Task<IList<VolumeLoaderResult>> LoadAllDicomSeriesInFolderAsync(
            string pathFolder, IVolumeGeometricAcceptanceTest acceptanceTests, bool loadStructuresIfExists = true, bool supportLossyCodecs = true)
        {
            var stopwatch = Stopwatch.StartNew();
            var dfc = await DicomFileSystemSource.Build(pathFolder);
            stopwatch.Stop();
            Trace.TraceInformation($"Analysing folder structure took: {stopwatch.ElapsedMilliseconds} ms");

            return LoadAllDicomSeries(dfc, acceptanceTests, loadStructuresIfExists, supportLossyCodecs);
        }

        /// <summary>
        /// Attempt to load all volume for all CT and MR image series within the given DicomFolderContents
        /// </summary>
        /// <param name="dfc">A pre-built description of DICOM contents within a particular folder</param>
        /// <param name="acceptanceTests">An implementation of IVolumeGeometricAcceptanceTest defining the geometric constraints of your application</param>
        /// <param name="loadStructuresIfExists">True if rt-structures identified in the folder and referencing a volume should be loaded</param>
        /// <param name="supportLossyCodecs">If you wish to accept lossy encodings of image pixel data</param>
        /// <returns></returns>
        public static IList<VolumeLoaderResult> LoadAllDicomSeries(
            DicomFolderContents dfc, IVolumeGeometricAcceptanceTest acceptanceTests, bool loadStructuresIfExists, bool supportLossyCodecs)
        {
            var stopwatch = Stopwatch.StartNew();

            var resultList = new List<VolumeLoaderResult>();

            foreach (var s in dfc.Series)
            {
                if (s.SeriesUID != null)
                {
                    resultList.Add(LoadDicomSeries(dfc, s.SeriesUID, acceptanceTests, loadStructuresIfExists, supportLossyCodecs));
                }
            }

            stopwatch.Stop();
            Trace.TraceInformation($"Reading all DICOM series took: {stopwatch.ElapsedMilliseconds} ms");
            return resultList;
        }

        /// <summary>
        /// Loads a Nifti file from disk, returning it as a <see cref="Volume3D{T}"/> with datatype
        /// <see cref="byte"/>, irrespective of the datatype used in the Nifti file itself.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static Volume3D<byte> LoadNiftiAsByte(string path)
        {
            return LoadNiftiFromFile(path, NiftiIO.ReadNiftiAsByte);
        }

        /// <summary>
        /// Loads a Nifti file from disk, where the Nifti file is expected to have
        /// voxels in 'byte' format.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static Volume3D<byte> LoadNiftiInByteFormat(string path)
        {
            return LoadNiftiFromFile(path, NiftiIO.ReadNiftiInByteFormat);
        }

        /// <summary>
        /// Loads a Nifti file from disk, returning it as a <see cref="Volume3D{T}"/> with datatype
        /// <see cref="short"/>, irrespective of the datatype used in the Nifti file itself.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static Volume3D<short> LoadNiftiAsShort(string path)
        {
            return LoadNiftiFromFile(path, NiftiIO.ReadNiftiAsShort);
        }

        /// <summary>
        /// Loads a Nifti file from disk, where the Nifti file is expected to have
        /// voxels in 'short' format.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static Volume3D<short> LoadNiftiInShortFormat(string path)
        {
            return LoadNiftiFromFile(path, NiftiIO.ReadNiftiInShortFormat);
        }

        /// <summary>
        /// Loads a Nifti file from disk, where the Nifti file is expected to have
        /// voxels in 'ushort' (unsigned 16 bit integer) format.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static Volume3D<ushort> LoadNiftiInUShortFormat(string path)
        {
            return LoadNiftiFromFile(path, NiftiIO.ReadNiftiInUShortFormat);
        }

        /// <summary>
        /// Loads a Nifti file from disk, returning it as a <see cref="Volume3D{T}"/> with datatype
        /// <see cref="short"/>, irrespective of the datatype used in the Nifti file itself.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static async Task<Volume3D<short>> LoadNiftiAsShortAsync(string path)
        {
            return await Task.Run(() => LoadNiftiAsShort(path));
        }

        /// <summary>
        /// Loads a Nifti file from disk, returning it as a <see cref="Volume3D{T}"/> with datatype
        /// <see cref="float"/>, irrespective of the datatype used in the Nifti file itself.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static Volume3D<float> LoadNiftiAsFloat(string path)
        {
            return LoadNiftiFromFile(path, NiftiIO.ReadNiftiAsFloat);
        }

        /// <summary>
        /// Loads a Nifti file from disk, where the Nifti file is expected to have
        /// voxels in 'float' format.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static Volume3D<float> LoadNiftiInFloatFormat(string path)
        {
            return LoadNiftiFromFile(path, NiftiIO.ReadNiftiInFloatFormat);
        }

        /// <summary>
        /// Loads a Nifti file from disk, returning it as a <see cref="Volume3D{T}"/> with datatype
        /// <see cref="float"/>, irrespective of the datatype used in the Nifti file itself.
        /// </summary>
        /// <param name="path">The file to load.</param>
        /// <returns></returns>
        public static async Task<Volume3D<float>> LoadNiftiAsFloatAsync(string path)
        {
            return await Task.Run(() => LoadNiftiAsFloat(path));
        }

        /// <summary>
        /// Save a 3D volume to a file on the local disk, in compressed or uncompressed Nifti format.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename">The name of the file to write. The compression level will be
        /// chosen based on the file extension (compressed if the extension is .nii.gz)</param>
        public static void SaveNifti(Volume3D<short> image, string filename)
        {
            SaveNiftiToFile(filename, image, NiftiIO.WriteToStream);
        }

        /// <summary>
        /// Save a 3D volume to a file on the local disk, in compressed or uncompressed Nifti format.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename">The name of the file to write. The compression level will be
        /// chosen based on the file extension (compressed if the extension is .nii.gz)</param>
        public static async Task SaveNiftiAsync(Volume3D<short> image, string filename)
        {
            await Task.Run(() => SaveNifti(image, filename));
        }

        /// <summary>
        /// Save a 3D volume to a file on the local disk, in compressed or uncompressed Nifti format.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename">The name of the file to write. The compression level will be
        /// chosen based on the file extension (compressed if the extension is .nii.gz)</param>
        public static void SaveNifti(Volume3D<float> image, string filename)
        {
            SaveNiftiToFile(filename, image, NiftiIO.WriteToStream);
        }

        /// <summary>
        /// Save a 3D volume to a file on the local disk, in compressed or uncompressed Nifti format.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename">The name of the file to write. The compression level will be
        /// chosen based on the file extension (compressed if the extension is .nii.gz)</param>
        public static void SaveNifti(Volume3D<byte> image, string filename)
        {
            SaveNiftiToFile(filename, image, NiftiIO.WriteToStream);
        }

        /// <summary>
        /// Save a 3D volume to a file on the local disk, in compressed or uncompressed Nifti format.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename">The name of the file to write. The compression level will be
        /// chosen based on the file extension (compressed if the extension is .nii.gz)</param>
        public static async Task SaveNiftiAsync(Volume3D<byte> image, string filename)
        {
            await Task.Run(() => SaveNifti(image, filename));
        }

        /// <summary>
        /// Save all images and the rt struct to the given folder.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="medicalVolume"></param>
        /// <returns></returns>
        public static async Task SaveMedicalVolumeAsync(
            string folderPath, MedicalVolume medicalVolume)
        {
            await Task.WhenAll(
                SaveDicomImageAsync(folderPath, medicalVolume),
                SaveRtStructAsync(Path.Combine(folderPath, "rtstruct.dcm"), medicalVolume.Struct));
        }

        /// <summary>
        /// Returns a task that saves a copy of the original DICOM files forming a medical volume.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="medicalVolume"></param>
        /// <returns></returns>
        private static async Task SaveDicomImageAsync(string folderPath, MedicalVolume medicalVolume)
        {
            await Task.Run(() =>
                Parallel.ForEach(
                    medicalVolume.FilePaths,
                    file =>
                    {
                        if (!File.Exists(file))
                        {
                            throw new FileNotFoundException(
                                $"The original dicom directory was modified while using this image. File {file} is missing");
                        }

                        // ReSharper disable once AssignNullToNotNullAttribute
                        var destFile = Path.Combine(folderPath, Path.GetFileName(file));
                        File.Copy(file, destFile);
                    }));
        }

        /// <summary>
        /// Returns a task to save the given rtStruct to disk.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="rtStruct"></param>
        /// <returns></returns>
        public static async Task SaveRtStructAsync(string filename, RadiotherapyStruct rtStruct)
        {
            await Task.Run(
                () => RtStructWriter.SaveRtStruct(filename, rtStruct));
        }

        private static T LoadNiftiFromFile<T>(string path, Func<Stream,NiftiCompression,T> loadFromStream)
        {
            var compression = GetNiftiCompressionOrFail(path);
            using (var fileStream = new FileStream(path, FileMode.Open))
            {
                return loadFromStream(fileStream, compression);
            }
        }

        private static void SaveNiftiToFile<T>(string path, T volume, Action<Stream, T, NiftiCompression> saveToStream)
        {
            var compress = GetNiftiCompressionOrFail(path);
            try
            {
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    saveToStream(fileStream, volume, compress);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing to file {path}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Attempt to load a volume from the given SeriesUID for the given DicomFolderContents
        /// </summary>
        /// <param name="dfc">A pre-built description of DICOM contents within a particular folder</param>
        /// <param name="seriesUID">The DICOM seriesUID you wish to construct a volume for</param>
        /// <param name="acceptanceTests">An implementation of IVolumeGeometricAcceptanceTest defining the geometric constraints of your application</param>
        /// <param name="loadStructuresIfExists">True if rt-structures identified in the folder and referencing seriesUID should be loaded</param>
        /// <param name="supportLossyCodecs">If you wish to accept lossy encodings of image pixel data</param>
        /// <returns></returns>
        private static VolumeLoaderResult LoadDicomSeries(
            DicomFolderContents dfc, DicomUID seriesUID, IVolumeGeometricAcceptanceTest acceptanceTests, bool loadStructuresIfExists, bool supportLossyCodecs)
        {
            try
            {
                var dicomSeriesContent = dfc.Series.FirstOrDefault((s) => s.SeriesUID == seriesUID);

                var warnings = new List<string>();
                RadiotherapyStruct rtStruct = null;

                if (dicomSeriesContent != null)
                {
                    var volumeData = DicomSeriesReader.BuildVolume(dicomSeriesContent.Content.Select(x => x.File.Dataset), acceptanceTests, supportLossyCodecs);

                    if (volumeData != null && loadStructuresIfExists)
                    {
                        var rtStructData = dfc.RTStructs.FirstOrDefault(rt => rt.SeriesUID == seriesUID);
                        if (rtStructData != null)
                        {
                            if (rtStructData.Content.Count == 1)
                            {

                                var rtStructAndWarnings = RtStructReader.LoadContours(
                                    rtStructData.Content.First().File.Dataset,
                                    volumeData.Transform.DicomToData,
                                    seriesUID.UID,
                                    null,
                                    false);

                                rtStruct = rtStructAndWarnings.Item1;

                                var warning = rtStructAndWarnings.Item2;

                                if (!string.IsNullOrEmpty(warning))
                                {
                                    warnings.Add(warning);
                                }
                            }
                            else if (rtStructData.Content.Count > 1)
                            {
                                warnings.Add("There is more than 1 RT STRUCT referencing this series - skipping structure set load");
                            }
                        }
                    }
                    var dicomIdentifiers = dicomSeriesContent.Content.Select((v) => DicomIdentifiers.ReadDicomIdentifiers(v.File.Dataset)).ToArray();

                    if (rtStruct == null)
                    {
                        rtStruct = RadiotherapyStruct.CreateDefault(dicomIdentifiers);
                    }

                    var result = new MedicalVolume(
                        volumeData,
                        dicomIdentifiers,
                        dicomSeriesContent.Content.Select((d) => d.Path).ToArray(),
                        rtStruct);

                    return new VolumeLoaderResult(seriesUID.UID, result, null, warnings);
                }
                throw new Exception("Could not find that series");
            }
            catch (Exception oops)
            {
                return new VolumeLoaderResult(seriesUID.UID, null, oops, new List<string>());
            }
        }
    }
}