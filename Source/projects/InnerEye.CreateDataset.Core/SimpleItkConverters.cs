///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    using System;
    using System.Runtime.InteropServices;
    using itk.simple;
    using InnerEye.CreateDataset.Volumes;

    /// <summary>
    /// Holds an ITK image, and the buffer that was used to create it when initializing the image from
    /// a managed <see cref="Volume3D{T}"/>. The class ensures that the memory is freed at the end of the object's lifetime.
    /// </summary>
    public class ItkImageFromManaged : IDisposable
    {
        /// <summary>
        /// Creates a new instance of the class, by cloning the given array and storing a pinned
        /// handle to the memory in the <see cref="Handle"/> property.
        /// </summary>
        /// <param name="array"></param>
        private ItkImageFromManaged(Array array)
        {
            Handle = GCHandle.Alloc(array.Clone(), GCHandleType.Pinned);
        }

        /// <summary>
        /// Gets the pinned handle to the image buffer that the present object stores. The handle can be null
        /// after calling dispose on the object.
        /// </summary>
        private GCHandle? Handle { get; set; }

        /// <summary>
        /// Gets the ITK image that is stored in the present object.
        /// </summary>
        public Image Image { get; private set; }

        /// <summary>
        /// Frees the memory that has been allocated for the image, and the ITK image itself.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees the memory that has been allocated for the image, and the ITK image itself.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "disposing")]
        protected virtual void Dispose(bool disposing)
        {
            if (Handle != null)
            {
                Handle.Value.Free();
                Handle = null;
            }

            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }
        }

        /// <summary>
        /// Converts a volume to the corresponding SimpleITK image,
        /// preserving all voxel values and transformations. The resulting image 
        /// will have pixel type <see cref="PixelIDValueEnum.sitkUInt8"/>.
        /// </summary>
        /// <param name="volume">The volume that should be converted to an ITK image.</param>
        /// <returns></returns>

        public static ItkImageFromManaged FromVolume(Volume3D<byte> volume)
        {
            return FromVolume(volume, SimpleITK.ImportAsUInt8);
        }

        /// <summary>
        /// Converts the volume to the corresponding SimpleITK image,
        /// preserving all voxel values and transformations. The resulting image 
        /// will have pixel type <see cref="PixelIDValueEnum.sitkInt16"/>.
        /// </summary>
        /// <param name="volume">The volume that should be converted to an ITK image.</param>
        /// <returns></returns>
        public static ItkImageFromManaged FromVolume(Volume3D<short> volume)
        {
            return FromVolume(volume, SimpleITK.ImportAsInt16);
        }

        /// <summary>
        /// Converts the volume to the corresponding SimpleITK image,
        /// preserving all voxel values and transformations. The resulting image 
        /// will have pixel type <see cref="PixelIDValueEnum.sitkFloat32"/>.
        /// </summary>
        /// <param name="volume">The volume that should be converted to an ITK image.</param>
        /// <returns></returns>
        public static ItkImageFromManaged FromVolume(Volume3D<float> volume)
        {
            return FromVolume(volume, SimpleITK.ImportAsFloat);
        }

        /// <summary>
        /// Converts a volume to a SimpleITK Image, preserving all voxel values and transforms.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volume">The volume to convert.</param>
        /// <param name="import">The SimpleITK import function to use when creating the image.
        /// Arguments are the voxel buffer, image dimensions, image spacing, image origin,
        /// image orientation.</param>
        /// <returns></returns>
        private static ItkImageFromManaged FromVolume<T>(Volume3D<T> volume,
            Func<IntPtr, VectorUInt32, VectorDouble, VectorDouble, VectorDouble, Image> import)
        {
            var itkImageFromManaged = new ItkImageFromManaged(volume.Array);
            var unmanagedPointer = itkImageFromManaged.Handle.Value.AddrOfPinnedObject();
            var origin = volume.Origin;
            var direction = volume.Direction.Data;
            var image = import(
                unmanagedPointer,
                new VectorUInt32() { (uint)volume.DimX, (uint)volume.DimY, (uint)volume.DimZ },
                new VectorDouble() { volume.SpacingX, volume.SpacingY, volume.SpacingZ },
                new VectorDouble() { origin.X, origin.Y, origin.Z },
                new VectorDouble()
                    {
                        direction[0],
                        direction[3],
                        direction[6],
                        direction[1],
                        direction[4],
                        direction[7],
                        direction[2],
                        direction[5],
                        direction[8]
                    });
            itkImageFromManaged.Image = image;
            return itkImageFromManaged;
        }
    }

    /// <summary>
    /// Contains methods to convert SimpleITK images (<see cref="Image"/>) to
    /// <see cref="Volume3D{T}"/>, and vice versa.
    /// </summary>
    public static class SimpleItkConverters
    {
        /// <summary>
        /// Converts the SimpleITK image to the corresponding <see cref="Volume3D{T}"/>,
        /// preserving all voxel values and transformations. The image is expected to
        /// have pixel type <see cref="PixelIDValueEnum.sitkUInt8"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Volume3D<byte> ImageToVolumeByte(Image image)
            => ImageToVolume(image, PixelIDValueEnum.sitkUInt8, ImageToByteBuffer);

        /// <summary>
        /// Converts the SimpleITK image to the corresponding <see cref="Volume3D{T}"/>,
        /// preserving all voxel values and transformations. The image is expected to
        /// have pixel type <see cref="PixelIDValueEnum.sitkInt16"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Volume3D<short> ImageToVolumeShort(Image image)
            => ImageToVolume(image, PixelIDValueEnum.sitkInt16, ImageToShortBuffer);

        /// <summary>
        /// Converts the SimpleITK image to the corresponding <see cref="Volume3D{T}"/>,
        /// preserving all voxel values and transformations. The image is expected to
        /// have pixel type <see cref="PixelIDValueEnum.sitkFloat32"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Volume3D<float> ImageToVolumeFloat(Image image)
            => ImageToVolume(image, PixelIDValueEnum.sitkFloat32, ImageToFloatBuffer);

        /// <summary>
        /// Extracts the data buffer as a byte buffer from a SimpleITK image which is expected to
        /// have pixel type <see cref="PixelIDValueEnum.sitkUInt8"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static byte[] ImageToByteBuffer(Image img)
        {
            var voxelCount = NumberOfVoxels(img);
            var imageBuffer = img.GetBufferAsUInt8();
            var volumeBuffer = new byte[voxelCount];
            Marshal.Copy(imageBuffer, volumeBuffer, 0, voxelCount);
            return volumeBuffer;
        }

        /// <summary>
        /// Extracts the data buffer as a float buffer from a SimpleITK image which is expected to
        /// have pixel type <see cref="PixelIDValueEnum.sitkFloat32"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static short[] ImageToShortBuffer(Image img)
        {
            var voxelCount = NumberOfVoxels(img);
            var imageBuffer = img.GetBufferAsInt16();
            var volumeBuffer = new short[voxelCount];
            Marshal.Copy(imageBuffer, volumeBuffer, 0, voxelCount);
            return volumeBuffer;
        }

        /// <summary>
        /// Extracts the data buffer as a short buffer from a SimpleITK image which is expected to
        /// have pixel type <see cref="PixelIDValueEnum.sitkInt16"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static float[] ImageToFloatBuffer(Image img)
        {
            var voxelCount = NumberOfVoxels(img);
            var imageBuffer = img.GetBufferAsFloat();
            var volumeBuffer = new float[voxelCount];
            Marshal.Copy(imageBuffer, volumeBuffer, 0, voxelCount);
            return volumeBuffer;
        }

        /// <summary>
        /// Gets the total number of voxels in the image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static int NumberOfVoxels(Image image)
        {
            var size = image.GetSize();
            var dimX = (int)size[0];
            var dimY = (int)size[1];
            var dimZ = size.Count == 3 ? (int)size[2] : 1;
            return dimX * dimY * dimZ;
        }

        /// <summary>
        /// Converts a SimpleITK image to the corresponding <see cref="Volume3D{T}"/>, taking
        /// transformations into account. The image must be 3-dimensional, and have a voxel type
        /// that is suitable for conversion to T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="image"></param>
        /// <param name="expectedPixelID">The pixel type that the image is expected to have.</param>
        /// <param name="getBuffer">A function that extracts the image buffer and converts
        /// the unmanaged voxel data to a managed array of voxels.</param>
        /// <returns></returns>
        private static Volume3D<T> ImageToVolume<T>(Image image, 
            PixelIDValueEnum expectedPixelID, 
            Func<Image, T[]> getBuffer)
        {
            var dimensions = image.GetDimension();
            if (dimensions != 3)
            {
                throw new ArgumentException($"This method can only be used for 3dimensional volumes.", nameof(image));
            }

            if (image.GetPixelID() != expectedPixelID)
            {
                throw new ArgumentException($"This method can only be used on images with pixel type {expectedPixelID.ToString()}, but got: {image.GetPixelIDTypeAsString()} (numeric {image.GetPixelIDValue()})");
            }

            var size = image.GetSize();
            var dimX = (int)size[0];
            var dimY = (int)size[1];
            var dimZ = (int)size[2];
            var spacing = image.GetSpacing();
            var origin = image.GetOrigin();
            var direction = image.GetDirection();
            var directionData =
                new[]
                {
                    // image.GetDirection() passes the matrix in row-major order. MedILib operates in column-major order.
                    direction[0], direction[3], direction[6], direction[1], direction[4],
                    direction[7], direction[2], direction[5], direction[8]
                };

            return new Volume3D<T>(
                getBuffer(image),
                dimX, dimY, dimZ,
                spacing[0], spacing[1], spacing[2],
                new Point3D(origin[0], origin[1],
                origin[2]), new Matrix3(directionData));
        }
    }
}