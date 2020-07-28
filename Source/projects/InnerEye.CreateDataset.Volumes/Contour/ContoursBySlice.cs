///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    [Serializable]
    // Threadsafe from outside
    public class ContoursBySlice : IEnumerable<KeyValuePair<int, IList<Contour>>>, INotifyCollectionChanged
    {
        private readonly IDictionary<int, IList<Contour>> _contoursBySliceDictionary;

        private readonly object _lock = new object();

        public ContoursBySlice()
        {
            _contoursBySliceDictionary = new Dictionary<int, IList<Contour>>();
        }

        public ContoursBySlice(IDictionary<int, IList<Contour>> contours)
        {
            _contoursBySliceDictionary = contours;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IList<int> GetSlicesWithContours()
        {
            lock(_lock)
            {
                return _contoursBySliceDictionary
                    .Where(x => x.Value.Any(c => c.ContourPoints.Length > 0))
                    .Select(x => x.Key)
                    .ToList();
            }
        }

        public IList<Contour> ContoursForSlice(int index)
        {
            lock (_lock)
            {
                return _contoursBySliceDictionary[index];
            }
        }

        public IList<Contour> TryGetContoursForSlice(int index)
        {
            lock (_lock)
            {
                return _contoursBySliceDictionary.ContainsKey(index) ? ContoursForSlice(index) : null;
            }
        }

        public int SlicesCount
        {
            get
            {
                lock (_lock)
                {
                    return 
                        _contoursBySliceDictionary.Count(
                            x => x.Value.Any(
                                c => c.ContourPoints.Length > 0));
                }
            }
        }

        public IEnumerator<KeyValuePair<int, IList<Contour>>> GetEnumerator()
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

        public void Replace(ContoursBySlice newContours)
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

                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void Append(ContoursBySlice newContours)
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

                CollectionChanged?.Invoke(this,
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
    }
}
