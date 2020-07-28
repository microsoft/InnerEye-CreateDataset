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
    using Dicom;
    using MedLib.IO.Extensions;
    using MedLib.IO.Models;
    using MedLib.IO.Readers;
    using MedLib.IO.RT;
    using MedLib.IO.Writers;
    using InnerEye.CreateDataset.Contours;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;
    using Models.DicomRt;
    using NUnit.Framework;

    [TestFixture]
    public class TestDicomRTTest
    {
        private string _tempfile;

        private string _tempFolder;

        [SetUp]
        public void SetUp()
        {
            _tempfile = Path.GetTempFileName() + ".dcm";
            _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempfile))
            {
                File.Delete(_tempfile);
            }

            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, recursive: true);
            }
        }

        [Description("Loads an image and checks to the rastered contour volumes")]
        [Test]
        public void LoadImageAndContours()
        {
            var acceptanceTests = new NonStrictGeometricAcceptanceTest(string.Empty, string.Empty);
            var medicalVolume = MedIO.LoadAllDicomSeriesInFolderAsync(TestData.GetFullImagesPath(@"sample_dicom"), acceptanceTests).Result.First().Volume;

            var volumes = new Dictionary<string, ContourStatistics>()
            {
                { "Bladder", new ContourStatistics(95.87, -1000.0, 0) },
                { "Femur_L", new ContourStatistics(205.04, -1000.0, 0) },
            };

            var readOnlyVolume = new InnerEye.CreateDataset.Volumes.ReadOnlyVolume3D<short>(medicalVolume.Volume);

            for (int i = 0; i < medicalVolume.Struct.Contours.Count; i++)
            {
                var contour = medicalVolume.Struct.Contours[i];
                var name = contour.StructureSetRoi.RoiName;
                var contourVolume = contour.Contours.ToVolume3D(medicalVolume.Volume);
                Console.WriteLine($"Starting checks for contour {name}");
                var contourStatistics = ContourStatistics.FromVolumeAndMask(readOnlyVolume, contourVolume);
                Assert.AreEqual(volumes[contour.StructureSetRoi.RoiName].SizeInCubicCentimeters, contourStatistics.SizeInCubicCentimeters, 1e-1, $"Size for contour {name}");
                Assert.AreEqual(volumes[contour.StructureSetRoi.RoiName].VoxelValueMean, contourStatistics.VoxelValueMean, 1e-1, $"Mean for contour {name}");
                Assert.AreEqual(volumes[contour.StructureSetRoi.RoiName].VoxelValueStandardDeviation, contourStatistics.VoxelValueStandardDeviation, 1e-1, $"Std for contour {name}");
            }
        }

        [Test]
        [Description("This needs refactoring to do something useful - at the moment it looks like it is carefully comparing a set of contours with itself.")]
        public void LoadAndSaveMedicalVolumeTest()
        {
            var directory = TestData.GetFullImagesPath("sample_dicom");
            var acceptanceTests = new StrictGeometricAcceptanceTest(string.Empty, string.Empty); 
            var medicalVolume = MedIO.LoadAllDicomSeriesInFolderAsync(directory, acceptanceTests).Result.First().Volume;

            Directory.CreateDirectory(_tempFolder);
            Console.WriteLine($"Directory created {_tempFolder}");

            Volume3D<byte>[] contourVolumes = new Volume3D<byte>[medicalVolume.Struct.Contours.Count];

            for (int i = 0; i < medicalVolume.Struct.Contours.Count; i++)
            {
                contourVolumes[i] = new Volume3D<byte>(
                    medicalVolume.Volume.DimX,
                    medicalVolume.Volume.DimY,
                    medicalVolume.Volume.DimZ,
                    medicalVolume.Volume.SpacingX,
                    medicalVolume.Volume.SpacingY,
                    medicalVolume.Volume.SpacingZ,
                    medicalVolume.Volume.Origin,
                    medicalVolume.Volume.Direction);

                contourVolumes[i].Fill(medicalVolume.Struct.Contours[i].Contours, (byte)1);
            }

            // Calculate contours based on masks
            var rtContours = new List<RadiotherapyContour>();

            for (int i = 0; i < medicalVolume.Struct.Contours.Count; i++)
            {
                var contour = medicalVolume.Struct.Contours[i];

                var contourForAllSlices = GetContoursForAllSlices(contourVolumes[i]);
                var rtcontour = new DicomRTContour(
               contour.DicomRtContour.ReferencedRoiNumber,
               contour.DicomRtContour.RGBColor,
               contourForAllSlices);
                DicomRTStructureSetROI rtROIstructure = new DicomRTStructureSetROI(
                    contour.StructureSetRoi.RoiNumber,
                    contour.StructureSetRoi.RoiName,
                    string.Empty,
                    ERoiGenerationAlgorithm.Semiautomatic);
                DicomRTObservation observation = new DicomRTObservation(
                    contour.DicomRtObservation.ReferencedRoiNumber, new DicomPersonNameConverter("Left^Richard^^Dr"), ROIInterpretedType.EXTERNAL);

                rtContours.Add(new RadiotherapyContour(rtcontour, rtROIstructure, observation));
            }

            var rtStructureSet = new RadiotherapyStruct(
                medicalVolume.Struct.StructureSet,
                medicalVolume.Struct.Patient,
                medicalVolume.Struct.Equipment,
                medicalVolume.Struct.Study,
                medicalVolume.Struct.RTSeries,
                rtContours);

            MedIO.SaveMedicalVolumeAsync(_tempFolder, new MedicalVolume(
                medicalVolume.Volume,
                medicalVolume.Identifiers,
                medicalVolume.FilePaths,
                rtStructureSet)).Wait();

            var medicalVolume2 = MedIO.LoadAllDicomSeriesInFolderAsync(_tempFolder, acceptanceTests).Result.First().Volume;

            foreach (var radiotherapyContour in medicalVolume2.Struct.Contours.Where(x => x.DicomRtContour.DicomRtContourItems.First().GeometricType == "CLOSED_PLANAR"))
            {
                var savedContour =
                    medicalVolume2.Struct.Contours.First(x => x.StructureSetRoi.RoiName == radiotherapyContour.StructureSetRoi.RoiName);

                foreach (var contour in radiotherapyContour.Contours)
                {
                    Assert.AreEqual(radiotherapyContour.DicomRtObservation.ROIInterpretedType, ROIInterpretedType.EXTERNAL);
                    for (int i = 0; i < contour.Value.Count; i++)
                    {
                        if (!contour.Value[i].Equals(savedContour.Contours.ContoursForSlice(contour.Key)[i]))
                        {
                            Console.WriteLine(radiotherapyContour.StructureSetRoi.RoiName);
                            Assert.Fail();
                        }
                    }
                }
            }
        }

        private static List<DicomRTContourItem> GetContoursForAllSlices(Volume3D<byte> segmentationMask)
        {
            var listOfPointsPerSlice = segmentationMask.ContoursWithHolesPerSlice();

            var resultList = new List<DicomRTContourItem>();

            foreach (var sliceContours in listOfPointsPerSlice)
            {
                foreach (var contour in sliceContours.Value)
                {
                    var dataToDicom = segmentationMask.Transform.DataToDicom;

                    var allpoints = contour.ContourPoints.SelectMany(
                        p => (dataToDicom * new InnerEye.CreateDataset.Volumes.Point3D(p.X, p.Y, sliceContours.Key)).Data).ToArray();

                    if (allpoints.Length > 0)
                    {
                        var contourImageSeq = new List<DicomRTContourImageItem>();
                        var dicomContour = new DicomRTContourItem(
                            allpoints,
                            contour.Length,
                            DicomExtensions.ClosedPlanarString,
                            contourImageSeq);
                        resultList.Add(dicomContour);
                    }
                }
            }

            return resultList;
        }

        [Description("Loads volume and struct with an expected name, saves the struct and reloads checks the name is the same")]
        [Test]
        public void LoadMedicalVolumeTest()
        {
            var dir = TestData.GetFullImagesPath(@"sample_dicom\");
            var acceptanceTests = new NonStrictGeometricAcceptanceTest(string.Empty, string.Empty);
            var medicalVolumes = MedIO.LoadAllDicomSeriesInFolderAsync(dir, acceptanceTests).Result;
            Assert.AreEqual(1, medicalVolumes.Count);

            var medicalVolume = medicalVolumes.First().Volume;

            Assert.IsTrue(medicalVolume.Struct.Contours.Any(x => x.StructureSetRoi.RoiName == "Bladder"));
            Assert.IsTrue(medicalVolume.Struct.Contours.Any(x => x.StructureSetRoi.RoiName == "Femur_L"));

            RtStructWriter.SaveRtStruct(_tempfile, medicalVolume.Struct);

            DicomFile file = DicomFile.Open(_tempfile);

            var identifiers = medicalVolume.Identifiers.First(); 

            var reloaded = RtStructReader.LoadContours(
                file.Dataset,
                medicalVolume.Volume.Transform.DicomToData,
                identifiers.Series.SeriesInstanceUid,
                identifiers.Study.StudyInstanceUid).Item1;
            Assert.IsTrue(reloaded.Contours.Any(x => x.StructureSetRoi.RoiName == "Bladder"));
            Assert.IsTrue(reloaded.Contours.Any(x => x.StructureSetRoi.RoiName == "Femur_L"));
        }
    }
}
