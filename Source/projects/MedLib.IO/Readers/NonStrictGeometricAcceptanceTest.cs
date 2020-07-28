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

    public class NonStrictGeometricAcceptanceTest : IVolumeGeometricAcceptanceTest
    {
        /// <summary>
        /// The error message that is printed out if the volume is rejected because of an invalid pixels size.
        /// </summary>
        private readonly string NonSquarePixelMessage;

        /// <summary>
        /// Rejection message 
        /// </summary>
        private readonly string UnsupportedOrientation;

        /// <summary>
        /// The maximum allowed aspect ratio for pixels, if the volume is an MR scan.
        /// </summary>
        private readonly double MaxPixelSizeRatioMR;

        /// <summary>
        /// The maximum distance in mm allowed between the proposed volume position and the actual DICOM position for each
        /// corner of every slice forming the volume. 
        /// </summary>
        public const double GridPositionOffsetToleranceMM = 2.0;

        /// <summary>
        /// The default value for the maximum allowed ratio between pixels of MR images.
        /// </summary>
        public const double DefaultMaxPixelSizeRatioMR = 1.001;

        /// <summary>
        /// The maximum difference between the median slice gap and any gap betwen adjacent slices in the volume.
        /// </summary>
        public const double SliceSpacingTolleranceMM = 0.1;

        /// <summary>
        /// The epsilon passed into the Orthonormality check for rounded orientations. 
        /// </summary>
        public const double OrthonormalityCheckEpsilon = 0.0001;

        /// <summary>
        /// Construct a new instance of NonFDAGeometricAcceptanceTest 
        /// </summary>
        /// <param name="nonSquarePixelMessage">Error message to display if pixel aspect ratio non-square</param>
        /// <param name="unsupportedOrientation">Error message to display if orientation unsupported</param>
        /// <param name="maxPixelSizeRatioMR">The maximum allowed aspect ratio for pixels, if the volume is an MR scan.</param>
        public NonStrictGeometricAcceptanceTest(string nonSquarePixelMessage, 
            string unsupportedOrientation,
            double maxPixelSizeRatioMR = DefaultMaxPixelSizeRatioMR) 
        {
            NonSquarePixelMessage = nonSquarePixelMessage;
            UnsupportedOrientation = unsupportedOrientation;
            MaxPixelSizeRatioMR = maxPixelSizeRatioMR;
        }

        /// <summary>
        /// Accept any orientation if the RenderVolume can represent it, apply limits to the ratio of voxelDimensions 0 and 1 for MR images
        /// </summary>
        /// <param name="sopClassUid"></param>
        /// <param name="volumeOrigin"></param>
        /// <param name="iop"></param>
        /// <param name="voxelDims"></param>
        /// <returns></returns>
        public bool Propose(DicomUID sopClassUid, Point3D volumeOrigin, Matrix3 iop, Point3D voxelDims, out string reason)
        {
            // We apply the same procedure as done to create the render volume. If this
            // leads to a loss of orthonormality - we reject the orientation
            var roundedDirection = Matrix3.ElementWiseRound(iop);
            var canBuildRenderVolume = Matrix3.IsOrthonormalBasis(roundedDirection, OrthonormalityCheckEpsilon);
            reason = string.Empty; 
            if (!canBuildRenderVolume)
            {
                reason = UnsupportedOrientation;
                return false; 
            }

            // Insist the pixel dimensions are exactly the same. 
            var isPixelIsotropic = voxelDims.X == voxelDims.Y;
            // Or within an epsilon
            var pixelSidesRatio = Math.Max(voxelDims.X, voxelDims.Y) / Math.Min(voxelDims.X, voxelDims.Y);
            var isPixelNearIsotropic = pixelSidesRatio < MaxPixelSizeRatioMR;

            if (sopClassUid == DicomUID.CTImageStorage && !isPixelIsotropic)
            {
                reason = NonSquarePixelMessage + $": Pixel size ({voxelDims.X}, {voxelDims.Y})";
                return false; 

            }
            else if (sopClassUid == DicomUID.MRImageStorage && !isPixelNearIsotropic)
            {
                reason = NonSquarePixelMessage + $": Pixel size ({voxelDims.X}, {voxelDims.Y}) has an aspect ratio of {pixelSidesRatio}, but should be below {MaxPixelSizeRatioMR}";
                return false;
            }
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
