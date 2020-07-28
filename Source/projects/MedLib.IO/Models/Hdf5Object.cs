///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Models
{
    using InnerEye.CreateDataset.Volumes;

    public class Hdf5Object
    {
        public Hdf5Object(MedicalVolume volume, Volume3D<byte> segmentation)
        {

            Volume = volume;
            Segmentation = segmentation;
        }
        public MedicalVolume Volume {get; set;}
        public Volume3D<byte> Segmentation { get; set; }

    }
}
