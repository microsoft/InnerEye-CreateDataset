///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Collections.Generic;

namespace InnerEye.CreateDataset.Volumes
{
    /// <summary>
    /// Represents a region a 3-dimensional space, given by the minimum and maximum values
    /// along each coordinate. Minimum and maximum are meant to be inclusive.
    /// </summary>
    [DebuggerDisplay("({MinimumX}, {MaximumX}) ({MinimumY}, {MaximumY}) ({MinimumZ}, {MaximumZ})")]
    public class Region3D<T>
    {
        /// <summary>
        /// Creates a new instance of Region3D, with all properties set to the respective arguments.
        /// </summary>
        /// <param name="minimumX"></param>
        /// <param name="minimumY"></param>
        /// <param name="minimumZ"></param>
        /// <param name="maximumX"></param>
        /// <param name="maximumY"></param>
        /// <param name="maximumZ"></param>
        public Region3D(T minimumX, T minimumY, T minimumZ, T maximumX, T maximumY, T maximumZ)
        {
            MinimumX = minimumX;
            MinimumY = minimumY;
            MinimumZ = minimumZ;
            MaximumX = maximumX;
            MaximumY = maximumY;
            MaximumZ = maximumZ;
        }

        public T MinimumX { get; }

        public T MinimumY { get; }

        public T MinimumZ { get; }

        public T MaximumX { get; }

        public T MaximumY { get; }

        public T MaximumZ { get; }

        /// <summary>
        /// Creates a full copy of the present object.
        /// </summary>
        /// <returns></returns>
        public Region3D<T> Clone()
        {
            return new Region3D<T>(MinimumX, MinimumY, MinimumZ, MaximumX, MaximumY, MaximumZ);
        }

        public override bool Equals(object obj)
        {
            var d = obj as Region3D<T>;
            var comparator = EqualityComparer<T>.Default;
            return d != null &&
                   comparator.Equals(MinimumX, d.MinimumX) &&
                   comparator.Equals(MinimumY, d.MinimumY) &&
                   comparator.Equals(MinimumZ, d.MinimumZ) &&
                   comparator.Equals(MaximumX, d.MaximumX) &&
                   comparator.Equals(MaximumY, d.MaximumY) &&
                   comparator.Equals(MaximumZ, d.MaximumZ);
        }

        public override int GetHashCode()
        {
            var hashCode = -436642110;
            var prime = -1521134295;
            var comparator = EqualityComparer<T>.Default;
            hashCode = hashCode * prime + comparator.GetHashCode(MinimumX);
            hashCode = hashCode * prime + comparator.GetHashCode(MinimumY);
            hashCode = hashCode * prime + comparator.GetHashCode(MinimumZ);
            hashCode = hashCode * prime + comparator.GetHashCode(MaximumX);
            hashCode = hashCode * prime + comparator.GetHashCode(MaximumY);
            hashCode = hashCode * prime + comparator.GetHashCode(MaximumZ);
            return hashCode;
        }

        /// <summary>
        /// Creates a copy of the present object, and overwrites the minimum and maximum Z 
        /// values with the arguments.
        /// </summary>
        /// <param name="minimumZ"></param>
        /// <param name="maximumZ"></param>
        /// <returns></returns>
        public Region3D<T> OverrideZ(T minimumZ, T maximumZ)
        {
            return new Region3D<T>(MinimumX, MinimumY, minimumZ, MaximumX, MaximumY, maximumZ);
        }

        public override string ToString()
        {
            return $"({MinimumX}, {MaximumX}) ({MinimumY}, {MaximumY}) ({MinimumZ}, {MaximumZ})";
        }
    }
}