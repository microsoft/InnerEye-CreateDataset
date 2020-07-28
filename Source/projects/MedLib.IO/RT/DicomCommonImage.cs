///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using Dicom;
using InnerEye.CreateDataset.Volumes;
using System.Collections.Generic;

namespace MedLib.IO.RT
{
    /// <summary>
    /// An aggregation and extension of important tags from the following DICOM IOD modules:
    ///  * General Image Module
    ///  * Image Plane Module
    ///  * Image Pixel Module  (partial)
    ///  * CT Image Module
    ///  * MR Image Module
    ///  Note that this class does not represent an IOD module itself, rather the common tags 
    ///  we need to operate with CT and MR images. 
    /// </summary>
    public class DicomCommonImage
    {
        /// <summary>
        /// SOP common instance information for this image
        /// </summary>
        public DicomSOPCommon SopCommon { get; }

        /// <summary>
        /// The position in the DICOM reference coordinate system of the center of the 
        /// first pixel in this image. Type 1
        /// </summary>
        public Point3D ImagePositionPatient { get; }

        /// <summary>
        /// The pixel size in mm. Type 1
        /// </summary>
        public Point2D PixelSpacing { get; }

        /// <summary>
        /// An optional and unreliable measure of slice location in mm. Type 3 
        /// *do not use*
        /// </summary>
        public double SliceLocation { get; }

        /// <summary>
        /// The number of Rows (or Height) of this image in pixels. Type 1
        /// </summary>
        public int Rows { get; }

        /// <summary>
        /// The number of columns (or Width) of this image in pixels. Type 1
        /// </summary>
        public int Columns { get; }

        /// <summary>
        /// Image identification characteristics (0008,0008), Type 1
        /// </summary>
        public IReadOnlyList<string> ImageType { get; }

        /// <summary>
        /// The first (if any) value for the first Window/Level center defined for this image (0 otherwise)
        /// Type 1C, VR DS (decimal string)
        /// </summary>
        public double WindowCenter { get; }

        /// <summary>
        /// The first (if any) value for the first Window/Level width defined for this image (0 otherwise)
        /// Type 1C, VR DS (decimal string)
        /// </summary>
        public double WindowWidth { get; }

        /// <summary>
        /// Read properties from the given DicomDataset, throwing if Type 1 parameters are not present. 
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static DicomCommonImage Read(DicomDataset ds)
        {
            var sopCommon = DicomSOPCommon.Read(ds);
            //throw
            var position = ds.GetValues<double>(DicomTag.ImagePositionPatient);
            var pixelSpacing = ds.GetValues<double>(DicomTag.PixelSpacing);
            var rows = ds.GetSingleValue<int>(DicomTag.Rows);
            var columns = ds.GetSingleValue<int>(DicomTag.Columns);
            var imageType = ds.GetValues<string>(DicomTag.ImageType);

            var pPosition = new Point3D(position);
            // Note that DICOM encodes this as (Y,X)!!!
            var pSpacing = new Point2D(pixelSpacing[1], pixelSpacing[0]);

            // no throw
            var location = ds.GetSingleValueOrDefault(DicomTag.SliceLocation, 0d);
            var windowCenter = ds.GetSingleValueOrDefault(DicomTag.WindowCenter, 0d);
            var windowWidth = ds.GetSingleValueOrDefault(DicomTag.WindowWidth, 0d);

            return new DicomCommonImage(sopCommon, pPosition, pSpacing, location, imageType, rows, columns, windowCenter, windowWidth);
        }

        /// <summary>
        /// Returns an empty DicomCommonImage instance. 
        /// </summary>
        /// <returns></returns>
        public static DicomCommonImage CreateEmpty()
        {
            return new DicomCommonImage(
                DicomSOPCommon.CreateEmpty(), new Point3D(), new Point2D(), 0, new string[0], 0, 0, 0, 0);
        }


        private DicomCommonImage(
            DicomSOPCommon sopCommon, Point3D ipp, Point2D pixelSpacing, double location, IReadOnlyList<string> imageType, int rows, int columns, double wCenter, double wWidth
        )
        {
            SopCommon = sopCommon;
            ImagePositionPatient = ipp;
            PixelSpacing = pixelSpacing;
            SliceLocation = location;
            ImageType = imageType;
            Rows = rows;
            Columns = columns;
            WindowCenter = wCenter;
            WindowWidth = wWidth; 
        }
    }
}
