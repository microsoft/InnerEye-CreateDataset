///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    /// <summary>
    /// Describes the different ways how a contour can be smoothed.
    /// </summary>
    public enum ContourSmoothingType
    {
        /// <summary>
        /// The contour is not smoothed, and traces the outside of the pixels.
        /// Pixels are drawn with their centers at integer coordinates, the contour will
        /// hence run as lines in between the integer coordinates.
        /// </summary>
        None,

        /// <summary>
        /// The contour is first tracing pixel outsides, and then corners are smoothed.
        /// </summary>
        Small
    }
}
