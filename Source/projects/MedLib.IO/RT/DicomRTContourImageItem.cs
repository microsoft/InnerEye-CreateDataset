///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using Dicom;

    using MedLib.IO.Extensions;

    public class DicomRTContourImageItem
    {
        /// <summary>
        /// Create a reference to the given SOP common instance. 
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static DicomRTContourImageItem Reference(DicomSOPCommon instance)
        {
            return new DicomRTContourImageItem(instance.SopClassUid, instance.SopInstanceUid);
        }

        public DicomRTContourImageItem(string referencedSopClassUid, string referencedSopInstanceUid)
        {
            ReferencedSOPClassUID = referencedSopClassUid;
            ReferencedSOPInstanceUID = referencedSopInstanceUid;
        }

        public string ReferencedSOPClassUID { get; }

        public string ReferencedSOPInstanceUID { get; }

        public static DicomRTContourImageItem Read(DicomDataset imageds)
        {
            var referencedSOPClassUID = imageds.GetStringOrEmpty(DicomTag.ReferencedSOPClassUID);
            var referencedSOPInstanceUID = imageds.GetStringOrEmpty(DicomTag.ReferencedSOPInstanceUID);
            return new DicomRTContourImageItem(referencedSOPClassUID, referencedSOPInstanceUID);
        }

        public static DicomDataset Write(DicomRTContourImageItem contour)
        {
            var ds = new DicomDataset();
            ds.Add(DicomTag.ReferencedSOPClassUID, contour.ReferencedSOPClassUID);
            ds.Add(DicomTag.ReferencedSOPInstanceUID, contour.ReferencedSOPInstanceUID);
            return ds;
        }
    }
}