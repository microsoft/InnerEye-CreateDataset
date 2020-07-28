///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Dicom;
    using InnerEye.CreateDataset.Common;
    using InnerEye.CreateDataset.TestHelpers;
    using InnerEye.CreateDataset.Contours;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;
    using NUnit.Framework;

    /// <summary>
    /// Contains tests to ensure that we can round trip from (VolumeShort, Set of binary masks) to Dicom
    /// to MedicalVolume and back.
    /// </summary>
    [TestFixture]
    public class VolumeToDicomTests
    {
        /// <summary>
        /// Tests whether a scan and a set of binary masks can be written to Dicom, and read back in again.
        /// </summary>
        [Test]
        public void VolumeToDicomAndBack()
        {
            var outputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "DicomOutput");
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
                Thread.Sleep(1000);
            }
            Directory.CreateDirectory(outputFolder);
            var scan = new Volume3D<short>(5, 5, 5);
            foreach (var index in scan.Array.Indices())
            {
                scan.Array[index] = (short)index;
            }
            // Create 3 structures, each with a different color
            var masks = new List<ContourRenderingInformation>();
            var colors = new[]
            {
                new RGBValue(255, 0, 0),
                new RGBValue(0, 255, 0),
                new RGBValue(0, 0, 255),
            };
            foreach (var index in Enumerable.Range(0, 3))
            {
                var mask = scan.CreateSameSize<byte>();
                mask[index+1, index+1, index+1] = 1;
                masks.Add(new ContourRenderingInformation($"structure_{index}", colors[index], mask));
            }
            var seriesDescription = "description";
            var patientId = DicomUID.Generate().UID;
            var studyId = DicomUID.Generate().UID;
            var dicomFiles = NiiToDicomHelpers.ScanAndContoursToDicom(scan, ImageModality.CT, masks, 
                seriesDescription, patientId, studyId);
            // Write to disk, so that we can load it into the App as well
            var dicomFilesOnDisk = new List<string>();
            foreach (var dicomFile in dicomFiles)
            {
                dicomFilesOnDisk.Add(dicomFile.SaveToFolder(outputFolder));
            }
            // Test if the first returned Dicom file is really the RTStruct
            var rtStructFromFile = RtStructReader.LoadContours(dicomFilesOnDisk[0], scan.Transform.DicomToData);
            Assert.IsNotNull(rtStructFromFile);
            Assert.AreEqual(masks.Count, rtStructFromFile.Item1.Contours.Count);
            var fromDisk = NiiToDicomHelpers.MedicalVolumeFromDicomFolder(outputFolder);
            VolumeAssert.AssertVolumesMatch(scan, fromDisk.Volume, "Loaded scan does not match");
            Assert.AreEqual(seriesDescription, fromDisk.Identifiers.First().Series.SeriesDescription);
            Assert.AreEqual(patientId, fromDisk.Identifiers.First().Patient.Id);
            Assert.AreEqual(studyId, fromDisk.Identifiers.First().Study.StudyInstanceUid);
            foreach (var index in Enumerable.Range(0, fromDisk.Struct.Contours.Count))
            {
                var loadedMask = fromDisk.Struct.Contours[index].Contours.ToVolume3D(scan);
                VolumeAssert.AssertVolumesMatch(masks[index].Contour.ToVolume3D(scan), loadedMask, $"Loaded mask {index}");
                Assert.AreEqual(masks[index].Name, fromDisk.Struct.Contours[index].StructureSetRoi.RoiName, $"Loaded mask name {index}");
            }

            // Now test if we can ZIP up all the Dicom files, and read them back in. 
            var zippedDicom = ZippedDicom.DicomFilesToZipArchive(dicomFiles);
            var dicomFromZip = ZippedDicom.DicomFilesFromZipArchive(zippedDicom);
            var volumeFromZip = NiiToDicomHelpers.MedicalVolumeFromDicom(dicomFromZip.ToList());
            VolumeAssert.AssertVolumesMatch(scan, volumeFromZip.Volume, "Scan from Zip archive does not match");
            Assert.AreEqual(masks.Count, volumeFromZip.Struct.Contours.Count, "RtStructs from Zip do not match");
        }

        /// <summary>
        /// Tests whether the mask-to-contour-to-mask conversion checks work as expected.
        /// </summary>
        /// <param name="trueForeground"></param>
        /// <param name="renderedForeground"></param>
        /// <param name="maxAbsoluteDifference"></param>
        /// <param name="maxRelativeDifference"></param>
        /// <param name="isAccepted">If true, the check method is expected to succeed with the above parameters.</param>
        [Test]
        [TestCase(0, 0, 0, 0.0, true)]
        // No difference, no slack allowed.
        [TestCase(10, 10, 0, 0.0, true)]
        // values are equal, with either relative or absolute slack
        [TestCase(10, 10, 1, 0.0, true)]
        [TestCase(10, 10, 0, 0.1, true)]
        // Values within the allowed max difference. The allowed relative difference is 0,
        // to confirm that in this case only the absolute difference is checked.
        [TestCase(10, 11, 1, 0.0, true)]
        // Values outside the max allowed difference, and no relative difference allowed.
        [TestCase(10, 12, 1, 0.0, false)]
        // Allowed absolute difference is exceeded, and also relative difference exceeded.
        [TestCase(100, 110, 1, 0.05, false)]
        // Allowed absolute difference is exceeded, but relative difference within bounds.
        [TestCase(100, 110, 1, 0.2, true)]
        public void MaskToContourCheck(int trueForeground, 
            int renderedForeground, 
            int? maxAbsoluteDifference, 
            double? maxRelativeDifference, 
            bool isAccepted)
        {
            if (isAccepted)
            {
                NiiToDicomHelpers.CheckContourRendering(trueForeground, renderedForeground, 
                    maxAbsoluteDifference, maxRelativeDifference, string.Empty);
            }
            else
            {
                var messagePrefix = "foo";
                var exception = Assert.Throws<InvalidOperationException>(() => 
                    NiiToDicomHelpers.CheckContourRendering(trueForeground, renderedForeground, 
                    maxAbsoluteDifference, maxRelativeDifference, messagePrefix));
                Assert.IsTrue(exception.Message.Contains(messagePrefix));
            }
        }

        [Test]
        public void SanitizeDicomLongStringTest()
        {
            var testCases = new List<(bool IsValidAlready, string Text, string Expected)>
            {
                (false, "12\\34", "12/34"),
                (true, "abcd/efg", "abcd/efg"),
                (true, null, null),
                (false, new string('a', NiiToDicomHelpers.MaxDicomLongStringLength * 2), new string('a', NiiToDicomHelpers.MaxDicomLongStringLength))
            };
            foreach (var (isValidAlready, text, expected) in testCases)
            {
                Assert.AreEqual(isValidAlready, NiiToDicomHelpers.IsValidDicomLongString(text), $"Validity check before conversion on {text}");
                var actual = NiiToDicomHelpers.SanitizeDicomLongString(text);
                Assert.IsTrue(NiiToDicomHelpers.IsValidDicomLongString(actual), $"Should be valid after conversion on {text}");
                Assert.AreEqual(expected, actual, "Conversion result does not match expected");
            }
        }
    }
}
