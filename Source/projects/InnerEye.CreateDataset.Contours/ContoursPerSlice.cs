///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// Stores a contour for slices of a volume, indexed by slice number.
    /// </summary>
    // Threadsafe from outside
    [Serializable]
    public class ContoursPerSlice : IEnumerable<KeyValuePair<int, IReadOnlyList<ContourPolygon>>>, INotifyCollectionChanged
    {
        private readonly IDictionary<int, IReadOnlyList<ContourPolygon>> _contoursBySliceDictionary;

        private readonly object _lock = new object();

        public ContoursPerSlice()
        {
            _contoursBySliceDictionary = new Dictionary<int, IReadOnlyList<ContourPolygon>>();
        }

        public ContoursPerSlice(IDictionary<int, IReadOnlyList<ContourPolygon>> contours)
        {
            _contoursBySliceDictionary = contours;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets the number of slices that have contours with non-empty sets of points.
        /// </summary>
        public int SliceCount
        {
            get
            {
                lock (_lock)
                {
                    return
                        _contoursBySliceDictionary.Count(
                            x => x.Value.Any(
                                c => c.Length > 0));
                }
            }
        }

        public IReadOnlyList<int> GetSlicesWithContours()
        {
            lock (_lock)
            {
                return _contoursBySliceDictionary
                    .Where(x => x.Value.Any(c => c.Length > 0))
                    .Select(x => x.Key)
                    .ToList();
            }
        }

        public IReadOnlyList<ContourPolygon> ContoursForSlice(int index)
        {
            lock (_lock)
            {
                return _contoursBySliceDictionary[index];
            }
        }

        public IReadOnlyList<ContourPolygon> TryGetContoursForSlice(int index)
        {
            lock (_lock)
            {
                return _contoursBySliceDictionary.ContainsKey(index) ? ContoursForSlice(index) : null;
            }
        }

        public IEnumerator<KeyValuePair<int, IReadOnlyList<ContourPolygon>>> GetEnumerator()
        {
            lock (_lock)
            {
                return _contoursBySliceDictionary.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lock)
            {
                return GetEnumerator();
            }
        }

        public bool ContainsKey(int sliceIndex)
        {
            lock (_lock)
            {
                return _contoursBySliceDictionary.ContainsKey(sliceIndex);
            }
        }

        public void Replace(ContoursPerSlice newContours)
        {
            lock (_lock)
            {
                _contoursBySliceDictionary.Clear();
                if (newContours != null)
                {
                    foreach (var newContour in newContours)
                    {
                        if (newContour.Value.Count > 0)
                        {
                            _contoursBySliceDictionary[newContour.Key] = newContour.Value;
                        }
                        else
                        {
                            _contoursBySliceDictionary.Remove(newContour.Key);
                        }
                    }
                }

                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void Append(ContoursPerSlice newContours)
        {
            lock (_lock)
            {
                if (newContours == null || !newContours.Any())
                {
                    Clear();
                    return;
                }

                foreach (var newContour in newContours)
                {
                    if (newContour.Value.Count > 0)
                    {
                        _contoursBySliceDictionary[newContour.Key] = newContour.Value;
                    }
                    else
                    {
                        _contoursBySliceDictionary.Remove(newContour.Key);
                    }
                }

                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _contoursBySliceDictionary.Clear();
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Gets the minimum and maximum slice index in the contour collection.
        /// </summary>
        /// <returns>The minimum and maximum slices.</returns>
        public (int Min, int Max) GetMinMaxSlices()
        {
            var min = int.MaxValue;
            var max = int.MinValue;
            lock (_lock)
            {
                if (!this.Any())
                {
                    throw new InvalidOperationException("Can't extract minimum and maximum because no contours are stored.");
                }

                foreach (var contour in _contoursBySliceDictionary)
                {
                    var sliceIndex = contour.Key;
                    if (sliceIndex < min)
                    {
                        min = sliceIndex;
                    }

                    if (sliceIndex > max)
                    {
                        max = sliceIndex;
                    }
                }
            }

            return (min, max);
        }
    }
}
