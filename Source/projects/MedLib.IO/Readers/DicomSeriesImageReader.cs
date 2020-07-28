///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Readers
{
    using System;
    using System.Threading.Tasks;
    using Dicom;
    using Dicom.Imaging;
    using Dicom.Imaging.Codec;
    using MedLib.IO.Models;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// DICOM reader class for decoding pixel data from a collection of DICOM datasets.
    /// </summary>
    public static class DicomSeriesImageReader
    {
        /// <summary>
        /// Builds a 3-dimensional volume from the provided volume information.
        /// This method will parallelise voxel extraction per slice.
        /// </summary>
        /// <param name="volumeInformation">The volume information.</param>
        /// <param name="maxDegreeOfParallelism">The maximum degrees of parallelism when extracting voxel data from the DICOM datasets.</param>
        /// <returns>The 3-dimensional volume.</returns>
        /// <exception cref="ArgumentNullException">The provided volume information was null.</exception>
        /// <exception cref="InvalidOperationException">The decoded DICOM pixel data was not the expected length.</exception>
        public static Volume3D<short> BuildVolume(
            VolumeInformation volumeInformation,
            uint maxDegreeOfParallelism = 100)
        {
            volumeInformation = volumeInformation ?? throw new ArgumentNullException(nameof(volumeInformation));

            // Allocate the array for reading the volume data.
            var result = new Volume3D<short>(
                (int)volumeInformation.Width,
                (int)volumeInformation.Height,
                (int)volumeInformation.Depth,
                volumeInformation.VoxelWidthInMillimeters,
                volumeInformation.VoxelHeightInMillimeters,
                volumeInformation.VoxelDepthInMillimeters,
                volumeInformation.Origin,
                volumeInformation.Direction);

            Parallel.For(
                0,
                volumeInformation.Depth,
                new ParallelOptions() { MaxDegreeOfParallelism = (int)maxDegreeOfParallelism },
                i => WriteSlice(result, volumeInformation.GetSliceInformation((int)i), (uint)i));

            return result;
        }

        /// <summary>
        /// Gets the uncompressed pixel data from the provided DICOM dataset.
        /// </summary>
        /// <param name="dataset">The DICOM dataset.</param>
        /// <returns>The uncompressed pixel data as a byte array.</returns>
        /// <exception cref="ArgumentNullException">The provided DICOM dataset was null.</exception>
        public static byte[] GetUncompressedPixelData(DicomDataset dataset)
        {
            dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));

            if (dataset.InternalTransferSyntax.IsEncapsulated)
            {
                // Decompress single frame from source dataset
                var transcoder = new DicomTranscoder(
                    inputSyntax: dataset.InternalTransferSyntax,
                    outputSyntax: DicomTransferSyntax.ExplicitVRLittleEndian);

                return transcoder.DecodeFrame(dataset, 0).Data;
            }
            else
            {
                // Pull uncompressed frame from source pixel data
                var pixelData = DicomPixelData.Create(dataset);
                return pixelData.GetFrame(0).Data;
            }
        }

        /// <summary>
        /// Writes a slice into the 3-dimensional volume based on the slice information and slice index provided.
        /// </summary>
        /// <param name="volume">The 3-dimensional volume.</param>
        /// <param name="sliceInformation">The slice information.</param>
        /// <param name="sliceIndex">The slice index the slice information relates to.</param>
        /// <exception cref="ArgumentException">The provided slice index was outside the volume bounds.</exception>
        /// <exception cref="InvalidOperationException">The decoded DICOM pixel data was not the expected length.</exception>
        private static unsafe void WriteSlice(Volume3D<short> volume, SliceInformation sliceInformation, uint sliceIndex)
        {
            // Check the provided slice index exists in the volume bounds.
            if (sliceIndex >= volume.DimZ)
            {
                throw new ArgumentException("Attempt to write slice outside the volume.", nameof(sliceIndex));
            }

            var data = GetUncompressedPixelData(sliceInformation.DicomDataset);

            // Checks the uncompressed data is the correct length. 
            if (data.Length < sizeof(short) * volume.DimXY)
            {
                throw new InvalidOperationException($"The decoded DICOM pixel data has insufficient length. Actual: {data.Length} Required: {sizeof(short) * volume.DimXY}");
            }

            if (sliceInformation.SignedPixelRepresentation)
            {
                WriteSignedSlice(data, volume, sliceIndex, (int)sliceInformation.HighBit, sliceInformation.RescaleIntercept, sliceInformation.RescaleSlope);
            }
            else
            {
                WriteUnsignedSlice(data, volume, sliceIndex, (int)sliceInformation.HighBit, sliceInformation.RescaleIntercept, sliceInformation.RescaleSlope);
            }
        }

        /// <summary>
        /// Writes a slice of signed data to the provided volume at the specified index.
        /// Note: This method is unsafe and only uses checking when creating a short value out of each voxel (to check for overflows).
        /// </summary>
        /// <param name="data">The uncompressed signed pixel data.</param>
        /// <param name="volume">The volume to write the slice into.</param>
        /// <param name="sliceIndex">The index of the slice to write.</param>
        /// <param name="highBit">The high bit value for reading the pixel information.</param>
        /// <param name="rescaleIntercept">The rescale intercept of the pixel data.</param>
        /// <param name="rescaleSlope">The rescale slope of the pixel data.</param>
        private static unsafe void WriteSignedSlice(
           byte[] data,
           Volume3D<short> volume,
           uint sliceIndex,
           int highBit,
           double rescaleIntercept,
           double rescaleSlope)
        {
            fixed (short* volumePointer = volume.Array)
            fixed (byte* dataPtr = data)
            {
                var slicePointer = volumePointer + volume.DimXY * sliceIndex;
                var dataPointer = dataPtr;

                for (var y = 0; y < volume.DimY; y++)
                {
                    for (var x = 0; x < volume.DimX; x++, dataPointer += 2, slicePointer++)
                    {
                        short value;

                        // Force unchecked so conversions won't cause overflow exceptions regardless of project settings.
                        unchecked
                        {
                            var bits = (ushort)(*dataPointer | *(dataPointer + 1) << 8);
                            value = (short)(bits << (15 - highBit));  // mask
                            value = (short)(value >> (15 - highBit)); // sign extend
                        }

                        // Force checked so out-of-range values will cause overflow exception.
                        checked
                        {
                            *slicePointer = (short)Math.Round(rescaleSlope * value + rescaleIntercept);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes a slice of unsigned data to the provided volume at the specified index.
        /// Note: This method is unsafe and only uses checking when creating a short value out of each voxel (to check for overflows).
        /// </summary>
        /// <param name="data">The uncompressed unsigned pixel data.</param>
        /// <param name="volume">The volume to write the slice into.</param>
        /// <param name="sliceIndex">The index of the slice to write.</param>
        /// <param name="highBit">The high bit value for reading the pixel information.</param>
        /// <param name="rescaleIntercept">The rescale intercept of the pixel data.</param>
        /// <param name="rescaleSlope">The rescale slope of the pixel data.</param>
        private static unsafe void WriteUnsignedSlice(
           byte[] data,
           Volume3D<short> volume,
           uint sliceIndex,
           int highBit,
           double rescaleIntercept,
           double rescaleSlope)
        {
            // Construct a binary mask such that all bit positions to the right of highbit and highbit 
            // are masked in, and all bit positions to the left are masked out.
            var mask = (2 << highBit) - 1;

            fixed (short* volumePointer = volume.Array)
            fixed (byte* dataPtr = data)
            {
                var slicePointer = volumePointer + volume.DimXY * sliceIndex;
                var dataPointer = dataPtr;

                for (var y = 0; y < volume.DimY; y++)
                {
                    for (var x = 0; x < volume.DimX; x++, dataPointer += 2, slicePointer++)
                    {
                        var value = (ushort)((*dataPointer | *(dataPointer + 1) << 8) & mask);

                        // Force checked so out-of-range values will cause overflow exception.
                        checked
                        {
                            *slicePointer = (short)Math.Round(rescaleSlope * value + rescaleIntercept);
                        }
                    }
                }
            }
        }
    }
}