///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System;
    using System.Linq;
    using Dicom;
    using MedLib.IO.Extensions;
    using MedLib.IO.Models;
    using MedLib.IO.Readers;
    using InnerEye.CreateDataset.Volumes;
    using NUnit.Framework;

    [Category("ReaderTests")]
    [TestFixture]
    public class DicomSeriesMetadataReaderTests
    {
        [Description("Tests creating a VolumeInformation object from a collection of DICOM datasets and check the expected values are correct and required exceptions are thrown.")]
        [Test]
        public void TestVolumeInformationCreateTest()
        {
            ushort highBit = 16;
            var expectedDimX = 54;
            var expectedDimY = 64;
            var expectedDimZ = 50;
            var expectedSpacingX = 3;
            var expectedSpacingY = 5;
            var expectedSpacingZ = 4;
            var expectedOrigin = new Point3D(1, 2, 3);

            var dicomDatasets = CreateValidDicomDatasetVolume(
                expectedDimX,
                expectedDimY,
                expectedDimZ,
                expectedSpacingX,
                expectedSpacingY,
                expectedSpacingZ,
                expectedOrigin,
                DicomUID.CTImageStorage,
                highBit);

            var volumeInformation = VolumeInformation.Create(dicomDatasets);

            Assert.AreEqual(expectedDimX, volumeInformation.Width);
            Assert.AreEqual(expectedDimY, volumeInformation.Height);
            Assert.AreEqual(expectedDimZ, volumeInformation.Depth);
            Assert.AreEqual(expectedSpacingX, volumeInformation.VoxelWidthInMillimeters);
            Assert.AreEqual(expectedSpacingY, volumeInformation.VoxelHeightInMillimeters);
            Assert.AreEqual(expectedSpacingZ, volumeInformation.VoxelDepthInMillimeters);
            Assert.AreEqual(0, volumeInformation.RescaleIntercept);
            Assert.AreEqual(1, volumeInformation.RescaleSlope);
            Assert.AreEqual(highBit, volumeInformation.HighBit);
            Assert.AreEqual(true, volumeInformation.SignedPixelRepresentation);
            Assert.AreEqual(expectedOrigin.X, volumeInformation.Origin.X);
            Assert.AreEqual(expectedOrigin.Y, volumeInformation.Origin.Y);
            Assert.AreEqual(expectedOrigin.Z, volumeInformation.Origin.Z);
            Assert.AreEqual(Matrix3.CreateIdentity(), volumeInformation.Direction);

            for (var i = 0; i < dicomDatasets.Length; i++)
            {
                Assert.AreEqual(i * expectedSpacingZ + expectedOrigin.Z, volumeInformation.GetSliceInformation(i).SlicePosition);
            }

            // Exception testing.
            dicomDatasets[dicomDatasets.Length - 1].AddOrUpdate(new DicomDecimalString(DicomTag.PixelSpacing, new decimal[] { 0, 0 }));
            Assert.Throws<ArgumentException>(() => VolumeInformation.Create(dicomDatasets.Take(1)));
            var sliceInformation = new SliceInformation[1];
            Assert.Throws<ArgumentException>(() => VolumeInformation.Create(sliceInformation));
            sliceInformation = null;
            Assert.Throws<ArgumentNullException>(() => VolumeInformation.Create(sliceInformation));
            dicomDatasets = null;
            Assert.Throws<ArgumentNullException>(() => VolumeInformation.Create(dicomDatasets));
        }

        [Description("Tests the validation logic of a volume information.")]
        [Test]
        public void TestVolumeInformationValidation()
        {
            var dicomDatasets = CreateValidDicomDatasetVolume(5, 5, 5, 1, 1, 3, new Point3D(), DicomUID.CTImageStorage, 16);
            var acceptanceTest = new NonStrictGeometricAcceptanceTest("Blah1", "Blah2");

            // Valid DICOM slice.
            DicomSeriesInformationValidator.ValidateVolumeInformation(
                VolumeInformation.Create(dicomDatasets),
                acceptanceTest,
                new[] { dicomDatasets[0].InternalTransferSyntax });

            // Inconsistent slice information.
            var dicomDatasets2 = CreateValidDicomDatasetVolume(5, 5, 5, 1, 1, 3, new Point3D(), DicomUID.CTImageStorage, 16);
            dicomDatasets2[dicomDatasets2.Length - 2].AddOrUpdate(new DicomUnsignedShort(DicomTag.Rows, 234));
            var exception = Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateVolumeInformation(
                VolumeInformation.Create(dicomDatasets2),
                acceptanceTest));
            Assert.IsTrue(exception.Message.Contains("Slice at position '9' has an inconsistent height. Expected: '5', Actual: '234'."));

            // Invalid supported transfer syntax
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateVolumeInformation(
                VolumeInformation.Create(dicomDatasets),
                acceptanceTest,
                new[] { DicomTransferSyntax.DeflatedExplicitVRLittleEndian }));

            // Failing acceptance test
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateVolumeInformation(
                VolumeInformation.Create(dicomDatasets),
                new FailingAcceptanceTest()));

            // Exception testing
            Assert.Throws<ArgumentNullException>(() => DicomSeriesInformationValidator.ValidateVolumeInformation(
                VolumeInformation.Create(dicomDatasets),
                null));

            Assert.Throws<ArgumentNullException>(() => DicomSeriesInformationValidator.ValidateVolumeInformation(
                null,
                acceptanceTest));
        }

        [Description("Tests creating a SliceInformation object from a DICOM dataset and check the expected values are correct and required exceptions are thrown.")]
        [Test]
        public void TestSliceInformationCreateTest()
        {
            ushort highBit = 16;
            var expectedDimX = 54;
            var expectedDimY = 64;
            var expectedSpacingX = 3;
            var expectedSpacingY = 5;
            var expectedOrigin = new Point3D(1, 2, 3);

            // Create a CT DICOM dataset.
            var dicomDataset = CreateDicomDatasetSlice(expectedDimX, expectedDimY, expectedSpacingX, expectedSpacingY, expectedOrigin, DicomUID.CTImageStorage, highBit);

            var sliceInformation = SliceInformation.Create(dicomDataset);

            Assert.AreEqual(expectedDimX, sliceInformation.Width);
            Assert.AreEqual(expectedDimY, sliceInformation.Height);
            Assert.AreEqual(expectedSpacingX, sliceInformation.VoxelWidthInMillimeters);
            Assert.AreEqual(expectedSpacingY, sliceInformation.VoxelHeightInMillimeters);
            Assert.AreEqual(3, sliceInformation.SlicePosition);
            Assert.AreEqual(0, sliceInformation.RescaleIntercept);
            Assert.AreEqual(1, sliceInformation.RescaleSlope);
            Assert.AreEqual(highBit, sliceInformation.HighBit);
            Assert.AreEqual(true, sliceInformation.SignedPixelRepresentation);
            Assert.AreEqual(DicomUID.CTImageStorage, sliceInformation.SopClass);
            Assert.AreEqual(expectedOrigin.X, sliceInformation.Origin.X);
            Assert.AreEqual(expectedOrigin.Y, sliceInformation.Origin.Y);
            Assert.AreEqual(expectedOrigin.Z, sliceInformation.Origin.Z);
            Assert.AreEqual(Matrix3.CreateIdentity(), sliceInformation.Direction);
            Assert.AreEqual(dicomDataset, sliceInformation.DicomDataset);

            // Test null argument exception
            Assert.Throws<ArgumentNullException>(() => SliceInformation.Create(null));

            // Remove the columns property
            dicomDataset.AddOrUpdate(new DicomDecimalString(DicomTag.PixelSpacing, expectedSpacingY, expectedSpacingX));
            dicomDataset.Remove(DicomTag.Columns);
            Assert.Throws<ArgumentException>(() => SliceInformation.Create(dicomDataset));
        }

        [Description("Tests the validation logic of a single DICOM slice information.")]
        [Test]
        public void TestSliceInformationValidation()
        {
            ushort highBit = 15;
            var dicomDataset = CreateValidDicomDatasetSlice(5, 5, 1, 1, new Point3D(), DicomUID.CTImageStorage, highBit);

            // Valid DICOM slice.
            DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset), new[] { dicomDataset.InternalTransferSyntax });

            // Invalid supported transfer syntax
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset), new[] { DicomTransferSyntax.DeflatedExplicitVRLittleEndian }));

            // Add LUT Sequence
            dicomDataset.Add(new DicomSequence(DicomTag.ModalityLUTSequence, new DicomDataset[0]));
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset)));

            // Remove LUT Sequence and set bits allocated to not 16
            dicomDataset.Remove(DicomTag.ModalityLUTSequence);
            DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset));
            dicomDataset.AddOrUpdate(new DicomUnsignedShort(DicomTag.BitsAllocated, 12));
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset)));

            // Set bits allocated to 16, and updated photometric interpation to not MONOCHROME2
            dicomDataset.AddOrUpdate(new DicomUnsignedShort(DicomTag.BitsAllocated, 16));
            DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset));
            dicomDataset.AddOrUpdate(new DicomCodeString(DicomTag.PhotometricInterpretation, "INVALID"));
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset)));

            // Set photometric interpation to MONOCHROME2 and change expected samples per pixel
            dicomDataset.AddOrUpdate(new DicomCodeString(DicomTag.PhotometricInterpretation, DicomSeriesInformationValidator.ExpectedPhotometricInterpretation));
            DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset));
            dicomDataset.AddOrUpdate(new DicomUnsignedShort(DicomTag.SamplesPerPixel, DicomSeriesInformationValidator.ExpectedSamplesPerPixel + 1));
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset)));

            // Set samples per pixel to 1 and change the modality to not CT
            dicomDataset.AddOrUpdate(new DicomUnsignedShort(DicomTag.SamplesPerPixel, DicomSeriesInformationValidator.ExpectedSamplesPerPixel));
            DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset));
            dicomDataset.AddOrUpdate(DicomTag.Modality, DicomConstants.MRModality);
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset)));

            // Set modality to CT and change the bits stored to highbit + 2
            dicomDataset.AddOrUpdate(DicomTag.Modality, DicomConstants.CTModality);
            DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset));
            dicomDataset.AddOrUpdate(new DicomUnsignedShort(DicomTag.BitsStored, (ushort)(highBit + 2)));
            Assert.Throws<ArgumentException>(() => DicomSeriesInformationValidator.ValidateSliceInformation(SliceInformation.Create(dicomDataset)));
        }

        private static DicomDataset[] CreateValidDicomDatasetVolume(
           int dimX,
           int dimY,
           int dimZ,
           int spacingX,
           int spacingY,
           int spacingZ,
           Point3D origin,
           DicomUID sopClass,
           ushort highBit)
        {
            var dicomDatasets = new DicomDataset[dimZ];
            for (var i = 0; i < dicomDatasets.Length; i++)
            {
                dicomDatasets[i] = CreateValidDicomDatasetSlice(
                    dimX,
                    dimY,
                    spacingX,
                    spacingY,
                    new Point3D(origin.X, origin.Y, origin.Z + (i * spacingZ)),
                    sopClass,
                    highBit);
            }

            return dicomDatasets;
        }

        private static DicomDataset CreateValidDicomDatasetSlice(
           int dimX,
           int dimY,
           int spacingX,
           int spacingY,
           Point3D origin,
           DicomUID sopClass,
           ushort highBit)
        {
            var dicomDataset = CreateDicomDatasetSlice(dimX, dimY, spacingX, spacingY, origin, sopClass, highBit);
            dicomDataset.Add(new DicomUnsignedShort(DicomTag.BitsAllocated, DicomSeriesInformationValidator.ExpectedBitsAllocated));
            dicomDataset.Add(new DicomCodeString(DicomTag.PhotometricInterpretation, DicomSeriesInformationValidator.ExpectedPhotometricInterpretation));
            dicomDataset.Add(new DicomUnsignedShort(DicomTag.SamplesPerPixel, DicomSeriesInformationValidator.ExpectedSamplesPerPixel));
            dicomDataset.Add(DicomTag.Modality, DicomConstants.CTModality);
            dicomDataset.Add(new DicomUnsignedShort(DicomTag.BitsStored, (ushort)(highBit + 1)));
            return dicomDataset;
        }

        /// <summary>
        /// Creates a CT slice with the minimum required information (this would not pass validation).
        /// </summary>
        /// <param name="dimX"></param>
        /// <param name="dimY"></param>
        /// <param name="spacingX"></param>
        /// <param name="spacingY"></param>
        /// <param name="origin"></param>
        /// <param name="sopClass"></param>
        /// <param name="bitsStored"></param>
        /// <returns></returns>
        private static DicomDataset CreateDicomDatasetSlice(
            int dimX,
            int dimY,
            int spacingX,
            int spacingY,
            Point3D origin,
            DicomUID sopClass,
            ushort highBit)
        {
            return new DicomDataset
            {
                { DicomTag.SOPClassUID, sopClass },
                { new DicomUnsignedShort(DicomTag.PixelRepresentation, 1) },
                { new DicomUnsignedShort(DicomTag.HighBit, highBit) },
                { new DicomUnsignedShort(DicomTag.Columns, (ushort)dimX) },
                { new DicomUnsignedShort(DicomTag.Rows, (ushort)dimY) },
                { new DicomDecimalString(DicomTag.PixelSpacing, spacingY, spacingX) },
                { new DicomDecimalString(DicomTag.ImagePositionPatient, (decimal)origin.X, (decimal)origin.Y, (decimal)origin.Z) },
                { new DicomDecimalString(DicomTag.RescaleIntercept, 0) },
                { new DicomDecimalString(DicomTag.RescaleSlope, 1) },
                { new DicomDecimalString(DicomTag.ImageOrientationPatient, 1, 0, 0, 0, 1, 0) },
            };
        }

        protected class FailingAcceptanceTest : IVolumeGeometricAcceptanceTest
        {
            public bool AcceptPositionError(DicomUID sopClassUid, Point3D actualCoordinate, Point3D volumeCoordinate)
                => false;

            public bool AcceptSliceSpacingError(DicomUID sopClassUid, double sliceGap, double medianSliceGap)
                => false;

            public bool Propose(DicomUID sopClassUid, Point3D volumeOrigin, Matrix3 iop, Point3D voxelDims, out string reason)
            {
                reason = "We always fail";
                return false;
            }
        }
    }
}