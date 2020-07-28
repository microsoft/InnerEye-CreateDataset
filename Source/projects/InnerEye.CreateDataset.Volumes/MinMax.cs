///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    /// <summary>
    /// Stores a tuple of (minimum, maximum).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct MinMax<T>
    {
        public T Minimum { get; set; }
        
        public T Maximum { get; set; }
    }

    public static class MinMax
    {
        public static MinMax<T> Create<T>(T min, T max)
        {
            return new MinMax<T> { Minimum = min, Maximum = max };
        }
    }
}