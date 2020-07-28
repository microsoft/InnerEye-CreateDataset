///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Math
{
    using System;
    using Volumes;
    using ImageProcessing;
    using Morphology;
    using System.Linq;

    public static class MorphologicalExtensions
    {
        /// <summary>
        /// Erode the input mask by the same margin in each dimension
        /// </summary>
        /// <param name="input">The volume is a binary mask where 1 represents foreground and 0 represents background</param>
        /// <param name="mmMargin">Erosion in the x,y and z dimension</param>
        /// <param name="structuringElement">Structuring element to use (the default implementation is an ellipsoid)</param>
        /// <returns>the eroded structure: value is 1 inside the structure, 0 outside.</returns>
        public static Volume3D<byte> Erode(this Volume3D<byte> volume, double mmMargin, StructuringElement structuringElement = null)
        {
            return DilateErode(volume, mmMargin, mmMargin, mmMargin, true, null, structuringElement);
        }

        /// <summary>
        /// Dilate the input mask by the same margin in each dimension, taking into account the restriction volume 
        /// </summary>
        /// <param name="input">The volume is a binary mask where 1 represents foreground and 0 represents background</param>
        /// <param name="mmMargin">Erosion in the x,y and z dimension</param>
        /// <param name="restriction">The restriction volume is optional and contrainst the dilation to another volume</param>
        /// <param name="structuringElement">Structuring element to use (the default implementation is an ellipsoid)</param>
        /// <returns>the dilated structure: value is 1 inside the structure, 0 outside.</returns>
        public static Volume3D<byte> Dilate(this Volume3D<byte> volume, double mmMargin,
            Volume3D<byte> restriction = null, StructuringElement structuringElement = null)
        {
            return DilateErode(volume, mmMargin, mmMargin, mmMargin, false, restriction, structuringElement);
        }

        /// <summary>
        /// Erode the input mask by the margins specified in each dimension
        /// </summary>
        /// <param name="input">The volume is a binary mask where 1 represents foreground and 0 represents background</param>
        /// <param name="mmMarginX">Erosion in the x dimension</param>
        /// <param name="mmMarginY">Erosion in the y dimension</param>
        /// <param name="mmMarginZ">Erosion in the z dimension</param>
        /// <param name="structuringElement">Structuring element to use (the default implementation is an ellipsoid)</param>
        /// <returns>the eroded structure: value is 1 inside the structure, 0 outside.</returns>
        public static Volume3D<byte> Erode(this Volume3D<byte> volume, double mmMarginX, double mmMarginY, double mmMarginZ, StructuringElement structuringElement = null)
        {
            return DilateErode(volume, mmMarginX, mmMarginY, mmMarginZ, true, null, structuringElement);
        }

        /// <summary>
        /// Dilate the input mask by the margins specified in each dimension, taking into account the restriction volume
        /// </summary>
        /// <param name="input">The volume is a binary mask where 1 represents foreground and 0 represents background</param>
        /// <param name="mmMarginX">Erosion in the x dimension</param>
        /// <param name="mmMarginY">Erosion in the y dimension</param>
        /// <param name="mmMarginZ">Erosion in the z dimension</param>
        /// <param name="restriction">The restriction volume is optional and contrainst the dilation to another volume</param>
        /// <param name="structuringElement">Structuring element to use for (the default implementation is an ellipsoid)</param>
        /// <returns>the dilated structure: value is 1 inside the structure, 0 outside.</returns>
        public static Volume3D<byte> Dilate(this Volume3D<byte> volume, double mmMarginX, double mmMarginY, double mmMarginZ,
            Volume3D<byte> restriction = null, StructuringElement structuringElement = null)
        {
            return DilateErode(volume, mmMarginX, mmMarginY, mmMarginZ, false, restriction, structuringElement);
        }


        /// <summary>
        /// Creates a new volume with the provided Dilation/Erosion margins applied
        /// The algorithm creates an ellipsoid structuring element (SE), extracts the surface poits of the ellipsoid, computes
        /// difference sets (see: StructuringElement.cs for further details) 
        /// and then paints the resulting volume on all the surface voxels.
        /// A connected components search is used to ensure the operation handles multiple components correctly
        /// </summary>
        /// <param name="input">The volume is a binary mask where 1 represents foreground and 0 represents background</param>
        /// <param name="mmMarginX">Dilation/Erosion in the x dimension</param>
        /// <param name="mmMarginY">Dilation/Erosion in the y dimension</param>
        /// <param name="mmMarginZ">Dilation/Erosion in the z dimension</param>
        /// <param name="isErosion">Only erosion or dilation can be performed at one time</param>
        /// <param name="restriction">The restriction volume is optional and contrainst the dilation to another volume</param>
        /// <param name="structuringElement">Structuring element to use (the default implementation is an ellipsoid)</param>
        /// <returns>the dilated-and-eroded structure: value is 1 inside the structure, 0 outside.</returns>
        private static Volume3D<byte> DilateErode(this Volume3D<byte> input, double mmMarginX, double mmMarginY,
            double mmMarginZ, bool isErosion, Volume3D<byte> restriction = null, StructuringElement structuringElement = null )
        {
            // Check input and restriction volume compatibility
            ValidateInputs(input, restriction, mmMarginX, mmMarginY, mmMarginZ);

            // Copy the input as only surface points are affected
            var result = input.Copy();

            // Calculate erosion/dilation bounds
            int xNumberOfPixels = (int)Math.Round(mmMarginX / input.SpacingX);
            int yNumberOfPixels = (int)Math.Round(mmMarginY / input.SpacingY);
            int zNumberOfPixels = (int)Math.Round(mmMarginZ / input.SpacingZ);

            // Check if there is nothing to do
            if (xNumberOfPixels == 0 && yNumberOfPixels == 0 && zNumberOfPixels == 0)
            {
                return result;
            }

            // The dimensions in which the operation will be performed in
            bool dilationRequiredInX = xNumberOfPixels > 0;
            bool dilationRequiredInY = yNumberOfPixels > 0;
            bool dilationRequiredInZ = zNumberOfPixels > 0;

            byte labelToPaint = ModelConstants.MaskBackgroundIntensity;

            // We do this as we always erode at least one surface point 
            if (isErosion)
            {
                xNumberOfPixels = xNumberOfPixels > 1 ? xNumberOfPixels - 1 : 0;
                yNumberOfPixels = yNumberOfPixels > 1 ? yNumberOfPixels - 1 : 0;
                zNumberOfPixels = zNumberOfPixels > 1 ? zNumberOfPixels - 1 : 0;
            }
            else
            {
                labelToPaint = ModelConstants.MaskForegroundIntensity;
            }

             // Create an ellipsoid structuring element (if none provided)
            var ellipsoidStructuringElement = structuringElement ?? 
                new StructuringElement(xNumberOfPixels, yNumberOfPixels, zNumberOfPixels);

            // Ensure we always paint at least one component fully for every component in the volume
            var components = PaintFullSEOnceForEachConnectedComponent(
                input, restriction, result,
                ellipsoidStructuringElement, dilationRequiredInX, dilationRequiredInY, dilationRequiredInZ, labelToPaint);

            // Check that components were found
            if (components > 0)
            {
                //We now march along the input from left to right on each slice and for each surface point on the volume paint
                //all of the surface points of the structuring element
                result.ParallelIterateSlices(p =>
                {
                    // Check that we are in a surface point on the volume
                    // This is to make sure that any dilation/erosion is performed around the edges of the components of the mask
                    if (input.IsSurfacePoint(p.x, p.y, p.z, dilationRequiredInX, dilationRequiredInY, dilationRequiredInZ))
                    {
                        ellipsoidStructuringElement.PaintSurfacePointsOntoVolume(result, restriction, labelToPaint, p.x, p.y, p.z);
                    }
                });
            }
            return result;
        }

        /// <summary>
        /// Paint all of the points that lie inside the SE mask for a single surface point on each of the components of the input image
        /// returns the number of components painted
        /// </sumary>
        private static int PaintFullSEOnceForEachConnectedComponent(
            Volume3D<Byte> input, Volume3D<byte> restriction, Volume3D<Byte> result,
            StructuringElement structuringElement, bool traverseX, bool traverseY, bool traverseZ, byte label)
        {
            // Identify the connected components so that we can paint the ellipsoid fully, only once for each component
            var connectedComponentslabelMap = new Volume3D<ushort>(input.DimX, input.DimY, input.DimZ, input.SpacingX, input.SpacingY, input.SpacingZ);

            // The number of face connected components
            int components = ConnectedComponents.Find3d(input.Array, input.DimX, input.DimY, input.DimZ, ModelConstants.MaskBackgroundIntensity, connectedComponentslabelMap.Array);

            var visited = new bool[components];
            visited[ModelConstants.MaskBackgroundIntensity] = true;

            // Paint the structuring element fully for just one of the surface points 
            for (var z = 0; z < connectedComponentslabelMap.DimZ; z++)
            {
                for (var y = 0; y < connectedComponentslabelMap.DimY; y++)
                {
                    for (var x = 0; x < connectedComponentslabelMap.DimX; x++)
                    {
                        var ccLabel = connectedComponentslabelMap[x, y, z];
                        if (!visited[ccLabel] && input.IsSurfacePoint(x, y, z, traverseX, traverseY, traverseZ))
                        {
                            structuringElement.PaintAllForegroundPointsOntoVolume(result, restriction, label, x, y, z);
                            visited[ccLabel] = true;
                            if (visited.All(b => b))
                            {
                                return components;
                            }
                        }
                    }
                }
            }

            return components;
        }

        /// <summary>
        /// A surface point is a foreground voxel such that at least one of its neighbors (+-1 in each dimension so maximum 27) is
        /// in the background or out of bounds
        /// </summary>
        public static bool IsSurfacePoint(this Volume3D<byte> volume, int px, int py, int pz, bool traverseX, bool traverseY, bool traverseZ)
        {
            // Only foreground voxels can be surface points
            if (volume[px, py, pz] == ModelConstants.MaskBackgroundIntensity)
            {
                return false;
            }

            // Create bounds around the neighbours based on the dilation/erosion bounds 
            const int step = 1;

            var stepX = traverseX ? step : 0;
            var stepY = traverseY ? step : 0;
            var stepZ = traverseZ ? step : 0;

            // Perform traversal of the on-point's 1-connectivity neighborhood
            for (int z = -stepZ; z <= stepZ; z++)
            {
                for (int y = -stepY; y <= stepY; y++)
                {
                    for (int x = -stepX; x <= stepX; x++)
                    {
                        int offsetX = px + x;
                        int offsetY = py + y; 
                        int offsetZ = pz + z;
                        // Offset by the step in each dimension and check if neighbor
                        if (!volume.IsValid(offsetX, offsetY, offsetZ) ||
                            volume[offsetX, offsetY, offsetZ] == ModelConstants.MaskBackgroundIntensity)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void ValidateInputs(Volume3D<byte> input, Volume3D<byte> restriction, double mmMarginX, double mmMarginY, double mmMarginZ)
        {
            if (InvalidMargins(mmMarginX, mmMarginY, mmMarginZ))
            {
                throw new ArgumentException(string.Format("Only non-negative margins are supported: {0}{1}{2}",
                    mmMarginX < 0 ? $"mmMarginX = {mmMarginX}\n" : "",
                    mmMarginY < 0 ? $"mmMarginY = {mmMarginY}\n" : "",
                    mmMarginZ < 0 ? $"mmMarginZ = {mmMarginZ}\n" : ""));
            }
            if (InvalidRestriction(input, restriction))
            {
                throw new ArgumentException(string.Format("Volume and restriction are different dimensions and/or spacing: {0}{1}{2}{3}{4}{5}",
                    // Dimensions
                    input.DimX != restriction.DimX ? $"input.DimX = {input.DimX} and restriction.DimX = {restriction.DimX}\n" : "",
                    input.DimY != restriction.DimY ? $"input.DimY = {input.DimY} and restriction.DimY = {restriction.DimY}\n" : "",
                    input.DimZ != restriction.DimZ ? $"input.DimZ = {input.DimZ} and restriction.DimZ = {restriction.DimZ}\n" : "",
                    // Spacing
                    input.SpacingX != restriction.SpacingX ? $"input.SpacingX = {input.SpacingX} and restriction.SpacingX = {restriction.SpacingX}\n" : "",
                    input.SpacingY != restriction.SpacingY ? $"input.SpacingY = {input.SpacingY} and restriction.SpacingY = {restriction.SpacingY}\n" : "",
                    input.SpacingZ != restriction.SpacingZ ? $"input.SpacingZ = {input.SpacingZ} and restriction.SpacingZ = {restriction.SpacingZ}\n" : ""
                 ));
            }
        }

        /// <summary>
        /// Checks to make sure that dilation/erosion margins are non-negative
        /// </summary>
        private static bool InvalidMargins(double mmMarginX, double mmMarginY, double mmMarginZ)
            => mmMarginX < 0 || mmMarginY < 0 || mmMarginZ < 0;

        /// <summary>
        /// Checks to make sure that if a restriction is provided, it is the same dimension and spacing as the volume itself
        /// </summary>
        private static bool InvalidRestriction(Volume3D<byte> volume, Volume3D<byte> restriction)
            => restriction != null && (volume.DimX != restriction.DimX || volume.DimY != restriction.DimY || volume.DimZ != restriction.DimZ
                                           || volume.SpacingX != restriction.SpacingX || volume.SpacingY != restriction.SpacingY || volume.SpacingZ != restriction.SpacingZ);

        /// <summary>
        /// Returns the union of the two volumes, assumed to be the same size and to have all non-negative values.
        /// The value at a location will be positive if either input values is positive.
        /// </summary>
        /// <param name="vol1"></param>
        /// <param name="vol2"></param>
        /// <returns></returns>
        public static Volume3D<byte> Union(Volume3D<byte> vol1, Volume3D<byte> vol2)
        {
            var result = vol1.Copy();
            var otherArray = vol2.Array;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == 0) result[i] = otherArray[i];
            }
            return result;
        }
    }
}
