///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Dicom;

    public class DicomRTContour
    {
        public Tuple<byte, byte, byte> RGBColor { get; }

        public IReadOnlyList<DicomRTContourItem> DicomRtContourItems { get; }

        public string RgbColorAsString()
        {
            return string.Join(@"\", new int[] { RGBColor.Item1, RGBColor.Item2, RGBColor.Item3 });
        }

        public string ReferencedRoiNumber { get; }

        public DicomRTContour(string referencedRoiNumber, Tuple<byte, byte, byte> colorRgb, IReadOnlyList<DicomRTContourItem> contoursPerSlice)
        {
            ReferencedRoiNumber = referencedRoiNumber;
            RGBColor = colorRgb;
            DicomRtContourItems = contoursPerSlice;
        }

        public static IReadOnlyList<DicomRTContour> Read(DicomDataset ds)
        {
            var contours = new List<DicomRTContour>();
            if (ds.Contains(DicomTag.ROIContourSequence))
            {
                var seq = ds.GetSequence(DicomTag.ROIContourSequence);
                foreach (var item in seq)
                {
                    // Note this must be present but we should avoid throwing here
                    var referencedRoiNumber = item.GetSingleValueOrDefault(DicomTag.ReferencedROINumber, string.Empty);

                    var color = item.GetValues<string>(DicomTag.ROIDisplayColor) ?? new[] { "255", "255", "255" };

                    var contourItems = new List<DicomRTContourItem>();
                    if (item.Contains(DicomTag.ContourSequence))
                    {
                        var seqContour = item.GetSequence(DicomTag.ContourSequence);
                        contourItems.AddRange(seqContour.Select(DicomRTContourItem.Read));
                    }

                    contours.Add(new DicomRTContour(referencedRoiNumber, ParseColor(color), contourItems));

                }
            }
            return contours;
        }

        public static void Write(DicomDataset ds, IEnumerable<DicomRTContour> contours)
        {
            var roiContourSequence = new List<DicomDataset>();
            foreach (var contour in contours)
            {
                var newDs = new DicomDataset();

                newDs.Add(DicomTag.ReferencedROINumber, contour.ReferencedRoiNumber);
                newDs.Add(DicomTag.ROIDisplayColor, contour.RgbColorAsString());

                var contourItemDs = contour.DicomRtContourItems.Select(DicomRTContourItem.Write).ToList();
                newDs.Add(new DicomSequence(DicomTag.ContourSequence, contourItemDs.ToArray()));

                roiContourSequence.Add(newDs);
            }

            // Only write out contour sequences with contours. 
            if (roiContourSequence.Count > 0)
            {
                ds.Add(new DicomSequence(DicomTag.ROIContourSequence, roiContourSequence.ToArray()));
            }
        }

        private static Tuple<byte, byte, byte> ParseColor(string[] colorString)
        {
            var rgb = colorString.Select(x => (byte)int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
            return Tuple.Create(rgb[0], rgb[1], rgb[2]);
        }
    }
}