///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Math
{
    using System.Threading.Tasks;
    using InnerEye.CreateDataset.Volumes;
    using InnerEye.CreateDataset.Contours;

    public static class ContourGeometryOperations
    {
        public static Volume3D<byte> GeometryUnion(this ContoursPerSlice contour1, ContoursPerSlice contour2, Volume3D<short> parentVolume)
        {
            var volume1 = contour1.ToVolume3D(parentVolume);
            var volume2 = contour2.ToVolume3D(parentVolume);

            Parallel.For(0, volume1.Length, i =>
            {
                if (volume2[i] > 0)
                {
                    volume1[i] = ModelConstants.MaskForegroundIntensity;
                }
            });

            return volume1;
        }

        public static Volume3D<byte> GeometryIntersect(this ContoursPerSlice contour1, ContoursPerSlice contour2, Volume3D<short> parentVolume)
        {
            var volume1 = contour1.ToVolume3D(parentVolume);
            var volume2 = contour2.ToVolume3D(parentVolume);

            Parallel.For(0, volume1.Length, i =>
            {
                volume1[i] = volume1[i] > 0 && volume2[i] > 0 ? ModelConstants.MaskForegroundIntensity : ModelConstants.MaskBackgroundIntensity;
            });

            return volume1;
        }

        public static Volume3D<byte> GeometryExclude(this ContoursPerSlice contour1, ContoursPerSlice contour2, Volume3D<short> parentVolume)
        {
            var volume1 = contour1.ToVolume3D(parentVolume);
            var volume2 = contour2.ToVolume3D(parentVolume);

            Parallel.For(0, volume1.Length, i =>
            {
                volume1[i] = volume1[i] > 0 && volume2[i] == 0 ? ModelConstants.MaskForegroundIntensity : ModelConstants.MaskBackgroundIntensity;
            });

            return volume1;
        }
    }
}