///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using MedLib.IO.Extensions;
    using InnerEye.CreateDataset.TestHelpers;
    using NUnit.Framework;
    using static MedLib.IO.NiftiIO.NiftiInternal;

    [TestFixture]
    public class NiftiIOTests
    {
        [TestCase(-1f, 0)]
        [TestCase(0f, 0)]
        [TestCase(256.0f, 255)]
        [TestCase(254.5f, 254)]
        [TestCase(254.501f, 255)]
        public void SingleToByte(float raw, byte converted)
        {
            Assert.AreEqual(converted, NiftiIO.SingleToByte(raw));
        }

        [TestCase(-32768.9f, -32768)]
        [TestCase(-32768.1f, -32768)]
        [TestCase(-1f, -1)]
        [TestCase(32767.1f, 32767)]
        [TestCase(32766.51f, 32767)]
        [TestCase(32766.5f, 32766)]
        public void SingleToShort(float raw, short converted)
        {
            Assert.AreEqual(converted, NiftiIO.SingleToShort(raw));
        }

        [TestCase(".nii", NiftiCompression.Uncompressed)]
        [TestCase("file.nii", NiftiCompression.Uncompressed)]
        [TestCase(".nii.gz", NiftiCompression.GZip)]
        [TestCase("file.nii.gz", NiftiCompression.GZip)]
        [TestCase(".nii.lz4", NiftiCompression.LZ4)]
        [TestCase("file.nii.lz4", NiftiCompression.LZ4)]
        public void NiftiFileNamesValid(string fileName, NiftiCompression expected)
        {
            Assert.AreEqual(expected, MedIO.GetNiftiCompressionOrFail(fileName));
            Assert.IsTrue(MedIO.IsNiftiFile(fileName));
            var extension = MedIO.GetNiftiExtension(expected);
            Assert.IsTrue(fileName.EndsWith(extension));
        }

        /// <summary>
        /// Checks that the round trip from (Nifti compression as enum) to
        /// (Nifti file extension) to (Nifti compresion as enum) works.
        /// </summary>
        /// <param name="compression"></param>
        [TestCase(NiftiCompression.Uncompressed)]
        [TestCase(NiftiCompression.GZip)]
        [TestCase(NiftiCompression.LZ4)]
        public void NiftiFileExtension(NiftiCompression compression)
        {
            var extension = MedIO.GetNiftiExtension(compression);
            Assert.AreEqual(compression, MedIO.GetNiftiCompression(extension));
        }

        /// <summary>
        /// Test cases where the filename is not any known Nifti file. In particular, case mismatch:
        /// file system is case sensitive.
        /// </summary>
        /// <param name="fileName"></param>
        [TestCase("something.gz")]
        [TestCase("myFile.nii.Gz")]
        [TestCase("myFile.Nii")]
        public void NiftiFileNamesInvalid(string fileName)
        {
            Assert.Throws<ArgumentException>(() => MedIO.GetNiftiCompressionOrFail(fileName));
            Assert.IsFalse(MedIO.IsNiftiFile(fileName));
        }

        [TestCase(NiftiCompression.Uncompressed)]
        [TestCase(NiftiCompression.GZip)]
        [TestCase(NiftiCompression.LZ4)]
        public void NiftiSaveByte(NiftiCompression niftiCompression)
        {
            var volume = TestHelpers.SingleSlice(Enumerable.Range(0, 9).Select(b => (byte)b).ToArray());
            var file = TestHelpers.CreateTempNiftiName(niftiCompression);
            MedIO.SaveNifti(volume, file);
            var loadedVolume = MedIO.LoadNiftiAsByte(file);
            VolumeAssert.AssertVolumesMatch(volume, loadedVolume);
        }

        [TestCase(NiftiCompression.Uncompressed)]
        [TestCase(NiftiCompression.GZip)]
        [TestCase(NiftiCompression.LZ4)]
        public void NiftiSaveShort(NiftiCompression niftiCompression)
        {
            var volume = TestHelpers.SingleSlice(Enumerable.Range(0, 9).Select(b => (short)b).ToArray());
            var file = TestHelpers.CreateTempNiftiName(niftiCompression);
            MedIO.SaveNifti(volume, file);
            var loadedVolume = MedIO.LoadNiftiAsShort(file);
            VolumeAssert.AssertVolumesMatch(volume, loadedVolume);
        }

        [TestCase(NiftiCompression.Uncompressed)]
        [TestCase(NiftiCompression.GZip)]
        [TestCase(NiftiCompression.LZ4)]
        public void NiftiSaveFloat(NiftiCompression niftiCompression)
        {
            var volume = TestHelpers.SingleSlice(Enumerable.Range(0, 9).Select(b => (float)b).ToArray());
            var file = TestHelpers.CreateTempNiftiName(niftiCompression);
            MedIO.SaveNifti(volume, file);
            var loadedVolume = MedIO.LoadNiftiAsFloat(file);
            VolumeAssert.AssertVolumesMatch(volume, loadedVolume);
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to
        /// byte given a byte array
        /// </summary>
        [TestCase]
        public void BatchConvertByteByte()
        {
            var b = new byte[] { 0, 1, 2, 3, 4 };
            var outB = new byte[5];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_UNSIGNED_CHAR, b, outB, 1, 1)(0,5);

            Assert.IsTrue(b.Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion and clamps
        /// to byte given a byte array
        /// </summary>
        [TestCase]
        public void BatchConvertByteByteSaturate()
        {
            var b = new byte[] { 1, 2, 3, 4, 5 };
            var outB = new byte[5];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_UNSIGNED_CHAR, b, outB, 255, 1)(0, 5);

            Assert.IsTrue(b.Zip(outB, (v1, v2) => v2 == 255).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// short given a byte array
        /// </summary>
        [TestCase]
        public void BatchConvertByteShort()
        {
            var b = new byte[] { 0, 1, 2, 3, 4 };
            var outB = new short[5];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_UNSIGNED_CHAR, b, outB, 1, 1)(0, 5);

            Assert.IsTrue(b.Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// float given a byte array
        /// </summary>
        [TestCase]
        public void BatchConvertByteFloat()
        {
            var b = new byte[] { 0, 1, 2, 3, 4 };
            var outB = new float[5];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_UNSIGNED_CHAR, b, outB, 1, 1)(0, 5);

            Assert.IsTrue(b.Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// byte given a byte array containing floats
        /// </summary>
        [TestCase]
        public void BatchConvertFloatByte()
        {
            var bufSize = 10;
            var buffer = new byte[sizeof(float) * bufSize]; 
            using (var memStream = new MemoryStream(buffer))
            {
                using (var br = new BinaryWriter(memStream, System.Text.Encoding.Default, true))
                {
                    for ( var i = 0; i < bufSize; i++)
                    {
                        br.Write((float)i);
                    }
                }
            }

            var outB = new byte[bufSize];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_FLOAT, buffer, outB, 1, 1)(0, bufSize-1);

            Assert.IsTrue(Enumerable.Range(0, bufSize).Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// short given a byte array containing floats
        /// </summary>
        [TestCase]
        public void BatchConvertFloatShort()
        {
            var bufSize = 10;
            var buffer = new byte[sizeof(float) * bufSize];
            using (var memStream = new MemoryStream(buffer))
            {
                using (var br = new BinaryWriter(memStream, System.Text.Encoding.Default, true))
                {
                    for (var i = 0; i < bufSize; i++)
                    {
                        br.Write((float)i);
                    }
                }
            }

            var outB = new short[bufSize];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_FLOAT, buffer, outB, 1, 1)(0, bufSize - 1);

            Assert.IsTrue(Enumerable.Range(0, bufSize).Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// float given a byte array containing floats
        /// </summary>
        [TestCase]
        public void BatchConvertFloatFloat()
        {
            var bufSize = 10;
            var buffer = new byte[sizeof(float) * bufSize];
            using (var memStream = new MemoryStream(buffer))
            {
                using (var br = new BinaryWriter(memStream, System.Text.Encoding.Default, true))
                {
                    for (var i = 0; i < bufSize; i++)
                    {
                        br.Write((float)i);
                    }
                }
            }

            var outB = new float[bufSize];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_FLOAT, buffer, outB, 1, 1)(0, bufSize - 1);

            Assert.IsTrue(Enumerable.Range(0, bufSize).Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// byte given a byte array containing shorts
        /// </summary>
        [TestCase]
        public void BatchConvertShortByte()
        {
            var bufSize = 10;
            var buffer = new byte[sizeof(short) * bufSize];
            using (var memStream = new MemoryStream(buffer))
            {
                using (var br = new BinaryWriter(memStream, System.Text.Encoding.Default, true))
                {
                    for (var i = 0; i < bufSize; i++)
                    {
                        br.Write((short)i);
                    }
                }
            }

            var outB = new byte[bufSize];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_SIGNED_SHORT, buffer, outB, 1, 1)(0, bufSize - 1);

            Assert.IsTrue(Enumerable.Range(0, bufSize).Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// short given a byte array containing shorts
        /// </summary>
        [TestCase]
        public void BatchConvertShortShort()
        {
            var bufSize = 10;
            var buffer = new byte[sizeof(short) * bufSize];
            using (var memStream = new MemoryStream(buffer))
            {
                using (var br = new BinaryWriter(memStream, System.Text.Encoding.Default, true))
                {
                    for (var i = 0; i < bufSize; i++)
                    {
                        br.Write((short)i);
                    }
                }
            }

            var outB = new short[bufSize];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_SIGNED_SHORT, buffer, outB, 1, 1)(0, bufSize - 1);

            Assert.IsTrue(Enumerable.Range(0, bufSize).Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// short given a byte array containing floats
        /// </summary>
        [TestCase]
        public void BatchConvertShortFloat()
        {
            var bufSize = 10;
            var buffer = new byte[sizeof(short) * bufSize];
            using (var memStream = new MemoryStream(buffer))
            {
                using (var br = new BinaryWriter(memStream, System.Text.Encoding.Default, true))
                {
                    for (var i = 0; i < bufSize; i++)
                    {
                        br.Write((short)i);
                    }
                }
            }

            var outB = new float[bufSize];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_SIGNED_SHORT, buffer, outB, 1, 1)(0, bufSize - 1);

            Assert.IsTrue(Enumerable.Range(0, bufSize).Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests VolumeRescaleConvert.BatchConvert correctly applies rescale intercept and conversion to 
        /// byte given a byte array containing uint16
        /// </summary>
        [TestCase]
        public void BatchConvertByteUshort()
        {
            var bufSize = 10;
            var buffer = new byte[sizeof(ushort) * bufSize];
            using (var memStream = new MemoryStream(buffer))
            {
                using (var br = new BinaryWriter(memStream, System.Text.Encoding.Default, true))
                {
                    for (var i = 0; i < bufSize; i++)
                    {
                        br.Write((ushort)i);
                    }
                }
            }

            var outB = new byte[bufSize];

            VolumeRescaleConvert.BatchConvert(Nifti1Datatype.DT_SIGNED_SHORT, buffer, outB, 1, 1)(0, bufSize - 1);

            Assert.IsTrue(Enumerable.Range(0, bufSize).Zip(outB, (v1, v2) => v1 == v2 - 1).All(v => v));
        }

        /// <summary>
        /// Tests loading from a Nifti file in UInt16 voxel format, coming from the ITK Snap tool.
        /// </summary>
        [Test]
        public void LoadNiftiInUInt16Format()
        {
            var filename = TestData.GetFullImagesPath("vol_uint16.nii.gz");
            var niftiInUShort = MedIO.LoadNiftiInUShortFormat(filename);
            var distinctValues = niftiInUShort.Array.Distinct().ToArray();
            var expected = new ushort[] { 0, 1, 2 };
            Assert.AreEqual(expected, distinctValues, "The Nifti file contains 2 contours, should hence have 3 distinct values");
            var niftiCastToByte = MedIO.LoadNiftiAsByte(filename);
            var niftiCastToShort = MedIO.LoadNiftiAsShort(filename);
            Assert.AreEqual(niftiCastToByte.Length, niftiCastToShort.Length);
            Assert.AreEqual(niftiCastToByte.Length, niftiInUShort.Length);
            Assert.AreEqual(new ushort[] { 0, 1, 2 }, niftiCastToShort.Array.Distinct().ToArray());
            Assert.AreEqual(new byte[] { 0, 1, 2 }, niftiCastToByte.Array.Distinct().ToArray());
        }
    }
}
