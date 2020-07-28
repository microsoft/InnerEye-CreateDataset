///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    public class Region2D<T>
    {
        public Region2D(T minimumX, T minimumY, T maximumX, T maximumY)
        {
            MinimumX = minimumX;
            MinimumY = minimumY;
            MaximumX = maximumX;
            MaximumY = maximumY;
        }
        
        public T MinimumX { get; }
        
        public T MinimumY { get; }
        
        public T MaximumX { get; }
        
        public T MaximumY { get; }
    }
}