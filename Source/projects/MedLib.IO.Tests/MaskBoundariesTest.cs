///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedILib.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using InnerEye.CreateDataset.Volumes;
    using NUnit.Framework;
    using MedILib;
    using InnerEye.CreateDataset.Math;

    /// <summary>
    ///  A set of tests to test the identification of boundary vocels in structures with different properties
    /// </summary>
    [TestFixture]
    public class MaskBoundariesTest
    {
        // Create cubic structures
        private readonly Volume3D<byte> _inputImageWithBoundaryVoxels =   new Volume3D<byte>(4, 4, 4);
        private readonly Volume3D<byte> _inputImageWithNoBoundaryVoxels = new Volume3D<byte>(4, 4, 4);
        private readonly Volume3D<byte> _inputWithNoForegroundVoxels =    new Volume3D<byte>(4, 4, 4);

        // Expected boundary points
        private readonly Point3D[] _expectedBoundaryVoxels = new Point3D[]
        {
            new Point3D(1,1,1),
            new Point3D(1,1,2),
            new Point3D(1,2,1),
            new Point3D(1,2,2)
        };

        // Set of points that lie on the edges of the structure
        private readonly List<Point3D> _edgeBoundaryVoxels = new List<Point3D>();

        // Setup the images by marking boundaries 
        [SetUp]
        public void Setup()
        {
            var DimX = _inputImageWithBoundaryVoxels.DimX;
            var DimY = _inputImageWithBoundaryVoxels.DimY;
            var DimZ = _inputImageWithBoundaryVoxels.DimZ;

            for (int x = 0; x < DimX; ++x)
            {
                for (int y = 0; y < DimY; ++y)
                {
                    for (int z = 0; z < DimZ; ++z)
                    {
                        var point = new Point3D(x, y, z);
                        _inputImageWithNoBoundaryVoxels[x, y, z] = 1;

                        // Create boundary voxels on the edges of the structure
                        if (_inputImageWithNoBoundaryVoxels.IsEdgeVoxel(x, y, z))
                        {
                            _edgeBoundaryVoxels.Add(point);
                        }

                        // Create a boundary on the bottom left corner of the structure
                        if (x == 0 && z <= 1)
                        {
                            _inputImageWithBoundaryVoxels[x, y, z] = 0;
                        }
                        else
                        {
                            _inputImageWithBoundaryVoxels[x, y, z] = 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  Test to ensure that only foreground (intensity > 0) voxels are used to identify the boundary
        ///  taking into account the edge points
        /// </summary>
        [Test]
        public void MaskWithNoForegroundMaskBoundariesWithEdgesTest()
        {

            CheckNoBoundaryPointsInImage(_inputWithNoForegroundVoxels, true);
        }

        /// <summary>
        ///  Test to ensure that only foreground (intensity > 0) voxels are used to identify the boundary
        ///  ignoring the edge points
        /// </summary>
        [Test]
        public void MaskWithNoForegroundBoundariesWithoutEdgesTest()
        {

            CheckNoBoundaryPointsInImage(_inputWithNoForegroundVoxels, false);
        }

        /// <summary>
        ///  Test to ensure no other boundary voxels are identified (apart from the ones on the edge)
        ///  in a structure with no background
        /// </summary>
        [Test]
        public void MaskWithNoBoundariesWithEdgesTest()
        {
            CheckBoundaryIsAsExpected(_inputImageWithNoBoundaryVoxels, true, _edgeBoundaryVoxels.ToArray());
        }

        /// <summary>
        ///  Test to ensure no boundary voxels are identified in a structure with no background
        /// </summary>
        [Test]
        public void MaskWithNoBoundariesWithoutEdgesTest()
        {
            CheckNoBoundaryPointsInImage(_inputImageWithNoBoundaryVoxels, false);
        }

        /// <summary>
        ///  Test to ensure only the 26 immediate neighbours of the bottom left corner,
        ///  marked as the boundary ie: the voxels (0,y,[0,1]) are identified as boundary voxels
        ///  ignoring the edge voxels
        /// </summary>       
        [Test]
        public void MaskWithBoundariesWithoutEdgesTest()
        {
            CheckBoundaryIsAsExpected(_inputImageWithBoundaryVoxels, false, _expectedBoundaryVoxels);
        }

        /// <summary>
        ///  Test to ensure only the 26 immediate neighbours of the bottom left corner,
        ///  marked as the boundary ie: the voxels (0,y,[0,1]) are identified as boundary voxels
        ///  taking into account the edge voxels
        /// </summary> 
        [Test]
        public void MaskWithBoundariesWithEdgesTest()
        {
            CheckBoundaryIsAsExpected(_inputImageWithBoundaryVoxels, true, _expectedBoundaryVoxels);
        }


        private void CheckNoBoundaryPointsInImage(Volume3D<byte> inputImage, bool withEdges)
        {
            var outputImage = inputImage.MaskBoundaries(withEdges);
            Assert.That(Array.TrueForAll(outputImage.Array, x => x == 0));
        }
        private void CheckBoundaryIsAsExpected(Volume3D<byte> inputImage, bool withEdges, Point3D[] boundaryPoints)
        {
            var outputImage = inputImage.MaskBoundaries(withEdges);
            var passed = false;
            for (int x = 0; x < inputImage.DimX; ++x)
            {
                for (int y = 0; y < inputImage.DimY; ++y)
                {
                    for (int z = 0; z < inputImage.DimZ; ++z)
                    {
                        var point = new Point3D(x, y, z);
                        var isBoundary = (int)outputImage[x, y, z] == 1;
                        if (isBoundary && boundaryPoints.Contains(point))
                        {
                            passed = true;
                        }
                        else if (isBoundary && !(withEdges && inputImage.IsEdgeVoxel(x, y, z)))
                        {
                            Assert.Fail($"{x},{y},{z} should not be a boundary voxel");
                        }
                    }
                }
            }
            Assert.That(passed);
        }

        [Test]
        public void MaskBoundariesAllOnes()
        {
            int dim = 4;
            var binary = new Volume3D<byte>(dim, dim, dim);
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        binary[i, j, k] = 1;
                    }
                }
            }
            var boundaryFull = binary.MaskBoundaries(true);
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        var val = (i * (dim - 1 - i) * j * (dim - 1 - j) * k * (dim - 1 - k) == 0) ? 1 : 0;
                        Assert.AreEqual(val, boundaryFull[i, j, k]);
                    }
                }
            }
            var boundaryInside = binary.MaskBoundaries(withEdges: false);
            for (int i = 1; i < dim - 1; i++)
            {
                for (int j = 1; j < dim - 1; j++)
                {
                    for (int k = 1; k < dim - 1; k++)
                    {
                        Assert.AreEqual(0, boundaryInside[i, j, k]);
                    }
                }
            }
        }

        [Test]
        public void MaskBoundariesPlane()
        {
            int dim = 7;
            var binary = new Volume3D<byte>(dim, dim, dim);
            for (int i = 0; i < dim; i++)
            {
                var val = (i >= 2 && i <= 4) ? 1 : 0;
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        binary[i, j, k] = (byte)val;
                    }
                }
            }
            var boundary = binary.MaskBoundaries(withEdges: true);
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        // The boundary should be: all of planes i==2 and i==4, and the parts of
                        // plane i==3 that are on the j and/or k edges.
                        var expected = (i == 2 || i == 4 || (i == 3 &&
                            (j == 0 || j == dim - 1 || k == 0 || k == dim - 1))) ? 1 : 0;
                        Assert.AreEqual(expected, boundary[i, j, k]);
                    }
                }
            }
        }

        [Test]
        public void MaskBoundariesChessBoard()
        {
            int dim = 5;
            var binary = new Volume3D<byte>(dim, dim, dim);
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        binary[i, j, k] = (byte)((i + j + k) % 2);
                    }
                }
            }
            var boundary = binary.MaskBoundaries(withEdges: true);
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int k = 0; k < dim; k++)
                    {
                        Assert.AreEqual(binary[i, j, k], boundary[i, j, k]);
                    }
                }
            }
        }
    }
}
