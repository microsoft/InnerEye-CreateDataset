///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO
{
    using System;
    using InnerEye.CreateDataset.Contours;

    /// <summary>
    /// Contains a segmentation as a contour, and information about how it should be rendered within a Dicom file.
    /// </summary>
    public class ContourRenderingInformation
    {
        /// <summary>
        /// Creates a new instance of the class, setting all properties that the class holds.
        /// </summary>
        /// <param name="name">The name of the anatomical structure that is represented by the contour.</param>
        /// <param name="color">The color that should be used to render the contour.</param>
        /// <param name="contour">The contours broken down by slice of the scan.</param>
        /// <exception cref="ArgumentNullException">The contour name or mask was null.</exception>
        public ContourRenderingInformation(string name, RGBValue color, ContoursPerSlice contour)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Color = color;
            Contour = contour ?? throw new ArgumentNullException(nameof(contour));
        }

        /// <summary>
        /// Creates a new instance of the class, from a binary mask. The contour is extracted from the mask
        /// using the default settings: Background 0, foreground 1, axial slices.
        /// </summary>
        /// <param name="name">The name of the anatomical structure that is represented by the contour.</param>
        /// <param name="color">The color that should be used to render the contour.</param>
        /// <param name="mask">The binary mask that represents the anatomical structure.</param>
        /// <exception cref="ArgumentNullException">The contour name or mask was null.</exception>
        public ContourRenderingInformation(string name, RGBValue color, InnerEye.CreateDataset.Volumes.Volume3D<byte> mask)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Color = color;
            mask = mask ?? throw new ArgumentNullException(nameof(mask));
            Contour = ExtractContours.ContoursWithHolesPerSlice(mask);
        }

        /// <summary>
        /// The name of the anatomical structure that is represented by the mask.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The color that should be used to render the anatomical structure.
        /// </summary>
        public RGBValue Color { get; }

        /// <summary>
        /// The segmentation as a contour by slice.
        /// </summary>
        public ContoursPerSlice Contour { get; }
    }
}
