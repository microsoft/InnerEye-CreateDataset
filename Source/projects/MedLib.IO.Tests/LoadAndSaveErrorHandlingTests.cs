///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using System.IO;
using InnerEye.CreateDataset.Volumes;
using NUnit.Framework;

namespace MedLib.IO.Tests
{
    [TestFixture]
    public class LoadAndSaveErrorHandlingTests
    {
        private readonly Volume3D<byte> image = new Volume3D<byte>(2, 2, 2, 1.0, 1.0, 1.0);

        /// When opening an invalid or non-existent file, an exception should be
        /// thrown that contains the path of the offending file.
        [Test]
        [TestCase("C:\\somegarbage.nii")]
        [TestCase("C:\\somegarbage.nii.gz")]
        public void LoadingInvalidFile(string file)
        {
            var ex = Assert.Catch(() => MedIO.LoadNiftiAsFloat(file), "Loading a non-existent file should fail");
            Assert.IsTrue(ex.Message.Contains(file));
            Assert.IsTrue(ex.Message.Contains("Could not find file"));
        }

        /// <summary>
        /// Reading Nifti files expects a wellformed filename with extension.
        /// </summary>
        [Test]
        public void LoadingInvalidExtensions()
        {
            const string validExtension = ".nii";
            const string invalidExtension = ".ni";

            var basePath = Path.GetTempFileName();
            var validPath = basePath + validExtension;
            var invalidPath = basePath + invalidExtension;

            // Can't save with invalid extension, so save with valid extension and rename.
            MedIO.SaveNifti(image, validPath);
            File.Move(validPath, invalidPath);


            var ex = Assert.Catch(() => MedIO.LoadNiftiAsFloat(invalidPath));
            Assert.IsTrue(ex.Message.Contains("filenames must end with"));
            Assert.IsTrue(ex.Message.Contains(invalidPath));
        }

        /// When writing to a path that is invalid, an exception should be
        /// thrown that contains the path of the offending file.
        [Test]
        public void WritingInvalidFile()
        {
            var file = "XYZ:/somegarbage.nii.gz";
            var ex = Assert.Catch(() => MedIO.SaveNifti(image, file), "Writing to a nonsensical path should fail");
            Assert.IsTrue(ex.Message.Contains(file));
            Assert.IsTrue(ex.Message.Contains("writing to file"));
        }


        /// <summary>
        /// Writing to Nifti files expects a wellformed filename with extension.
        /// </summary>
        [Test]
        public void WritingInvalidExtension()
        {
            var file = @"C:\temp\nonsense.ni";
            var ex = Assert.Catch(() => MedIO.SaveNifti(image, file));
            Assert.IsTrue(ex.Message.Contains("filenames must end with"));
            Assert.IsTrue(ex.Message.Contains(file));
        }

        /// <summary>
        /// Verify that writing to files that have any of the Nifti extensions works.
        /// Reading back in and verifying the contents is the same.
        /// </summary>
        [TestCase(".nii")]
        [TestCase(".nii.gz")]
        [Test]
        public void WritingToFileAndReading(string extension)
        {
            var file = Path.GetTempFileName() + extension;
            MedIO.SaveNifti(image, file);
            var image2 = MedIO.LoadNiftiAsByte(file);
            Assert.AreEqual(image.DimX, image2.DimX);
            Assert.AreEqual(image.DimY, image2.DimY);
            Assert.AreEqual(image.DimZ, image2.DimZ);
            File.Delete(file);
        }

        private string GetTempNiftiFileName()
        {
            return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".nii.gz");
        }

        [TestCase]
        public void ReadniftiAndSave()
        {
            var tempRandomNiftyFilePath = GetTempNiftiFileName();

            var medimage3D = MedIO.LoadNiftiAsFloat(TestData.GetFullImagesPath(@"vol_int16.nii.gz"));

            Assert.IsNotNull(medimage3D);
            Assert.AreEqual(3, medimage3D.Dimensions);

            MedIO.SaveNifti(medimage3D, tempRandomNiftyFilePath);

            var savedMedimage3D = MedIO.LoadNiftiAsFloat(tempRandomNiftyFilePath);

            Assert.AreEqual(medimage3D.Origin, savedMedimage3D.Origin);
            Assert.AreEqual(medimage3D.Array, savedMedimage3D.Array);
        }

    }
}
