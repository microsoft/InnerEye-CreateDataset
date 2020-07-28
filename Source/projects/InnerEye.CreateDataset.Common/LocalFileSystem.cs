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

    /// <summary>
    /// Provides an abstraction for reading and writing streams to the local file system.
    /// </summary>
    [DebuggerDisplay("Local in {RootDirectory}")]
    public class LocalFileSystem : StreamsFromFileSystem
    {
        /// <summary>
        /// Gets the root folder for reading and writing files. The root folder can be be a
        /// fully qualitfied path or relative.
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        /// Creates a new instance of a file system accessor, that reads/writes from a given root directory.
        /// </summary>
        /// <param name="rootDirectory">The top level folder. All file names are relative to this folder.</param>
        /// <param name="isReadOnly">If true, the object will refuse write operations, and can't delete its root folder.</param>
        public LocalFileSystem(string rootDirectory, bool isReadOnly)
        {
            var root = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            if (!Directory.Exists(rootDirectory))
            {
                throw new ArgumentException($"Root directory {rootDirectory} does not exist.");
            }
            RootDirectory = AddSeparatorAtEnd(ReplaceSeparators(root));
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Creates a new local file system accessor that writes to a newly created folder
        /// in the user's temp directory.
        /// </summary>
        /// <returns></returns>
        public static LocalFileSystem CreateInTempFolder()
        {
            return new LocalFileSystem(CreateRandomDirectory(), false);
        }

        /// <summary>
        /// Checks the directory information in the given filename, and creates the directories
        /// if they don't exist already. Directories can be nested: For a filename
        /// "/temp/sub1/sub2/file.txt", the following directories can be created if needed:
        /// "temp", "temp/sub1", and "temp/sub1/sub2".
        /// </summary>
        /// <param name="fileName"></param>
        public static void CreateDirectoryIfNeeded(string fileName)
        {
            var fullDir = Path.GetDirectoryName(fileName);
            if (fullDir == null)
            {
                throw new ArgumentNullException(nameof(fileName), $"File {fileName} has no directory.");
            }
            Directory.CreateDirectory(fullDir);
        }

        private string GetFullPath(string fileName, bool isPathForWriting, bool allowEmptyFilename = false)
        {
            if (isPathForWriting)
            {
                ThrowIfReadOnly();
            }
            if (!allowEmptyFilename && (string.IsNullOrWhiteSpace(fileName) || fileName.Length == 0))
            {
                throw new ArgumentException("File names must be strings that are not only whitespace", nameof(fileName));
            }
            if (fileName.Length > 1
                && (fileName[0] == DirectorySeparator || fileName[0] == Path.DirectorySeparatorChar))
            {
                throw new ArgumentException("File names must not start with a directory separator character.", nameof(fileName));
            }
            var fullPath = JoinPath(RootDirectory, fileName);
            if (isPathForWriting)
            {
                CreateDirectoryIfNeeded(fullPath);
            }
            return fullPath;
        }

        private string GetPathForReading(string fileName)
        {
            return GetFullPath(fileName, false, false);
        }

        private string GetPathForWriting(string fileName)
        {
            return GetFullPath(fileName, true, false);
        }

        /// <inheritdoc />
        public override Stream GetStreamForReading(string fileName)
        {
            return new FileStream(GetPathForReading(fileName), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <inheritdoc />
        public override Stream GetStreamForWriting(string fileName)
        {
            base.GetStreamForWriting(fileName);
            var permissions =
                AllowFileOverwrite
                ? FileMode.Create
                : FileMode.CreateNew;
            return new FileStream(GetPathForWriting(fileName), permissions, FileAccess.Write);
        }

        /// <inheritdoc />
        public override bool FileExists(string fileName)
        {
            Trace.TraceInformation($"Local: {nameof(FileExists)} on {fileName}");
            return File.Exists(GetPathForReading(fileName));
        }

        /// <summary>
        /// Gets whether an input directory of the given name exists.
        /// </summary>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public bool DirectoryExists(string directoryName)
        {
            Trace.TraceInformation($"Local: {nameof(DirectoryExists)} on {directoryName}");
            return Directory.Exists(GetPathForWriting(directoryName));
        }

        /// <inheritdoc />
        public override void DeleteAll()
        {
            base.DeleteAll();
            Directory.Delete(RootDirectory, true);
        }

        /// <summary>
        /// Recursively enumerates all files in the given folder, going through
        /// all subdirectories.
        /// </summary>
        /// <param name="folder">The folder to start in.</param>
        /// <returns></returns>
        public static List<string> RecursiveEnumerate(string folder)
        {
            var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories);
            return files.ToList();
        }

        /// <inheritdoc/>
        public override IReadOnlyList<string> EnumerateFiles(string prefix)
        {
            var prefixAsString = ReplaceSeparators(prefix ?? string.Empty);
            try
            {
                // Enumerating files works with prefixes, not directories. If we have prefix "SubFold", and two
                // directories present that are called "SubFolder1" and "Subfolder2", we want to enumerate the contents
                // of both, and later filter by prefix.
                var folder = Path.GetDirectoryName(GetFullPath(prefixAsString, isPathForWriting: false, allowEmptyFilename: true));
                return
                    RecursiveEnumerate(folder)
                    .Select(file => StripPrefix(ReplaceSeparators(file), RootDirectory))
                    .Where(file => file.StartsWith(prefixAsString, StringComparison.Ordinal))
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<string>(0);
            }
        }

        /// <inheritdoc />
        public override long GetFileLength(string fileName)
        {
            return new FileInfo(GetPathForReading(fileName)).Length;
        }

        private Uri GetUriForAnyFileName(string fileName)
        {
            // For some tests, the root path is a relative path in the test assembly
            // output folder. We can't create a URI from that - convert to a fully qualified path.
            var rootPath = GetFullPath(fileName, isPathForWriting: false, allowEmptyFilename: true);
            return new Uri(Path.GetFullPath(rootPath));
        }

        /// <inheritdoc />
        public override string GetUri(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("The file name must be a non-empty string.", nameof(fileName));
            }
            return GetUriForAnyFileName(fileName).ToString();
        }

        /// <inheritdoc />
        public override string GetUriForRoot()
        {
            return GetUriForAnyFileName(string.Empty).ToString();
        }

        /// <summary>
        /// Creates a new directory with a random filename, located in the user's temp folder,
        /// and returns its path. If "prefix" is non-null, it is prepended to the random
        /// file (base) name.
        /// </summary>
        /// <returns></returns>
        public static string CreateRandomDirectory(string prefix=null)
        {
            var rootDir = Path.Combine(Path.GetTempPath(), prefix + Path.GetRandomFileName());
            Console.WriteLine($"Creating temporary folder {rootDir}");
            Directory.CreateDirectory(rootDir);
            return rootDir;
        }

        public override void CopyFromStreamToFile(Stream stream, string fileName)
        {
            using (var fileStream = GetStreamForWriting(fileName))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }
        }
    }
}
