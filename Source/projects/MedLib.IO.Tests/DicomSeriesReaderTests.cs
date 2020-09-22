///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Dicom;

    using MedLib.IO.Readers;

    using NUnit.Framework;

    [Category("ReaderTests")]
    [TestFixture]
    public class DicomSeriesReaderTests
    {
        [Description("Tests opening an image from a collection of DICOM datasets including validating the input and extracting voxels.")]
        [Test]
        public async Task TestImageOpening()
        {
            var prostateFolder = TestData.GetFullImagesPath("sample_dicom");
            var files = Directory.EnumerateFiles(prostateFolder)
                .Where(x => !x.EndsWith("rtstruct_mod.dcm")).ToList();

            var dicomFiles = new DicomFile[files.Count];

            for (var i = 0; i < files.Count; i++)
            {
                dicomFiles[i] = await DicomFile.OpenAsync(files[i]);
            }

            var volume = DicomSeriesReader.BuildVolume(
                                dicomFiles.Select(x => x.Dataset),
                                new NonStrictGeometricAcceptanceTest(string.Empty, string.Empty), true);

            Assert.AreEqual(512, volume.DimX);
            Assert.AreEqual(262144, volume.DimXY);
            Assert.AreEqual(512, volume.DimY);
            Assert.AreEqual(2, volume.DimZ);
            Assert.AreEqual(3, volume.Dimensions);
            Assert.AreEqual(524288, volume.Length);
            Assert.AreEqual(-250d, volume.Origin.X);
            Assert.AreEqual(-250d, volume.Origin.Y);
            Assert.AreEqual(125.5d, volume.Origin.Z);
            Assert.AreEqual(0.9765625, volume.SpacingX);
            Assert.AreEqual(0.9765625, volume.SpacingY);
            Assert.AreEqual(2.86102294921875, volume.VoxelVolume);
            Assert.AreEqual(-1000, volume.Array[3453]);
            Assert.AreEqual(-1000, volume.Array[8453]);
            Assert.AreEqual(-1000, volume.Array[10453]);
            Assert.AreEqual(-1000, volume.Array[100453]);
        }
    }
}