///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using Dicom;
    using MedLib.IO.Extensions;
    using MedLib.IO.Models.DicomRt;
    using MedLib.IO.RT;
    using InnerEye.CreateDataset.Contours;
    using InnerEye.CreateDataset.Volumes;

    public class RtStructReader
    {              
        public static Tuple<RadiotherapyStruct, string> LoadContours(
            string filePath, Transform3 dicomToData, string seriesUID = null, string studyUID = null, bool warningsAsErrors = true)
        {
            var file = DicomFile.Open(filePath);
            return LoadContours(file.Dataset, dicomToData, seriesUID, studyUID, warningsAsErrors);
        }

        /// <summary>
        /// Load a RadiotherapyStruct from the given dicom dataset and map into the coordinate of the given Volume3D
        /// </summary>
        /// <param name="ds">Dataset to read the structure set from</param>
        /// <param name="dicomToData">The transform from going between dicom and voxel points.</param>
        /// <param name="seriesUID">SeriesUID that must match the referenced seriesUID inside the structure set</param>
        /// <param name="studyUID">The structure set must belong to the same study</param>
        /// <param name="warningsAsErrors">true if warnings should be treated as errors and thrown from this method.</param>
        /// <returns>A new RadiotherapyStruct with any warnings collated into a string</returns>
        public static Tuple<RadiotherapyStruct, string> LoadContours(
           DicomDataset ds, Transform3 dicomToData, string seriesUID = null, string studyUID = null, bool warningsAsErrors = true)
        {
            RadiotherapyStruct rtStruct = RadiotherapyStruct.Read(ds);

            if (studyUID != null && studyUID != rtStruct.Study.StudyInstanceUid)
            {
                var warningText = $"The RT STRUCTURE Set does not belong to this Study";
                if (warningsAsErrors)
                {
                    throw new ArgumentException(warningText);
                }
                return Tuple.Create<RadiotherapyStruct, string>(null, warningText);
            }

            // ReferencedFramesOfRef must contain at least 1 study/series reference 
            if (CheckUnreferencedSeries(rtStruct.StructureSet, seriesUID))
            {
                var warningText = $"The RT STRUCTURE does not reference this Series";
                if (warningsAsErrors)
                {
                    throw new ArgumentException(warningText);
                }
                return Tuple.Create<RadiotherapyStruct, string>(null, warningText);
            }

            var contoursData = rtStruct.Contours;
            // Do not warn if all structures are empty
            if (contoursData.Count != 0 && !contoursData.Any(c => CanUseStructData(c.DicomRtContour)))
            {
                var warningText = $"The RT STRUCTURE does not contain CLOSED PLANAR contours";
                if (warningsAsErrors)
                {
                    throw new ArgumentException(warningText);
                }
                return Tuple.Create<RadiotherapyStruct, string>(null, warningText);
            }

            var warningTypeText = string.Empty;

            if (CheckUnsupportedData(rtStruct))
            {
                var badTypes = UnsupportedTypes(rtStruct);
                warningTypeText = $"The RT STRUCTURE contains unsupported Contour Types: {string.Join(",",badTypes)}";

                // remove the parent structures
                RemoveUnsupportedTypes(rtStruct);
            }

            Parallel.ForEach(contoursData, c => CreateStructureContoursBySlice(dicomToData, c)); 

            return Tuple.Create(rtStruct, warningTypeText);
        }

        /// <summary>
        /// returns true if the given seriesUID is not null and not referenced in the structure set specified
        /// </summary>
        /// <param name="structureSet"></param>
        /// <param name="seriesUID"></param>
        /// <returns></returns>
        private static bool CheckUnreferencedSeries(DicomRTStructureSet structureSet, string seriesUID)
        {
            return seriesUID != null && 
                !structureSet.ReferencedFramesOfRef.Any(
                x => x.ReferencedStudies.Any(
                    y => y.ReferencedSeries.Any(
                        z => z.SeriesInstanceUID == seriesUID)));
        }

        /// <summary>
        /// Returns true if there is at least 1 closed planar contour or no contours at all. 
        /// </summary>
        /// <param name="contour"></param>
        /// <returns></returns>
        private static bool CanUseStructData(DicomRTContour c)
        {
            return c.DicomRtContourItems.Count == 0 || c.DicomRtContourItems.Any(r => r.GeometricType == DicomExtensions.ClosedPlanarString);
        }

        /// <summary>
        /// Checks if any of the contours are unsupported 
        /// </summary>
        /// <param name="contoursData"></param>
        /// <returns></returns>
        private static bool CheckUnsupportedData(RadiotherapyStruct rtStruct)
        {
            return rtStruct.Contours.Any(c => c.DicomRtContour.DicomRtContourItems.Any(r => r.GeometricType != DicomExtensions.ClosedPlanarString));
        }

        /// <summary>
        /// Returns a unique sequence of unsupported types in the given structure by name
        /// </summary>
        /// <param name="contoursData"></param>
        /// <returns></returns>
        private static IEnumerable<string> UnsupportedTypes(RadiotherapyStruct rtStruct)
        {
            return rtStruct.Contours.SelectMany(
                c => c.DicomRtContour.DicomRtContourItems).
                    Where(c => c.GeometricType != DicomExtensions.ClosedPlanarString).
                        Select(c1 => c1.GeometricType).
                            Distinct();
        }

        /// <summary>
        /// Removes all unsupported types in the given struct
        /// </summary>
        /// <param name="rtStruct"></param>
        private static void RemoveUnsupportedTypes(RadiotherapyStruct rtStruct)
        {
            // remove the parent structures
            var invalidContours = rtStruct.Contours.Where(
                c => c.DicomRtContour.DicomRtContourItems.Any(
                    r => r.GeometricType != DicomExtensions.ClosedPlanarString)).ToList();

            foreach (var i in invalidContours)
            {
                rtStruct.Contours.Remove(i);
            }
        }

        /// <summary>
        /// Map the given structure into the coordinate space of the Volume3D specified creating the
        /// ContoursBySlice instance
        /// </summary>
        /// <param name="dicomToData">The dicom to data transform.</param>
        /// <param name="c"></param>
        private static void CreateStructureContoursBySlice(Transform3 dicomToData, RadiotherapyContour c)
        {
            var structContours = c.DicomRtContour.DicomRtContourItems;
            var contourPoints = new List<Tuple<int, ContourPolygon>>();

            foreach (var contour in structContours)
            {
                var pointsOnSlice = ToContour(dicomToData, contour.Data);
                contourPoints.Add(pointsOnSlice);
               
            }

            var sliceDict = 
                contourPoints.GroupBy(x => x.Item1).
                ToDictionary(x => x.Key, g => (IReadOnlyList<ContourPolygon>)g.Select(x => x.Item2).
                ToList());

            c.Contours = new ContoursPerSlice(sliceDict);
        }


        /// <summary>
        /// Map an array of doubles are read from DICOM into the coordinate space of the given volume3D
        /// </summary>
        /// <param name="dicomToData">The dicom to data transform.</param>
        /// <param name="contourData"></param>
        /// <returns></returns>
        private static Tuple<int, ContourPolygon> ToContour(Transform3 dicomToData, IReadOnlyList<double> contourData)
        {
            var array = new PointF[contourData.Count / 3];
            var first3DPoint = new Point3D(contourData[0], contourData[1], contourData[2]);
            var firstPixel = dicomToData * first3DPoint;

            var z = Convert.ToInt32(firstPixel.Z);

            Parallel.For(
                0,
                array.Length,
                i =>
                {
                    var j = i * 3;

                    var physicalPoint = new Point3D(
                        contourData[j],
                        contourData[j + 1],
                        contourData[j + 2]);

                    var pixelPoint = dicomToData * physicalPoint;


                    if (Convert.ToInt32(pixelPoint.Z) != z)
                    {
                        throw new ArgumentException(
                            "Invalid data: this contour contains points that are not in the same plane");
                    }

                    array[i] = new PointF((float)pixelPoint.X, (float)pixelPoint.Y);
                });

            return Tuple.Create(z, new ContourPolygon(array,0));
        }
    }
}