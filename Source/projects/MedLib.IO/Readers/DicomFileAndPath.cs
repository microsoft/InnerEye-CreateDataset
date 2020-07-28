///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿
namespace MedLib.IO.Readers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Dicom;

    /// <summary>
    /// Read only tuple of DicomFile and its original path
    /// </summary>
    public sealed class DicomFileAndPath
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="dicomFile">The value to use for the File property of the object.</param>
        /// <param name="path">The value to use for the Path property of the object.</param>
        public DicomFileAndPath(DicomFile dicomFile, string path)
        {
            File = dicomFile;
            Path = path;
        }

        /// <summary>
        /// Create a DicomFileAndPath from a path to a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DicomFileAndPath SafeCreate(string path)
        {
            try
            {
                return new DicomFileAndPath(DicomFile.Open(path), path);
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"DicomFileAndPath.Create: File {path} error {e}");
            }
            return null;
        }

        /// <summary>
        /// Create a DicomFileAndPath from a stream and path
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DicomFileAndPath SafeCreate(Stream stream, string path)
        {
            try
            {
                return new DicomFileAndPath(DicomFile.Open(stream), path);
            }
            catch (Exception e)
            {
                Trace.TraceInformation($"DicomFileAndPath.Create: Cannot open from stream. Error {e}");
            }
            return null;
        }

        /// <summary>
        /// The DicomFile associated with the given Path, 
        /// </summary>
        public DicomFile File { get; private set; }

        /// <summary>
        /// The file system path to the DicomFile
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Saves the Dicom file to the given folder. The filename is read from the <see cref="Path"/>
        /// property, which must be non-empty. Returns the full filename (folder plus filename) to which 
        /// the file was saved.
        /// </summary>
        /// <param name="folder">The directory into which the Dicom file should be saved. The directory must exist already.</param>
        public string SaveToFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                throw new InvalidOperationException("Saving requires a non-empty string in the Path property.");
            }
            var fullFilename = System.IO.Path.Combine(folder, Path);
            File.Save(fullFilename);
            return fullFilename;
        }
    }
}