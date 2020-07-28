///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Writers
{
    using System;
    using System.Collections.Generic;
    using MedLib.IO.Extensions;
    using MedLib.IO.Models.DicomRt;
    using MedLib.IO.Readers;
    using MedLib.IO.RT;
    using InnerEye.CreateDataset.Contours;
    using InnerEye.CreateDataset.Volumes;

    public class RTStructCreator
    {
        /// <summary>
        /// Creates a new RadiotherapyContour for inclusion in a RadiotherapyStruct ready for serialization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="axialContours">The contours relative to the given volume you wish to map into the DICOM reference coordinate system</param>
        /// <param name="identifiers"> The DICOM identifiers describing the origin of the volume</param>
        /// <param name="volumeTransform">The volume transform.</param>
        /// <param name="name">The DICOM structure name</param>
        /// <param name="color">The color of this structure</param>
        /// <param name="roiNumber">The roiNumber of this structure</param>
        /// <returns></returns>
        public static RadiotherapyContour CreateRadiotherapyContour(
            ContoursPerSlice axialContours, 
            IReadOnlyList<DicomIdentifiers> identifiers, 
            VolumeTransform volumeTransform, 
            string name, 
            (byte R, byte G, byte B) color, 
            string roiNumber, 
            DicomPersonNameConverter interpreterName,
            ROIInterpretedType roiInterpretedType)
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                throw new ArgumentException("The DICOM identifiers cannot be null or empty");
            }

            var contours = axialContours.ToDicomRtContours(identifiers, volumeTransform);
            var rtcontour = new DicomRTContour(roiNumber, Tuple.Create(color.R, color.G, color.B), contours);
            var rtRoIstructure = new DicomRTStructureSetROI(roiNumber, name, identifiers[0].FrameOfReference.FrameOfReferenceUid, ERoiGenerationAlgorithm.Semiautomatic);
            var observation = new DicomRTObservation(roiNumber, interpreterName, roiInterpretedType);
            var output = new RadiotherapyContour(rtcontour, rtRoIstructure, observation);
            output.Contours = axialContours;
            return output;
        }
    }
}