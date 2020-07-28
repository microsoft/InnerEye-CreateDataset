///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Models.DicomRt
{
    using MedLib.IO.RT;
    using InnerEye.CreateDataset.Contours;

    /// <summary>
    /// A RadiotherapyContour is a set of contours forming a single structure within
    /// an RT Structure set. This class brings together:
    ///  * Contour information defining the geometry of the structure
    ///  * An observation of that structure
    ///  * Names and labels associated with the structure. 
    ///  * A Derived copy of the contours in image space.
    /// </summary>
    public class RadiotherapyContour
    {
        /// <summary>
        /// The set of contours forming this structure
        /// </summary>
        public DicomRTContour DicomRtContour { get; }

        /// <summary>
        /// Information about this structure
        /// </summary>
        public DicomRTStructureSetROI StructureSetRoi { get; }

        /// <summary>
        /// An Observation about this structure. Note that in the DICOM RT-Struct model 
        /// a structure can have more than 1 observation - this is not supported. 
        /// </summary>
        public DicomRTObservation DicomRtObservation { get; }

        /// <summary>
        /// A derived version of the DicomRTContours transformed into the coordinate space of a volume3D. 
        /// </summary>
        public ContoursPerSlice Contours { get; set; }

        public RadiotherapyContour(DicomRTContour dicomRtContour, DicomRTStructureSetROI structure, DicomRTObservation dicomRtObservation)
        {
            DicomRtContour = dicomRtContour;
            StructureSetRoi = structure;
            DicomRtObservation = dicomRtObservation;
        }
    }
}