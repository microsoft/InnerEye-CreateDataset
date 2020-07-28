///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Volumes
{
    using System;

    /// <summary>
    /// This class wraps the Volume2D so it cannot be written to the array. No data is copied or duplicated
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReadOnlyVolume2D<T> : Volume2D<T>
    {
        public ReadOnlyVolume2D(Volume2D<T> volume2d)
                : base(volume2d.Array, volume2d.DimX, volume2d.DimY, volume2d.SpacingX, volume2d.SpacingY, volume2d.Origin, volume2d.Direction)
        {
        }

        public new T this[int index]
        {
            get { return base[index]; }

            set { throw new InvalidOperationException($"{index} {value}"); }
        }
    }
}
