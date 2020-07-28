///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace MedLib.IO.RT
{
    using System.Collections.Generic;
    using System.Linq;

    using Dicom;

    using Extensions;
    using System.Globalization;

    public class DicomRTContourItem
    {
        /// <summary>
        /// The points of the contour in the DICOM Reference Coordinate System, consisting of {x,y,z} triplets.
        /// </summary>
        public IReadOnlyList<double> Data { get; }

        /// <summary>
        /// Number of {x,y,z} triplets in Data
        /// </summary>
        public int NumberOfPoints { get; }

        /// <summary>
        /// The Geoetric type of the contour
        /// </summary>
        public string GeometricType { get; }

        /// <summary>
        /// The sequence of referenced images containing the contour data
        /// </summary>
        public IReadOnlyList<DicomRTContourImageItem> ImageSeq { get; }

        /// <summary>
        /// Construct a new DicomRTContourItem 
        /// </summary>
        /// <param name="data"> An array containing 3*n numbers where n is the number of points</param>
        /// <param name="numberOfPoints">The number of {x,y,z} triplets in data</param>
        /// <param name="geometricType">The type of the contour item {CLOSED_PLANAR, POINT, OPEN_PLANAR, OPEN_NONPLANAR}</param>
        /// <param name="imageSeq">The image UID containing this contour. There is usually just 1 item in the sequence</param>
        public DicomRTContourItem(double[] data, int numberOfPoints, string geometricType, IReadOnlyList<DicomRTContourImageItem> imageSeq)
        {
            Data = data;
            NumberOfPoints = numberOfPoints;
            GeometricType = geometricType;
            ImageSeq = imageSeq;
        }

        /// <summary>
        /// Read an RT Contour Item from the given dataset.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DicomRTContourItem Read(DicomDataset ds)
        {
            var numberOfPoints = ds.GetSingleValue<int>(DicomTag.NumberOfContourPoints);
            // fo-dicom internally parses strings to decimal using InvariantCulture and then converts to double 
            var data = ds.GetValues<double>(DicomTag.ContourData);
            var geometricType = ds.GetTrimmedStringOrEmpty(DicomTag.ContourGeometricType);
            List<DicomRTContourImageItem> listImages = new List<DicomRTContourImageItem>();
            if (ds.Contains(DicomTag.ContourImageSequence))
            {
                var seqImages = ds.GetSequence(DicomTag.ContourImageSequence);
                foreach (var item in seqImages)
                {
                    listImages.Add(DicomRTContourImageItem.Read(item));
                }
            }
            return new DicomRTContourItem(data, numberOfPoints, geometricType, listImages);
        }

        /// <summary>
        /// Constructs a new DicomDataSet representing the given DicomRTContourItem
        /// </summary>
        /// <param name="contourItem"></param>
        /// <returns></returns>
        public static DicomDataset Write(DicomRTContourItem contourItem)
        {
            var newDS = new DicomDataset();

            // The contour data is in Dicom Patient Coordinates (always mm units)
            // rounding to 0.001 gives us 1 micron accuracy and should not blow the 16 char limit in DICOM
            // for DS value representations. 
            var roundedData = contourItem.Data.Select(d => d.ToString("F3", CultureInfo.InvariantCulture)).ToArray();

            newDS.Add(DicomTag.ContourData, roundedData);
            newDS.Add(DicomTag.NumberOfContourPoints, contourItem.NumberOfPoints);
            newDS.Add(DicomTag.ContourGeometricType, contourItem.GeometricType);
            var imageDataSets = new List<DicomDataset>();
            foreach (var imageSeq in contourItem.ImageSeq)
            {
                imageDataSets.Add(DicomRTContourImageItem.Write(imageSeq));
            }
            newDS.Add(new DicomSequence(DicomTag.ContourImageSequence, imageDataSets.ToArray()));
            return newDS;
        }
    }
}