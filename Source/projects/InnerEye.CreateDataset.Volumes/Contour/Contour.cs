///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Windows;
    using System.Linq;

    [Obsolete("All contour-related code should move to using the new classes in the InnerEye.CreateDataset.Contours namespace.")]
    [Serializable]
    public struct Contour : IEquatable<Contour>
    {
        public Contour(Point[] contourPoints, int regionAreaPixels)
        {
            ContourPoints = contourPoints;
            RegionAreaPixels = regionAreaPixels;
        }

        public Point[] ContourPoints { get; }

        public int RegionAreaPixels { get; }

        public bool Equals(Contour other)
        {
            if (ContourPoints.Length != other.ContourPoints?.Length)
            {
                return false;
            }

            return !ContourPoints.Where((t, i) => t != other.ContourPoints[i]).Any();
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals((Contour)obj);
        }

        public static bool operator ==(Contour c1, Contour c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(Contour c1, Contour c2)
        {
            return !c1.Equals(c2);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                return ContourPoints.Aggregate(19, (current, foo) => current * 31 + foo.GetHashCode());
            }
        }

        public object ToList()
        {
            throw new NotImplementedException();
        }
    }
}