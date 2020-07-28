///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "()}")]
    [Serializable]
    public class Volume3D<T> : Volume<T>
    {
        public Volume3D(T[] array, int dimX, int dimY, int dimZ, double spacingX, double spacingY, double spacingZ, Point3D origin, Matrix3 direction)
                : base(array, 3)
        {
            DimX = dimX;
            DimY = dimY;
            DimZ = dimZ;

            DimXY = dimX * dimY;

            if (array?.Length != DimXY * dimZ)
            {
                throw new ArgumentException();
            }

            SpacingX = spacingX;
            SpacingY = spacingY;
            SpacingZ = spacingZ;

            Origin = origin;
            Direction = direction;

            Transform = new VolumeTransform(spacingX, spacingY, spacingZ, origin, direction);
        }


        public Volume3D(int dimX, int dimY, int dimZ, double spacingX, double spacingY, double spacingZ, Point3D origin, Matrix3 direction)
                : this(new T[dimX * dimY * dimZ], dimX, dimY, dimZ, spacingX, spacingY, spacingZ, origin, direction)
        {
        }


        public Volume3D(int dimX, int dimY, int dimZ, double spacingX, double spacingY, double spacingZ)
                : this(new T[dimX * dimY * dimZ], dimX, dimY, dimZ, spacingX, spacingY, spacingZ, new Point3D(), Matrix3.CreateIdentity())
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="Volume3D{T}"/> with the given dimensions. The spacing is set
        /// to 1.0, origin to 0, direction is identity.
        /// </summary>
        /// <param name="dimX"></param>
        /// <param name="dimY"></param>
        /// <param name="dimZ"></param>
        public Volume3D(int dimX, int dimY, int dimZ)
                : this(new T[dimX * dimY * dimZ], dimX, dimY, dimZ, 1.0, 1.0, 1.0, new Point3D(), Matrix3.CreateIdentity())
        {
        }

        public Volume3D(T[] array, int dimX, int dimY, int dimZ, double spacingX = 1.0, double spacingY = 1.0, double spacingZ = 1.0)
                : this(array, dimX, dimY, dimZ, spacingX, spacingY, spacingZ, new Point3D(), Matrix3.CreateIdentity())
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
        }

        /// <summary>
        /// Gets the X-dimension length.
        /// </summary>
        /// <value>
        /// The X-dimension length.
        /// </value>
        public int DimX { get; }

        /// <summary>
        /// Gets the Y-dimension length.
        /// </summary>
        /// <value>
        /// The Y-dimension length.
        /// </value>
        public int DimY { get; }

        /// <summary>
        /// Gets the Z-dimension length.
        /// </summary>
        /// <value>
        /// The Z-dimension length.
        /// </value>
        public int DimZ { get; }

        /// <summary>
        /// Gets the pre-multipled DimX * DimY.
        /// </summary>
        /// <value>
        /// The pre-multipled DimX * DimY.
        /// </value>
        public int DimXY { get; }

        /// <summary>
        /// Gets the X-dimension pixel spacing.
        /// </summary>
        /// <value>
        /// The X-dimension pixel spacing.
        /// </value>
        public double SpacingX { get; }

        /// <summary>
        /// Gets the Y-dimension pixel spacing.
        /// </summary>
        /// <value>
        /// The Y-dimension pixel spacing.
        /// </value>
        public double SpacingY { get; }

        /// <summary>
        /// Gets the Z-dimension pixel spacing.
        /// </summary>
        /// <value>
        /// The Z-dimension pixel spacing.
        /// </value>
        public double SpacingZ { get; }

        /// <summary>
        /// Gets the volume of an individual voxel: Product of spacing along the 3 dimensions.
        /// </summary>
        public double VoxelVolume => SpacingX * SpacingY * SpacingZ;

        /// <summary>
        /// Gets the origin.
        /// </summary>
        /// <value>
        /// The origin.
        /// </value>
        public Point3D Origin { get; }

        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>
        /// The direction.
        /// </value>
        public Matrix3 Direction { get; }

        /// <summary>
        /// Returns the transform that maps voxel coordinates into the reference coordinate system
        /// </summary>
        /// <value>
        /// The transform.
        /// </value>
        public VolumeTransform Transform { get; }

        /// <summary>
        /// Gets the index position for the X, Y, Z location.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="z">The Z position.</param>
        /// <returns>The index.</returns>
        public int GetIndex(int x, int y, int z) => x + y * DimX + z * DimXY;

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>The copied instance.</returns>
        public Volume3D<T> Copy() => new Volume3D<T>(Array.ToArray(), DimX, DimY, DimZ, SpacingX, SpacingY, SpacingZ, Origin, Direction);

        /// <summary>
        /// Creates a <see cref="Volume3D{U}"/> instance that has the same size as the present
        /// object, but with a newly created (not initialized) data array.
        /// </summary>
        public Volume3D<U> CreateSameSize<U>() => new Volume3D<U>(DimX, DimY, DimZ, SpacingX, SpacingY, SpacingZ, Origin, Direction);

        /// <summary>
        /// Creates a <see cref="Volume3D{T}"/> instance that has the same size as the present
        /// object. The data buffer will be filled with the given constant.
        /// </summary>
        public Volume3D<U> CreateSameSize<U>(U value)
        {
            var result = CreateSameSize<U>();
            result.Fill(value);
            return result;
        }

        /// <summary>
        /// Overwrites all voxel data in the present object with the given constant.
        /// </summary>
        /// <param name="value"></param>
        public void Fill(T value)
        {
            for (int i = 0; i < Array.Length; i++)
            {
                this[i] = value;
            }
        }

        /// <summary>
        /// Gets the index for the X, Y, Z position and validates that it exists within this volume.
        /// </summary>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="z">The Z position.</param>
        /// <param name="index">The index.</param>
        /// <returns>If the index is valid.</returns>
        public bool TryGetIndex(int x, int y, int z, out int index)
        {
            index = GetIndex(x, y, z);
            return IsValid(x, y, z);
        }

        /// <summary>
        /// Returns true if the voxel coordinate (x,y,z) is within the volume bounds.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool IsValid(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0)
            {
                return false;
            }

            return x < DimX && y < DimY && z < DimZ;
        }

        /// <summary>
        /// Gets or sets the <see cref="T"/> with the specified X, Y, Z position.
        /// </summary>
        /// <value>
        /// The <see cref="T"/>.
        /// </value>
        /// <param name="x">The X position.</param>
        /// <param name="y">The Y position.</param>
        /// <param name="z">The Z position.</param>
        /// <returns></returns>
        public T this[int x, int y, int z]
        {
            get { return this[GetIndex(x, y, z)]; }
            set { this[GetIndex(x, y, z)] = value; }
        }

        /// <summary>
        /// Gets whether a voxel at the given coordinates is on the boundary of the 
        /// volume on any side.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool IsEdgeVoxel(int x, int y, int z)
        {
            // x,y,z = 0 are the bottom voxels in each dimension and x,y,z = dim - 1 are the top voxels in each dimension
            return x == 0 || x == DimX - 1 || y == 0 || y == DimY - 1 || z == 0 || z == DimZ - 1;
        }

        /// <summary>
        /// Creates a short human readable string that will be displayed in the VS debugger.
        /// </summary>
        /// <returns></returns>
        public string DebuggerDisplay()
        {
            return $"Size {DimX} x {DimY} x {DimZ}, spacing {SpacingX:0.00} x {SpacingY:0.00} x {SpacingZ:0.00}";
        }
    }
}