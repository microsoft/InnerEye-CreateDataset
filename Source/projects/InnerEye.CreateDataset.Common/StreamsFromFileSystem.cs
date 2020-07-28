///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using MedLib.IO;

    using InnerEye.CreateDataset.Volumes;

    using MoreLinq;

    /// <summary>
    /// Provides an abstraction for reading and writing streams to a file system.
    /// </summary>
    public abstract class StreamsFromFileSystem
    {
        private readonly List<string> _filesWritten = new List<string>();

        // An object for locking the _filesWritten list. 
        private object listLock = new object();

        /// <summary>
        /// Gets the filenames of all files that have yet been written by this object.
        /// </summary>
        public IReadOnlyList<string> FilesWritten()
        {
            List<string> files;
            lock (listLock)
            {
                files = _filesWritten.ToList();
            }
            return files;
        }

        /// <summary>
        /// Throws an exception if the object is in read-only mode.
        /// </summary>
        public void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("The operation cannot be completed because the object accesses the file system in read-only mode.");
            }
        }

        /// <summary>
        /// The directory separator that should be used when enumerating files.
        /// </summary>
        public const char DirectorySeparator = '/';

        /// <summary>
        /// Gets whether the object is in read only mode. Writing to the file system
        /// will cause an exception in read-only mode.
        /// </summary>
        public bool IsReadOnly;

        /// <summary>
        /// Gets whether the object allows to overwrite existing files.
        /// </summary>
        public virtual bool AllowFileOverwrite { get; set; }

        /// <summary>
        /// Gets a stream that can be used to read from an existing file.
        /// Throws a <see cref="FileNotFoundException"/> if no file of that given name exists.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public abstract Stream GetStreamForReading(string fileName);

        /// <summary>
        /// Gets a stream that can be used to write to the file system, where the result
        /// will get the given file name.
        /// Throws an exception if the file system access is read-only.
        /// Throws an exception if the file already exists, and the <see cref="AllowFileOverwrite"/>
        /// property is false.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public virtual Stream GetStreamForWriting(string fileName)
        {
            ThrowIfReadOnly();
            // Multiple threads can request read streams at the same time, that sometimes
            // leads to IndexOutOfRangeExceptions.
            lock (listLock)
            {
                _filesWritten.Add(fileName);
            }
            // Implement this is a virtual function such that implementations can re-use the read-only check
            // and the bookkeeping. Returning null is not optimal, but I could not come up with a better solution.
            return null;
        }

        /// <summary>
        /// Writes a file from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fileName">Name of the file to be written</param>
        public abstract void CopyFromStreamToFile(Stream stream, string fileName);

        /// <summary>
        /// Gets all file names that are available, where the file name
        /// starts with the given prefix. Each of the returned files will start with that prefix.
        /// The prefix does not need to be aligned in any way with a folder structure that the
        /// underlying file system may implement. When the files live in a directory structure,
        /// the returned file name will use DirectorySeparator as the sepator, independent of the
        /// separator character that the underlying file system uses.
        /// The file names returned must be such that they can directly be used in a GetStreamForReading
        /// operation.
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public abstract IReadOnlyList<string> EnumerateFiles(string prefix);

        /// <summary>
        /// Gets the size in bytes of a specific file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public abstract long GetFileLength(string fileName);

        /// <summary>
        /// Gets whether an input file of the given name exists.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public abstract bool FileExists(string fileName);

        /// <summary>
        /// Gets the URI for the file of the given name, including all relevant root folders if needed.
        /// The URI should include any access tokens to allow read access, if needed. The file may or may not
        /// exist already. Throws an ArgumentException if the filename is missing or an empty string.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public abstract string GetUri(string fileName);

        /// <summary>
        /// Gets the URI of the root folder or container that stores the files.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public abstract string GetUriForRoot();

        /// <summary>
        /// Deletes all files that are in the directory/container,
        /// as well as the actual directory/container. Throws an exception if
        /// the object is in read-only mode.
        /// </summary>
        public virtual void DeleteAll()
        {
            ThrowIfReadOnly();
        }

        /// <summary>
        /// Gets the encoding that the IO abstraction uses for reading and writing text.
        /// </summary>
        public static readonly Encoding TextEncoding = Encoding.UTF8;

        /// <summary>
        /// Writes text data to the file system.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="text"></param>
        public void WriteAllText(string fileName, string text)
        {
            using (var writeStream = GetStreamForWriting(fileName))
            {
                var bytes = TextEncoding.GetBytes(text);
                writeStream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// Writes text data to the file system.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="lines"></param>
        public void WriteAllLines(string fileName, IEnumerable<string> lines)
        {
            using (var writeStream = GetStreamForWriting(fileName))
            using (var textWriter = new StreamWriter(writeStream, TextEncoding, bufferSize: 1024, leaveOpen: true))
            {
                lines.ForEach(line => textWriter.WriteLine(line));
            }
        }

        /// <summary>
        /// Reads all text that is available in the file of given name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string ReadAllText(string fileName)
        {
            using (var stream = GetStreamForReading(fileName))
            using (var reader = new StreamReader(stream, TextEncoding,
                detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Reads all text that is available in the file of given name, using lazy reading.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public IEnumerable<string> ReadAllLines(string fileName)
        {
            using (var stream = GetStreamForReading(fileName))
            using (var reader = new StreamReader(stream, TextEncoding,
                detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    yield return line;
                    line = reader.ReadLine();
                }
            }
        }

        /// <summary>
        /// Saves a medical image using the given file name, by opening a write stream and then invoking the
        /// given 'save' action to save to that stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="image"></param>
        /// <param name="save"></param>
        private void SaveImage<T>(string fileName, Volume3D<T> image, Action<Stream, Volume3D<T>, NiftiCompression> save)
        {
            var compression = MedIO.GetNiftiCompressionOrFail(fileName);
            using (var writeStream = GetStreamForWriting(fileName))
            {
                save(writeStream, image, compression);
            }
        }

        /// <summary>
        /// Writes a medical image to the file system, using the given file name.
        /// Compression level will be decided based upon the file extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="image"></param>
        public void SaveImage(string fileName, Volume3D<byte> image)
        {
            SaveImage(fileName, image, NiftiIO.WriteToStream);
        }

        /// <summary>
        /// Writes a medical image to the file system, using the given file name.
        /// Compression level will be decided based upon the file extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="image"></param>
        public void SaveImage(string fileName, Volume3D<short> image)
        {
            SaveImage(fileName, image, NiftiIO.WriteToStream);
        }

        /// <summary>
        /// Writes a medical image to the file system, using the given file name.
        /// Compression level will be decided based upon the file extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="image"></param>
        public void SaveImage(string fileName, Volume3D<float> image)
        {
            SaveImage(fileName, image, NiftiIO.WriteToStream);
        }

        /// <summary>
        /// Loads a medical volume from a file system, by opening a stream for reading, and
        /// then calling the given action to do the actual loading from the stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="loadAction"></param>
        /// <returns></returns>
        private Volume3D<T> LoadImage<T>(string fileName, Func<Stream, NiftiCompression, Volume3D<T>> loadAction)
        {
            var compression = MedIO.GetNiftiCompressionOrFail(fileName);
            using (var readStream = GetStreamForReading(fileName))
            {
                return loadAction(readStream, compression);
            }
        }

        /// <summary>
        /// Loads a medical image from the file system. The image file is expected to contain a byte image.
        /// An <see cref="InvalidDataException"/> is thrown if that is not the case.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public Volume3D<byte> LoadImageInByteFormat(string fileName)
        {
            return LoadImage(fileName, NiftiIO.ReadNiftiInByteFormat);
        }

        /// <summary>
        /// Loads a medical image from the file system. The image file is expected to contain an image
        /// with pixel values in short format. An <see cref="InvalidDataException"/> is thrown if that is not the case.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public Volume3D<short> LoadImageInShortFormat(string fileName)
        {
            return LoadImage(fileName, NiftiIO.ReadNiftiInShortFormat);
        }

        /// <summary>
        /// Loads a medical image from the file system. The image file is expected to contain an image
        /// with pixel values in single precision floating point format. 
        /// An <see cref="InvalidDataException"/> is thrown if that is not the case.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public Volume3D<float> LoadImageInFloatFormat(string fileName)
        {
            return LoadImage(fileName, NiftiIO.ReadNiftiInFloatFormat);
        }

        /// <summary>
        /// Joins two path strings via the directory separator.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static string JoinPath(string path1, string path2)
        {
            if (string.IsNullOrWhiteSpace(path1))
            {
                return path2;
            }
            if (string.IsNullOrWhiteSpace(path2))
            {
                return path1;
            }
            return AddSeparatorAtEnd(path1) + path2.TrimStart(DirectorySeparator);
        }

        /// <summary>
        /// Adds a directory separation character at the end of the path. If the path already ends with a 
        /// (single) separator, it is not changed.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string AddSeparatorAtEnd(string path)
        {
            return path.TrimEnd(DirectorySeparator) + DirectorySeparator;
        }

        /// <summary>
        /// Replaces all occurrences of the standard Windows file system separator backslash
        /// with the directory separator that the class is exposing.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string ReplaceSeparators(string fileName)
        {
            return fileName.Replace(Path.DirectorySeparatorChar, DirectorySeparator);
        }

        /// <summary>
        /// If the given text starts with the prefix, returns the substring after the prefix.
        /// Otherwise, returns the text unchanged.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static string StripPrefix(string text, string prefix)
        {
            return
                text.StartsWith(prefix, StringComparison.Ordinal)
                ? text.Substring(prefix.Length)
                : text;
        }
    }
}
