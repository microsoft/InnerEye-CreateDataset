///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿
namespace MedLib.IO.Readers
{
    using System;
    using Dicom;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Implementation of IVolumeGeometricAcceptanceTest based upon IHE BRTO guidelines. 
    /// </summary>
    public class StrictGeometricAcceptanceTest : IVolumeGeometricAcceptanceTest
    {
        /// <summary>
        /// Rejection message
        /// </summary>
        private readonly string NonSquarePixelMessage;

        /// <summary>
        /// Rejection message
        /// </summary>
        private readonly string NonAxialMessage;

        /// <summary>
        /// The maximum angle in radians from true Axial all volumes
        /// </summary>
        public const double MaxAngleFromAxialInRadians = 0.001;

        /// <summary>
        /// The maximum difference between the median slice gap and any gap betwen adjacent slices in the volume.
        /// </summary>
        public const double SliceSpacingTolleranceMM = 0.01;

        /// <summary>
        /// This is not clearly defined in the IHE BRTO profile. We chose 0.1mm
        /// </summary>
        public const double GridPositionOffsetToleranceMM = 0.1;

        /// <summary>
        /// The epsilon passed into the Orthonormality check for MR orientations. 
        /// </summary>
        public const double OrthonormalityCheckEpsilonMR = 0.0001;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nonSquarePixelMessage">Error to display if pixels are non square</param>
        /// <param name="nonAxialMessage">Error to display if volume is not axial.</param>
        public StrictGeometricAcceptanceTest( string nonSquarePixelMessage, string nonAxialMessage )
        {
            NonSquarePixelMessage = nonSquarePixelMessage;
            NonAxialMessage = nonAxialMessage; 
        }

        /// <summary>
        /// Only allow an epsilon away from true axial, insist on square pixels. 
        /// </summary>
        /// <param name="sopClassUid"></param>
        /// <param name="volumeOrigin"></param>
        /// <param name="iop"></param>
        /// <param name="voxelDims"></param>
        /// <returns></returns>
        public bool Propose(DicomUID sopClassUid, Point3D volumeOrigin, Matrix3 iop, Point3D voxelDims, out string reason)
        {
            // Restrict to MaxAngleFromAxialInRadians from True Axial. 
            Point3D xAxis = new Point3D(1, 0, 0);
            Point3D yAxis = new Point3D(0, 1, 0);
            Point3D zAxis = new Point3D(0, 0, 1);

            var minCosine = Math.Cos(MaxAngleFromAxialInRadians);

            var xAxisT = iop * xAxis;
            var yAxisT = iop * yAxis;
            var zAxisT = iop * zAxis;

            var isWithinAngle = Point3D.DotProd(xAxis, xAxisT) >= minCosine && Point3D.DotProd(yAxis, yAxisT) >= minCosine && Point3D.DotProd(zAxis, zAxisT) >= minCosine;
            var isPixelIsotropic = voxelDims[0] == voxelDims[1];

            if (!isWithinAngle)
            {
                reason = NonAxialMessage;
                return false; 
            } else 
            if (!isPixelIsotropic)
            {
                reason = NonSquarePixelMessage;
                return false; 
            }
            reason = string.Empty; 
            return true; 
        }

        /// <summary>
        /// Insist that we are are at most GridPositionOffsetToleranceVoxels away from the true position
        /// </summary>
        /// <param name="actualCoordinate"></param>
        /// <param name="volumeCoordinate"></param>
        /// <returns></returns>
        public bool AcceptPositionError(DicomUID sopClassUid, Point3D actualCoordinate, Point3D volumeCoordinate)
        {
            return (actualCoordinate - volumeCoordinate).Norm() < GridPositionOffsetToleranceMM;
        }

        /// <summary>
        /// Insist that all slice gaps differ from the median by at most SliceSpacingTolleranceMM
        /// </summary>
        /// <param name="sliceGap"></param>
        /// <param name="medianSliceGap"></param>
        /// <returns></returns>
        public bool AcceptSliceSpacingError(DicomUID sopClassUid, double sliceGap, double medianSliceGap)
        {
            return Math.Abs(medianSliceGap - sliceGap) < SliceSpacingTolleranceMM;
        }
    }
}
