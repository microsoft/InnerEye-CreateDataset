///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math.Tests.Morphology
{
    using System.Collections.Generic;
    using System.Linq;
    using MedLib.IO;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;
    using NUnit.Framework;
    using System.IO;
    using System;

    [TestFixture]
    public class MorphologicalTests
    {
        private static string BaseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"Morphology\TestData\Structures");

        [Test]
        public void IntersectTest()
        {
            var structure1 = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure1.nii.gz");
            var structure2 = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure2.nii.gz");

            var volume = MedIO.LoadNiftiAsShort(BaseFolder + @"\ParentVolume.nii.gz");

            var structure1Contour = structure1.ContoursWithHolesPerSlice();
            var structure2Contour = structure2.ContoursWithHolesPerSlice();

            var volumeResult = structure1Contour.GeometryIntersect(structure2Contour, volume);

            var actualVolumeResult = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure1IntersectStructure2.nii.gz");

            Assert.AreEqual(volumeResult.Length, actualVolumeResult.Length);

            for (var i = 0; i < actualVolumeResult.Length; i++)
            {
                Assert.AreEqual(volumeResult[i], actualVolumeResult[i]);
            }
        }

        [Test]
        public void MinusTest()
        {
            var structure1 = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure1.nii.gz");
            var structure2 = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure2.nii.gz");

            var volume = MedIO.LoadNiftiAsShort(BaseFolder + @"\ParentVolume.nii.gz");

            var structure1Contour = structure1.ContoursWithHolesPerSlice();
            var structure2Contour = structure2.ContoursWithHolesPerSlice();

            var volumeResult = structure1Contour.GeometryExclude(structure2Contour, volume);

            var actualVolumeResult = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure1MinusStructure2.nii.gz");

            Assert.AreEqual(volumeResult.Length, actualVolumeResult.Length);

            for (var i = 0; i < actualVolumeResult.Length; i++)
            {
                Assert.AreEqual(volumeResult[i], actualVolumeResult[i]);
            }
        }

        [Test]
        public void UnionTest()
        {
            var structure1 = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure1.nii.gz");
            var structure2 = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure2.nii.gz");

            var volume = MedIO.LoadNiftiAsShort(BaseFolder + @"\ParentVolume.nii.gz");

            var structure1Contour = structure1.ContoursWithHolesPerSlice();
            var structure2Contour = structure2.ContoursWithHolesPerSlice();

            var volumeResult = structure1Contour.GeometryUnion(structure2Contour, volume);

            var actualVolumeResult = MedIO.LoadNiftiAsByte(BaseFolder + @"\Structure1UnionStructure2.nii.gz");

            Assert.AreEqual(volumeResult.Length, actualVolumeResult.Length);

            for (var i = 0; i < actualVolumeResult.Length; i++)
            {
                Assert.AreEqual(volumeResult[i], actualVolumeResult[i]);
            }
        }

        [Test]
        public void SurfacePointExtractionTest()
        {
            // check empty volume has no surface points
            Assert.IsFalse(ExtractSurfacePoints(new Volume3D<byte>(1, 1, 1)).Any());

            // check that a single voxel volume has a trivial surface point 
            Assert.IsTrue(ExtractSurfacePoints(new Volume3D<byte>(new byte[] { 1 }, 1, 1, 1, 1, 1, 1)).Count() == 1);

            // check that all boundary points are considered as surface points
            var volumeWithOnlyBoundaryAsFG = new Volume3D<byte>(3, 3, 3);
            var expectedForegroundEdgePoints = new List<(int x, int y, int z)>();
            volumeWithOnlyBoundaryAsFG.IterateSlices(p =>
            {
                if (volumeWithOnlyBoundaryAsFG.IsEdgeVoxel(p.x, p.y, p.z))
                {
                    volumeWithOnlyBoundaryAsFG[p.x, p.y, p.z] = Volumes.ModelConstants.MaskForegroundIntensity;
                    expectedForegroundEdgePoints.Add(p);
                }
            });
            CollectionAssert.AreEqual(expectedForegroundEdgePoints, ExtractSurfacePoints(volumeWithOnlyBoundaryAsFG));

            // check that only voxels that contain a BG voxel in their 1-connectivity neighborhood are considered as surface points
            var volumeWithNonSurfacePointVoxels = new Volume3D<byte>(5, 5, 5);
            var volumeWithNonSurfacePointVoxelsCenter = (x: volumeWithNonSurfacePointVoxels.DimX / 2, y: volumeWithNonSurfacePointVoxels.DimY / 2, z: volumeWithNonSurfacePointVoxels.DimZ / 2);
            var expectedNonEdgeSurfacePoints = new List<(int x, int y, int z)>();

            volumeWithNonSurfacePointVoxels.IterateSlices(p =>
            {
                if (!volumeWithNonSurfacePointVoxels.IsEdgeVoxel(p.x, p.y, p.z))
                {
                    volumeWithNonSurfacePointVoxels[p.x, p.y, p.z] = Volumes.ModelConstants.MaskForegroundIntensity;
                    // the center voxel in this instance should not be a surface point
                    if (!p.Equals(volumeWithNonSurfacePointVoxelsCenter))
                    {
                        expectedNonEdgeSurfacePoints.Add(p);
                    }
                }
            });
            CollectionAssert.AreEqual(expectedNonEdgeSurfacePoints, ExtractSurfacePoints(volumeWithNonSurfacePointVoxels));
        }

        private List<(int x, int y, int z)> ExtractSurfacePoints(Volume3D<byte> volume)
        {
            var surfacePoints = new List<(int x, int y, int z)>();
            volume.IterateSlices(p =>
            {
                if (volume.IsSurfacePoint(p.x, p.y, p.z, true, true, true))
                {
                    surfacePoints.Add(p);
                }
            });
            return surfacePoints;
        }
    }
}
