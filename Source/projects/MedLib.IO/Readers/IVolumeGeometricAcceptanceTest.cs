///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Readers
{
    using Dicom;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Allow for customized acceptance of a volume depending on criteria outside the scope of this library
    /// </summary>
    public interface IVolumeGeometricAcceptanceTest
    {
        /// <summary>
        /// Called by the framework when it proposes to form a volume encoded by {volumeOrigin, iop, voxelDims} from the
        /// given set of ordered Dicom datasets. 
        /// </summary>
        /// <param name="sopClassUid">The type of images forming the volume</param>
        /// <param name="volumeOrigin">The origin of the volume in the DICOM reference coordinate system</param>
        /// <param name="iop">The orientation matrix of the volume in the DICOM reference coordinate system</param>
        /// <param name="voxelDims">The dimensions of the voxels in mm</param>
        /// <param name="reason">Set this to a human readable string defining the reason for rejection</param>
        /// <returns>Return true if and only if you agree to this</returns>
        bool Propose(DicomUID sopClassUid, Point3D volumeOrigin, Matrix3 iop, Point3D voxelDims, out string reason);

        /// <summary>
        /// Constructing volumes from a candidate set of DICOM images necessarily involves error. This method will be called
        /// by the implementation for every corner of every slice. You must explicitely accept the error for each call otherwise
        /// the entire volume will be rejected and the images will not load. 
        /// </summary>
        /// <param name="actualCoordinate">The actual position of the slice corner in the reference coordinate system
        /// inferred from the DICOM dataset</param>
        /// <param name="volumeCoordinate">The position of the slice corner in the reference coordinate system
        /// within the volumetric construction</param>
        /// <returns></returns>
        bool AcceptPositionError(DicomUID sopClassUid, Point3D actualCoordinate, Point3D volumeCoordinate);

        /// <summary>
        /// Constructing volumes from a candidate set of DICOM images necessarily involves error.This method is called for every sequential pair of slices in the volume. 
        /// You must explicitely accept the error for each call otherwise the entire volume will be rejected and the images will not load
        /// </summary>
        /// <param name="sliceGap">The gap in mm between sequential slices</param>
        /// <param name="medianSliceGap">The median slice gap in mm</param>
        /// <returns></returns>
        bool AcceptSliceSpacingError(DicomUID sopClassUid, double sliceGap, double medianSliceGap);
    }

}