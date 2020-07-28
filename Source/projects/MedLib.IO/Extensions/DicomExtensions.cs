///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dicom;
    using InnerEye.CreateDataset.Volumes;    
    using MedLib.IO.RT;
    using MedLib.IO.Readers;

    public static class DicomExtensions
    {
        /// <summary>
        /// DICOM Code String (CS) String literal for the types of contours we produce
        /// </summary>
        public const string ClosedPlanarString = "CLOSED_PLANAR";

        public static string GetStringOrEmpty(this DicomDataset ds, DicomTag tag)
        {
            return ds.GetSingleValueOrDefault(tag, string.Empty);
        }

        /// <summary>
        /// In general DICOM values have an even byte length when serialized. fo-dicom correctly adds a padding value to those VRs 
        /// requiring a pad byte. However the actual padding value used by other implementations can vary, often 0x00 is used instead
        /// of 0x20 (specificed as the padding byte in DICOM), fo-dicom will not remove these on deserialization. We add this method
        /// so users can selectively remove erroneous trailing white space characters and preserve the validity of our output dicom. 
        /// Use with caution, it is only valid for all VRs encoded as character strings BUT NOT VR UI (Unique Identifier) types. 
        /// </summary>
        /// <see cref="ftp://dicom.nema.org/medical/DICOM/2013/output/chtml/part05/sect_6.2.html"/>
        /// <param name="ds"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static string GetTrimmedStringOrEmpty(this DicomDataset ds, DicomTag tag)
        {
            return DicomTrim(ds.GetStringOrEmpty(tag));
        }

        public static string DicomTrim(string s)
        {
            return s.Trim('\0').Trim();
        }

        /// <summary>
        /// Returns the value of the tag within the given dataset. Throws an exception if the tag was not present or if the
        /// tag value cannot be converted to type T.
        /// </summary>
        /// <typeparam name="T">The expected return type of the DICOM attribute.</typeparam>
        /// <param name="dataset">The DICOM dataset you wish to extract the tag from</param>
        /// <param name="tag">The tag you wish to extract from the dataset</param>
        /// <param name="i">For multivalue tags, specify the ith element to return</param>
        /// <returns>The required DICOM attribute as type T.</returns>
        /// <exception cref="ArgumentException">The dataset did not contain the expected tag or the DICOM tag does not have a value at index 'i'.</exception>
        /// <exception cref="ArgumentNullException">The DICOM dataset or DICOM tag provided was null.</exception>
        /// <exception cref="InvalidCastException">The DICOM tag could not be converted into the expected type.</exception>
        public static T GetRequiredDicomAttribute<T>(this DicomDataset dataset, DicomTag tag, uint i = 0)
        {
            dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            tag = tag ?? throw new ArgumentNullException(nameof(tag));

            // Attempt to find the expected tag in the DICOM dataset.
            if (!(dataset.FirstOrDefault(x => x.Tag == tag) is DicomElement dicomElement))
            {
                throw new ArgumentException($"The DICOM dataset does not contain the required attribute: {tag}.");
            }

            if (i >= dicomElement.Count)
            {
                throw new ArgumentException($"The DICOM tag {tag} only has {dicomElement.Count} parts. Expected to get value at index {i}.");
            }

            try
            {
                // Attempt to cast the i(th) element to the expected return type.
                return dicomElement.Get<T>((int)i);
            }
            catch (Exception)
            {
                throw new InvalidCastException($"The attribute: {tag} could not be converted to the expected type: {typeof(T)}.");
            }
        }

        /// <summary>
        /// Converts a set of application contours into DICOM RT objects ready for serialization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="axialContours">The axial contours you wish to convert</param>
        /// <param name="identifiers">The set of identifiers, 1 for each slice in parentVolume</param>
        /// <param name="volumeTransform">The volume transform.</param>
        /// <returns></returns>
        public static List<DicomRTContourItem> ToDicomRtContours(
            this InnerEye.CreateDataset.Contours.ContoursPerSlice axialContours, IReadOnlyList<DicomIdentifiers> identifiers, VolumeTransform volumeTransform)
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                throw new ArgumentException(nameof(identifiers), "The identifiers cannot be null or empty");
            }

            if (volumeTransform == null)
            {
                throw new ArgumentException(nameof(volumeTransform), "The volume cannot be null or empty");
            }

            var resultList = new List<DicomRTContourItem>();

            var tolerance = volumeTransform.SpacingZ / 2;

            // Iterate through the sets of contours per slice
            foreach (var tuple in axialContours)
            {
                // For this slice, compute the z-axis coordinate in the DICOM reference coordinate system 
                var z = tuple.Key;
                var zAxisPhysicalCoordinate = (volumeTransform.DataToDicom * new Point3D(0, 0, z)).Z;

                // Locate the DICOM identifier containing this slice. Note that for non-axial volumes this is not the right test
                // but is sufficient for Axial volumes accepted in the FDA version. 
                var identifier = identifiers.FirstOrDefault(x => Math.Abs(x.Image.ImagePositionPatient.Z - zAxisPhysicalCoordinate) < tolerance);

                if (identifier == null)
                {
                    throw new Exception("Invalid contour or image identifiers");
                }

                // Reference the slice containing these contours by SopInstance UID
                var sopCommonInstance = identifier.Image.SopCommon;
                var contourImageSeq = new List<DicomRTContourImageItem>()
                {
                    new DicomRTContourImageItem(sopCommonInstance.SopClassUid, sopCommonInstance.SopInstanceUid)
                };

                // Iterate through the contour objects on this slice. 
                foreach (var contour in tuple.Value)
                {
                    // Convert the pixel based contour into the DICOM reference coordinate system and flatten to an arry of doubles.
                    var allpoints = contour.ContourPoints.SelectMany(
                        p => (volumeTransform.DataToDicom * new Point3D(p.X, p.Y, z)).Data).ToArray();
                    
                    resultList.Add(new DicomRTContourItem(
                        allpoints,
                        contour.Length,
                        ClosedPlanarString,
                        contourImageSeq));
                }
            }

            return resultList;
        }

        /// <summary>
        /// Return true if and only if the DicomDataset has the RT Structure set SOPClassUID
        /// </summary>
        /// <param name="dcmset"></param>
        /// <returns></returns>
        public static bool IsRTStructure(this DicomDataset dcmset)
        {
            return dcmset.GetSingleValueOrDefault(DicomTag.SOPClassUID, EmptyUid) == DicomUID.RTStructureSetStorage;
        }

        /// <summary>
        /// Returns an empty DICOM uid.
        /// </summary>
        public static DicomUID EmptyUid
        {
            get { return new DicomUID(string.Empty, string.Empty, DicomUidType.Unknown); }
        }
    }
}