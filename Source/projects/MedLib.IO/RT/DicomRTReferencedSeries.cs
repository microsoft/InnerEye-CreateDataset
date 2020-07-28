///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using System.Collections.Generic;

    using Dicom;

    using MedLib.IO.Extensions;

    public class DicomRTReferencedSeries
    {
        public string SeriesInstanceUID { get; }

        public IReadOnlyList<DicomRTContourImageItem> ContourImages { get; }

        public DicomRTReferencedSeries(string seriesInstanceUid, IReadOnlyList<DicomRTContourImageItem> contourImages)
        {
            SeriesInstanceUID = seriesInstanceUid;
            ContourImages = contourImages;
        }

        public static DicomRTReferencedSeries Read(DicomDataset ds)
        {
            var seriesInstanceUID = ds.GetStringOrEmpty(DicomTag.SeriesInstanceUID);

            var contourImages = new List<DicomRTContourImageItem>();
            if (ds.Contains(DicomTag.ContourImageSequence))
            {
                var seq = ds.GetSequence(DicomTag.ContourImageSequence);
                foreach (var item in seq)
                {
                    var contourImageItem = DicomRTContourImageItem.Read(item);
                    contourImages.Add(contourImageItem);
                }
            }

            return new DicomRTReferencedSeries(seriesInstanceUID, contourImages);
        }

        public static DicomDataset Write(DicomRTReferencedSeries series)
        {
            var ds = new DicomDataset();
            ds.Add(DicomTag.SeriesInstanceUID, series.SeriesInstanceUID);

            var listOfContour = new List<DicomDataset>();
            foreach (var contour in series.ContourImages)
            {
                var newDS = DicomRTContourImageItem.Write(contour);
                listOfContour.Add(newDS);
            }

            ds.Add(new DicomSequence(DicomTag.ContourImageSequence, listOfContour.ToArray()));
            return ds;
        }
    }
}