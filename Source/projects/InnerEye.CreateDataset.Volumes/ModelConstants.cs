///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Volumes
{
    public class ModelConstants
    {
        /// <summary>
        /// The voxel value in a binary mask that represents foreground.
        /// </summary>
        public const byte MaskForegroundIntensity = 1;

        /// <summary>
        /// The voxel value in a binary mask that represents background.
        /// </summary>
        public const byte MaskBackgroundIntensity = 0;
    }
}
