///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using MedLib.IO.Readers;

    public class ZippedDicom
    {
        /// <summary>
        /// Opens the given byte array as a Zip archive, and returns Dicom files for all entries 
        /// in the archive.
        /// </summary>
        /// <param name="zipArchive">The full Zip archive as a byte array.</param>
        /// <returns></returns>
        public static IEnumerable<DicomFileAndPath> DicomFilesFromZipArchive(byte[] zipArchive)
        {
            using (var zip = new ZipArchive(new MemoryStream(zipArchive), ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    yield return DicomFileAndPath.SafeCreate(new MemoryStream(ToByteArray(entry)), string.Empty);
                }
            }
        }

        /// <summary>
        /// Creates a Zip archive that contains all the given Dicom files, and writes the Zip archive
        /// to the stream.
        /// </summary>
        /// <param name="stream">The stream to which the Zip archive should be written.</param>
        /// <param name="dicomFiles">The Dicom files to write to the Zip archive.</param>
        /// <returns></returns>
        public static void DicomFilesToZipArchive(IEnumerable<DicomFileAndPath> dicomFiles, Stream stream)
        {
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var dicomCount = 0;
                foreach (var file in dicomFiles)
                {
                    dicomCount++;
                    var dicomFileStream = new MemoryStream();
                    file.File.Save(dicomFileStream);
                    dicomFileStream.Seek(0, SeekOrigin.Begin);
                    var zipName =
                        string.IsNullOrWhiteSpace(file.Path)
                        ? $"DicomFile{dicomCount}.dcm"
                        : file.Path;
                    var zipEntry = zip.CreateEntry(zipName, CompressionLevel.Fastest);
                    using (var entryStream = zipEntry.Open())
                    {
                        dicomFileStream.CopyTo(entryStream);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a Zip archive that contains all the given Dicom files, and returns the
        /// Zip archive as a byte array.
        /// </summary>
        /// <param name="dicomFiles">The Dicom files to write to the Zip archive.</param>
        /// <returns></returns>
        public static byte[] DicomFilesToZipArchive(IEnumerable<DicomFileAndPath> dicomFiles)
        {
            using (var zipStream = new MemoryStream())
            {
                DicomFilesToZipArchive(dicomFiles, zipStream);
                return zipStream.ToArray();
            }
        }

        /// <summary>
        /// The input byte[] containing zip file contents is deflated.
        /// Resulting file names and their contents are returned.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static (string FileName, byte[] Data)[] GetUncompressedPayload(byte[] data)
        {
            var files = new List<(string FileName, byte[] Data)>();

            using (var zipToOpen = new MemoryStream(data))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        byte[] bytes = ToByteArray(entry);
                        files.Add((FileName: entry.Name, Data: bytes));
                    };
                };

            };

            return files.ToArray();
        }

        /// <summary>
        /// Read the contents of ziparchiveentry and return them as a byte array.
        /// </summary>
        /// <param name="entry">The zip archive entry.</param>
        /// <returns>The entry contents as a byte array.</returns>
        private static byte[] ToByteArray(ZipArchiveEntry entry)
        {
            using (var entryStream = entry.Open())
            using (var memoryStream = new MemoryStream())
            {
                entryStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
