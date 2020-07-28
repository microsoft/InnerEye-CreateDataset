///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Threading.Tasks;

    public class Volume2D<T> : Volume<T>
    {
        public Volume2D(int dimX, int dimY, double spacingX, double spacingY, Point2D origin, Matrix2 direction)
                : this(new T[dimX * dimY], dimX, dimY, spacingX, spacingY, origin, direction)
        {
        }

        public Volume2D(T[] array, int dimX, int dimY, double spacingX, double spacingY, Point2D origin, Matrix2 direction)
                : base(array, 2)
        {
            DimX = dimX;
            DimY = dimY;

            DimXY = dimX * dimY;

            SpacingX = spacingX;
            SpacingY = spacingY;

            Origin = origin;
            Direction = direction;

            Matrix2 pixelToPhysicalMatrix;
            Matrix2 physicalToPixelMatrix;

            ComputePixelToPhysicalMatrices(direction, out pixelToPhysicalMatrix, out physicalToPixelMatrix);

            PixelToPhysicalMatrix = pixelToPhysicalMatrix;
            PhysicalToPixelMatrix = physicalToPixelMatrix;

            var roundedData = new double[direction.Data.Length];

            for (var i = 0; i < direction.Data.Length; i++)
            {
                roundedData[i] = Math.Round(direction.Data[i]);
            }

            RoundedDirection = new Matrix2(roundedData);
            InverseRoundedDirection = RoundedDirection.Inverse();
        }

        public int DimX { get; }

        public int DimY { get; }

        public int DimXY { get; }

        public double SpacingX { get; }

        public double SpacingY { get; }

        public Point2D Origin { get; }

        public Matrix2 Direction { get; }

        public Matrix2 RoundedDirection { get; }

        public Matrix2 InverseRoundedDirection { get; }

        public Matrix2 PixelToPhysicalMatrix { get; }

        public Matrix2 PhysicalToPixelMatrix { get; }

        public int GetIndex(int x, int y) => x + y * DimX;

        /// <summary>
        /// Gets the (X, Y) coordinates of a point from a given linear array index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public (int X, int Y) GetCoordinates(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index value {index} is out of bounds for volume of length {Length}"); ;
            }

            var y = Math.DivRem(index, DimX, out var x);
            return (x, y);
        }

        public T this[int x, int y]
        {
            get { return base[GetIndex(x, y)]; }
            set { base[GetIndex(x, y)] = value; }
        }

        public static Volume2D<T> CreateFromVolume<TK>(Volume2D<TK> volume)
        {
            return new Volume2D<T>(new T[volume.Length], volume.DimX, volume.DimY, volume.SpacingX, volume.SpacingY, volume.Origin, volume.Direction);
        }

        public static bool operator ==(Volume2D<T> a, Volume2D<T> b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }

            if ((object)a == null || (object)b == null || a.DimX != b.DimX || a.DimY != b.DimY)
            {
                return false;
            }

            var result = true;

            Parallel.For(0, a.DimY, delegate (int y)
            {
                for (var x = 0; x < a.DimX; x++)
                {
                    if (!a[x, y].Equals(b[x, y]))
                    {
                        result = false;
                    }
                }
            });

            return result;
        }

        public static bool operator !=(Volume2D<T> a, Volume2D<T> b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Volume2D<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DimX;
                hashCode = (hashCode * 397) ^ DimY;
                hashCode = (hashCode * 397) ^ DimXY;
                hashCode = (hashCode * 397) ^ SpacingX.GetHashCode();
                hashCode = (hashCode * 397) ^ SpacingY.GetHashCode();
                hashCode = (hashCode * 397) ^ Origin.GetHashCode();
                hashCode = (hashCode * 397) ^ (Direction?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (PixelToPhysicalMatrix?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (PhysicalToPixelMatrix?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public bool TryGetIndex(int x, int y, out int index)
        {
            index = GetIndex(x, y);
            return IsValid(x, y);
        }

        public bool IsValid(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return false;
            }

            return x < DimX && y < DimY;
        }

        public Point2D PhysicalToPixel(Point2D physical)
        {
            return PhysicalToPixelMatrix * (physical - Origin);
        }

        public Point2D PixelToPhysical(Point2D pixelPoint)
        {
            return PixelToPhysicalMatrix * pixelPoint + Origin;
        }

        protected bool Equals(Volume2D<T> other)
        {
            return DimX == other.DimX && DimY == other.DimY && DimXY == other.DimXY && SpacingX.Equals(other.SpacingX) && SpacingY.Equals(other.SpacingY) && Origin.Equals(other.Origin) && Equals(Direction, other.Direction) && Equals(PixelToPhysicalMatrix, other.PixelToPhysicalMatrix) && Equals(PhysicalToPixelMatrix, other.PhysicalToPixelMatrix);
        }

        private void ComputePixelToPhysicalMatrices(Matrix2 direction, out Matrix2 pixelToPhysicalMatrix, out Matrix2 physicalToPixelMatrix)
        {
            pixelToPhysicalMatrix = new Matrix2(direction);

            var spacings = new[] { SpacingX, SpacingY };

            for (var i = 0; i < 4; i++)
            {
                pixelToPhysicalMatrix.Data[i] *= spacings[i / 2];
            }

            physicalToPixelMatrix = pixelToPhysicalMatrix.Inverse();
        }

        /// <summary>
        /// Creates a <see cref="Volume2D{U}"/> instance that has the same size as the present
        /// object, but with a newly created (not initialized) data array.
        /// </summary>
        public Volume2D<U> CreateSameSize<U>() => new Volume2D<U>(DimX, DimY, SpacingX, SpacingY, Origin, Direction);
    }
}