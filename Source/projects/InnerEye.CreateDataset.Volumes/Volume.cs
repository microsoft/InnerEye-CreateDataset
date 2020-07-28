///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;

    [Serializable]
    public abstract class Volume<T>
    {
        private readonly T[] _array;

        protected Volume(T[] array, int dimensions)
        {
            _array = array;
            Dimensions = dimensions;
        }

        /// <summary>
        /// Gets the underlying array for this Volume.
        /// </summary>
        public T[] Array => _array;

        /// <summary>
        /// Gets the number of dimensions for this volume. 
        /// Example: Volume2D will have 2 dimensions.
        /// </summary>
        public int Dimensions { get; }

        /// <summary>
        /// Gets the length of the array that holds all voxels.
        /// </summary>
        public int Length => _array.Length;

        public T this[int index]
        {
            get { return _array[index]; }

            set { _array[index] = value; }
        }
    }
}