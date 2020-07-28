///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Volumes
{
    using System;
    /// <summary>
    /// This class wraps the Volume3D so it cannot be written to the array. No data is copied or duplicated
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReadOnlyVolume3D<T> : Volume3D<T>
    {
        public ReadOnlyVolume3D(Volume3D<T> volume3D)
                : base(volume3D.Array, volume3D.DimX, volume3D.DimY, volume3D.DimZ, volume3D.SpacingX, volume3D.SpacingY, volume3D.SpacingZ, volume3D.Origin, volume3D.Direction)
        {
        }

        public new T this[int index]
        {
            get { return base[index]; }

            set { throw new InvalidOperationException($"{index} {value}"); }
        }
    }
}
