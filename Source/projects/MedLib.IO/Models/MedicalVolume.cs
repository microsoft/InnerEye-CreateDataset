///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Models
{
    using DicomRt;
    using Readers;
    using InnerEye.CreateDataset.Volumes;
    using System.Diagnostics;
    using System.Collections.Generic;
    using Dicom;

    public class MedicalVolume
    {
        /// <summary>
        /// True if this MedicalVolume was generated from DICOM images. 
        /// </summary>
        public bool IsDicom => Identifiers != null & Identifiers.Count > 0;

        /// <summary
        /// Return true if this is a CT scan - we default to true for the nifti case. 
        /// </summary>
        public bool IsCT => !IsDicom || (IsDicom && Identifiers[0].Image.SopCommon.SopClassUid == DicomUID.CTImageStorage.UID);

        /// <summary>
        /// The 3D volume 
        /// </summary>
        public Volume3D<short> Volume { get; }

        /// <summary>
        /// Paths of all the files that formed the Volume
        /// </summary>
        public IReadOnlyList<string> FilePaths { get; }

        /// <summary>
        /// Dicom information for all the files that formed the volume (or empty in the case of nifti)
        /// </summary>
        public IReadOnlyList<DicomIdentifiers> Identifiers { get; }

        /// <summary>
        /// Any radiotherapy struct associated with the Volume. This is never null. 
        /// </summary>
        public RadiotherapyStruct Struct { get; set; }

        /// <summary>
        /// Construct MedicalVolume from Dicom images
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="identifiers"></param>
        /// <param name="filePaths"></param>
        /// <param name="rtStruct"></param>
        public MedicalVolume(
            Volume3D<short> volume,
            IReadOnlyList<DicomIdentifiers> identifiers,
            IReadOnlyList<string> filePaths,
            RadiotherapyStruct rtStruct)
        {
            Debug.Assert(volume != null);
            Debug.Assert(identifiers != null);
            Debug.Assert(filePaths != null);
            Debug.Assert(rtStruct != null);

            Identifiers = identifiers;
            Volume = volume;
            FilePaths = filePaths;
            Struct = rtStruct;
        }

    }
}
