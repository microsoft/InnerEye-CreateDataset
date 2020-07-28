///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Writers
{
    using Dicom;
    using Models.DicomRt;

    public class RtStructWriter
    {
        public static void SaveRtStruct(string filePath, RadiotherapyStruct rtStruct)
        {
            var file = GetRtStructFile(rtStruct);
            file.Save(filePath);
        }

        public static DicomFile GetRtStructFile(RadiotherapyStruct rtStruct)
        {
            var file = new DicomFile();
            var ds = file.Dataset;

            // We must use the same UID for SOPInstanceUID & MediaStorageSOPInstanceUID
            DicomUID sopInstanceUID = DicomUID.Generate();

            file.FileMetaInfo.MediaStorageSOPClassUID = DicomUID.RTStructureSetStorage;
            file.FileMetaInfo.MediaStorageSOPInstanceUID = sopInstanceUID;
            // Fo-Dicom has some policy for this - using the machine name - we remove this
            file.FileMetaInfo.Remove(DicomTag.SourceApplicationEntityTitle);

            // It is very important that we only use ImplicitVRLittleEndian here, otherwise large contours
            // can exceed the maximum length of a an explicit VR. 
            file.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ImplicitVRLittleEndian;

            //WRITE INSTANCE UID AND SOP CLASS UID
            ds.Add(DicomTag.SOPClassUID, DicomUID.RTStructureSetStorage);
            ds.Add(DicomTag.SOPInstanceUID, sopInstanceUID);

            RadiotherapyStruct.Write(ds, rtStruct);

            return file;
        }
    }
}
