///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using System;
    using System.Collections.Generic;

    using Dicom;

    using MedLib.IO.Extensions;

    public class DicomRTReferencedStudy
    {

        public static readonly string StudyComponentManagementSopClass = DicomUID.StudyComponentManagementSOPClassRETIRED.UID;

        public string ReferencedSOPClassUID { get; }

        public string ReferencedSOPInstanceUID { get; }

        public IReadOnlyList<DicomRTReferencedSeries> ReferencedSeries { get; }

        public DicomRTReferencedStudy(
            string referencedSopClassUid,
            string referencedSopInstanceUid,
            IReadOnlyList<DicomRTReferencedSeries> referencedSeries)
        {
            ReferencedSOPClassUID = referencedSopClassUid;
            ReferencedSOPInstanceUID = referencedSopInstanceUid;
            ReferencedSeries = referencedSeries;
        }

        public static DicomRTReferencedStudy Read(DicomDataset ds)
        {
            ds = ds ?? throw new ArgumentException(nameof(ds));

            var refSOPClass = ds.GetStringOrEmpty(DicomTag.ReferencedSOPClassUID);
            var refSOPInstance = ds.GetStringOrEmpty(DicomTag.ReferencedSOPInstanceUID);
            var listSeries = new List<DicomRTReferencedSeries>();

            if (ds.Contains(DicomTag.RTReferencedSeriesSequence))
            {
                var seq = ds.GetSequence(DicomTag.RTReferencedSeriesSequence);
                foreach (var item in seq)
                {
                    listSeries.Add(DicomRTReferencedSeries.Read(item));
                }
            }
            return new DicomRTReferencedStudy(refSOPClass, refSOPInstance, listSeries);
        }

        public static DicomDataset Write(DicomRTReferencedStudy refStudy)
        {
            refStudy = refStudy ?? throw new ArgumentException(nameof(refStudy));

            var ds = new DicomDataset();
            ds.Add(DicomTag.ReferencedSOPClassUID, refStudy.ReferencedSOPClassUID);
            ds.Add(DicomTag.ReferencedSOPInstanceUID, refStudy.ReferencedSOPInstanceUID);
            var listOfContour = new List<DicomDataset>();
            foreach (var series in refStudy.ReferencedSeries)
            {
                var newDS = DicomRTReferencedSeries.Write(series);
                listOfContour.Add(newDS);
            }
            if (listOfContour.Count > 0)
            {
                ds.Add(new DicomSequence(DicomTag.RTReferencedSeriesSequence, listOfContour.ToArray()));
            }
            return ds;
        }
    }
}