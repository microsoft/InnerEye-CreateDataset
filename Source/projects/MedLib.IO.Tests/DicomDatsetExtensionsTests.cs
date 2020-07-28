///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Dicom;
    using Dicom.Imaging;
    using Dicom.IO.Buffer;

    using InnerEye.CreateDataset.Volumes;

    using NUnit.Framework;

    [TestFixture]
    public class DicomDatsetExtensionsTests
    {
        [Description("Tests all the Dicom dataset extensions.")]
        [Test]
        public async Task TestAllExtensions()
        {
            var prostateFolder = TestData.GetFullImagesPath("sample_dicom");
            var files = Directory.EnumerateFiles(prostateFolder).ToList();

            var dicomFiles = new DicomFile[files.Count];

            for (var i = 0; i < files.Count; i++)
            {
                dicomFiles[i] = await DicomFile.OpenAsync(files[i]);
            }

            var (width, height) = dicomFiles[0].Dataset.GetSliceSize();
            var origin = dicomFiles[0].Dataset.GetOrigin();
            var (spacingX, spacingY) = dicomFiles[0].Dataset.GetPixelSpacings();
            var direction = dicomFiles[0].Dataset.GetDirectionalMatrix();
            var rescaleIntercept = dicomFiles[0].Dataset.GetRescaleIntercept();
            var rescaleSlope = dicomFiles[0].Dataset.GetRescaleSlope();
            var isSignedPixelRepresentation = dicomFiles[0].Dataset.IsSignedPixelRepresentation();
            var highBit = dicomFiles[0].Dataset.GetHighBit();
            var sopClass = dicomFiles[0].Dataset.GetSopClass();

            Assert.AreEqual(512, width);
            Assert.AreEqual(512, height);

            Assert.AreEqual(-250, origin.X);
            Assert.AreEqual(-250, origin.Y);
            Assert.AreEqual(125.5, origin.Z);

            Assert.AreEqual(0.9765625, spacingX);
            Assert.AreEqual(0.9765625, spacingY);

            Assert.AreEqual(1, direction.Data[0]);
            Assert.AreEqual(0, direction.Data[1]);
            Assert.AreEqual(0, direction.Data[2]);
            Assert.AreEqual(0, direction.Data[3]);
            Assert.AreEqual(1, direction.Data[4]);
            Assert.AreEqual(0, direction.Data[5]);
            Assert.AreEqual(0, direction.Data[6]);
            Assert.AreEqual(0, direction.Data[7]);
            Assert.AreEqual(1, direction.Data[8]);

            Assert.AreEqual(0, rescaleIntercept);
            Assert.AreEqual(1, rescaleSlope);
            Assert.AreEqual(true, isSignedPixelRepresentation);
            Assert.AreEqual(15, highBit);
            Assert.AreEqual(DicomUID.CTImageStorage, sopClass);
        }
    }
}