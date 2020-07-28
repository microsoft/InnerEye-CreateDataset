///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    /// <summary>
    /// The slice type.
    /// More info: https://en.wikipedia.org/wiki/Anatomical_plane
    /// </summary>
    public enum SliceType
    {
        /// <summary>
        /// The axial XY plane.
        /// </summary>
        Axial,

        /// <summary>
        /// The coronal XZ plane.
        /// </summary>
        Coronal,

        /// <summary>
        /// The sagittal YZ plane.
        /// </summary>
        Sagittal,
    }
}
