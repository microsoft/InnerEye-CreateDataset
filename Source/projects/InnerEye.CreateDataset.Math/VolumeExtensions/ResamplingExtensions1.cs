///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿

/*
	==> AUTO GENERATED FILE, edit ResamplingExtensions.tt instead <==
*/
namespace InnerEye.CreateDataset.Math
{
	using System;
	using InnerEye.CreateDataset.Volumes;

	public static class ResamplingExtensions
	{
	
        // Expects pixel coordinates.
        private static double Linear(this Volume3D<double> input, double pixelX, double pixelY, double pixelZ, double outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            // local copies to help the compiler in subsequent optimizations
            var dimX = input.DimX;
            var dimY = input.DimY;
            var dimZ = input.DimZ;
            var dimXY = input.DimXY;

            // boundary check
            if (pixelX < 0 || pixelY < 0 || pixelZ < 0 || pixelX >= dimX - 1 || pixelY >= dimY - 1 || pixelZ >= dimZ - 1)
            {
                return input.LinearOutside(pixelX, pixelY, pixelZ, outsideValue);
            }

            // everything is inside
            int ind = xx + yy * dimX + zz * dimXY;
            var interpolation =
                  input[ind] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2)
                + input[ind + 1] * x2 * (1.0 - y2) * (1.0 - z2)
                + input[ind + dimX] * (1.0 - x2) * y2 * (1.0 - z2)
                + input[ind + dimXY] * (1.0 - x2) * (1.0 - y2) * z2
                + input[ind + 1 + dimXY] * x2 * (1.0 - y2) * z2
                + input[ind + dimX + dimXY] * (1.0 - x2) * y2 * z2
                + input[ind + 1 + dimX] * x2 * y2 * (1.0 - z2)
                + input[ind + 1 + dimX + dimXY] * x2 * y2 * z2;
			return (double)interpolation;
        }

        private static void AdjustXyz(this Volume3D<double> input, double pixelX, double pixelY, double pixelZ,
            ref double x2, ref int xx,
            ref double y2, ref int yy,
            ref double z2, ref int zz)
        {
            if (pixelX < 0)
            {
                x2 = 0;
                xx = 0;
            }
            else if (pixelX > input.DimX - 1)
            {
                x2 = 0;
            }

            if (pixelY < 0)
            {
                y2 = 0;
                yy = 0;
            }
            else if (pixelY > input.DimY - 1)
            {
                y2 = 0;
            }

            if (pixelZ < 0)
            {
                z2 = 0;
                zz = 0;
            }
            else if (pixelZ > input.DimZ - 1)
            {
                z2 = 0;
            }
        }

		/// <summary>
		/// Creates a new volume of the given size, using linear interpolation to create the voxels in the new volume.
		/// </summary>
        public static Volume3D<double> ResampleLinear(this Volume3D<double> input, int dimX, int dimY, int dimZ)
        {
            double spacingX = input.SpacingX * (input.DimX - 1) / (dimX - 1);
            double spacingY = input.SpacingY * (input.DimY - 1) / (dimY - 1);
            double spacingZ = input.SpacingZ * (input.DimZ - 1) / (dimZ - 1);
			// When using linear resampling, the default value will not be used, can hence set to a default.
			var outsideValue = default(double);
            var output = new Volume3D<double>(dimX, dimY, dimZ, spacingX, spacingY, spacingZ, input.Origin, input.Direction);
            GenericResampling.ResampleImage(input, output, outsideValue, (x, y, z) => input.Linear(x, y, z, 0));
            return output;
        }
        

        private static double LinearOutside(this Volume3D<double> input, double pixelX, double pixelY, double pixelZ, double outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            if (pixelX < -0.5 || pixelY < -0.5 || pixelZ < -0.5
                || pixelX >= input.DimX - 0.5
                || pixelY >= input.DimY - 0.5
                || pixelZ >= input.DimZ - 0.5)
            {
                return outsideValue;
            }

            input.AdjustXyz(pixelX, pixelY, pixelZ, ref x2, ref xx, ref y2, ref yy, ref z2, ref zz);

            int index = xx + yy * input.DimX + zz * input.DimXY;

            // this is always valid, return only this if x==_dimX and y==_dimY and z==_dimZ
            double value = input[index] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2);

            // check all cases where at least one of the x2,y2,z2 values is 0, but not all are 0.
            value += input.ValueAdjustment(x2, y2, z2, index);

            return  (double)value;
        }

        private static double ValueAdjustment(this Volume3D<double> input, double x2, double y2, double z2, int index)
        {
            if (x2 == 0 && y2 == 0 && z2 != 0)
            {
                // 0 0 1
                return input[index + input.DimXY] * z2;
            }
            if (x2 == 0 && y2 != 0 && z2 == 0)
            {
                // 0 1 0
                return input[index + input.DimX] * y2;
            }
            if (x2 != 0 && y2 == 0 && z2 == 0)
            {
                // 1 0 0
                return input[index + 1] * x2;
            }
            if (x2 == 0 && y2 != 0 && z2 != 0)
            {
                // 0 1 1
                return input[index + input.DimX + input.DimXY] * y2 * z2
                    + input[index + input.DimX] * y2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - y2) * z2;
            }
            if (x2 != 0 && y2 == 0 && z2 != 0)
            {
                // 1 0 1
                return input[index + 1 + input.DimXY] * x2 * z2
                    + input[index + 1] * x2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - x2) * z2;
            }
            if (x2 != 0 && y2 != 0 && z2 == 0)
            {
                // 1 1 0
                return input[index + 1 + input.DimX] * x2 * y2
                    + input[index + input.DimX] * (1.0 - x2) * y2
                    + input[index + 1] * x2 * (1.0 - y2);
            }
            return 0;
        }
       	
        // Expects pixel coordinates.
        private static float Linear(this Volume3D<float> input, double pixelX, double pixelY, double pixelZ, float outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            // local copies to help the compiler in subsequent optimizations
            var dimX = input.DimX;
            var dimY = input.DimY;
            var dimZ = input.DimZ;
            var dimXY = input.DimXY;

            // boundary check
            if (pixelX < 0 || pixelY < 0 || pixelZ < 0 || pixelX >= dimX - 1 || pixelY >= dimY - 1 || pixelZ >= dimZ - 1)
            {
                return input.LinearOutside(pixelX, pixelY, pixelZ, outsideValue);
            }

            // everything is inside
            int ind = xx + yy * dimX + zz * dimXY;
            var interpolation =
                  input[ind] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2)
                + input[ind + 1] * x2 * (1.0 - y2) * (1.0 - z2)
                + input[ind + dimX] * (1.0 - x2) * y2 * (1.0 - z2)
                + input[ind + dimXY] * (1.0 - x2) * (1.0 - y2) * z2
                + input[ind + 1 + dimXY] * x2 * (1.0 - y2) * z2
                + input[ind + dimX + dimXY] * (1.0 - x2) * y2 * z2
                + input[ind + 1 + dimX] * x2 * y2 * (1.0 - z2)
                + input[ind + 1 + dimX + dimXY] * x2 * y2 * z2;
			return (float)interpolation;
        }

        private static void AdjustXyz(this Volume3D<float> input, double pixelX, double pixelY, double pixelZ,
            ref double x2, ref int xx,
            ref double y2, ref int yy,
            ref double z2, ref int zz)
        {
            if (pixelX < 0)
            {
                x2 = 0;
                xx = 0;
            }
            else if (pixelX > input.DimX - 1)
            {
                x2 = 0;
            }

            if (pixelY < 0)
            {
                y2 = 0;
                yy = 0;
            }
            else if (pixelY > input.DimY - 1)
            {
                y2 = 0;
            }

            if (pixelZ < 0)
            {
                z2 = 0;
                zz = 0;
            }
            else if (pixelZ > input.DimZ - 1)
            {
                z2 = 0;
            }
        }

		/// <summary>
		/// Creates a new volume of the given size, using linear interpolation to create the voxels in the new volume.
		/// </summary>
        public static Volume3D<float> ResampleLinear(this Volume3D<float> input, int dimX, int dimY, int dimZ)
        {
            double spacingX = input.SpacingX * (input.DimX - 1) / (dimX - 1);
            double spacingY = input.SpacingY * (input.DimY - 1) / (dimY - 1);
            double spacingZ = input.SpacingZ * (input.DimZ - 1) / (dimZ - 1);
			// When using linear resampling, the default value will not be used, can hence set to a default.
			var outsideValue = default(float);
            var output = new Volume3D<float>(dimX, dimY, dimZ, spacingX, spacingY, spacingZ, input.Origin, input.Direction);
            GenericResampling.ResampleImage(input, output, outsideValue, (x, y, z) => input.Linear(x, y, z, 0));
            return output;
        }
        

        private static float LinearOutside(this Volume3D<float> input, double pixelX, double pixelY, double pixelZ, float outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            if (pixelX < -0.5 || pixelY < -0.5 || pixelZ < -0.5
                || pixelX >= input.DimX - 0.5
                || pixelY >= input.DimY - 0.5
                || pixelZ >= input.DimZ - 0.5)
            {
                return outsideValue;
            }

            input.AdjustXyz(pixelX, pixelY, pixelZ, ref x2, ref xx, ref y2, ref yy, ref z2, ref zz);

            int index = xx + yy * input.DimX + zz * input.DimXY;

            // this is always valid, return only this if x==_dimX and y==_dimY and z==_dimZ
            double value = input[index] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2);

            // check all cases where at least one of the x2,y2,z2 values is 0, but not all are 0.
            value += input.ValueAdjustment(x2, y2, z2, index);

            return  (float)value;
        }

        private static double ValueAdjustment(this Volume3D<float> input, double x2, double y2, double z2, int index)
        {
            if (x2 == 0 && y2 == 0 && z2 != 0)
            {
                // 0 0 1
                return input[index + input.DimXY] * z2;
            }
            if (x2 == 0 && y2 != 0 && z2 == 0)
            {
                // 0 1 0
                return input[index + input.DimX] * y2;
            }
            if (x2 != 0 && y2 == 0 && z2 == 0)
            {
                // 1 0 0
                return input[index + 1] * x2;
            }
            if (x2 == 0 && y2 != 0 && z2 != 0)
            {
                // 0 1 1
                return input[index + input.DimX + input.DimXY] * y2 * z2
                    + input[index + input.DimX] * y2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - y2) * z2;
            }
            if (x2 != 0 && y2 == 0 && z2 != 0)
            {
                // 1 0 1
                return input[index + 1 + input.DimXY] * x2 * z2
                    + input[index + 1] * x2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - x2) * z2;
            }
            if (x2 != 0 && y2 != 0 && z2 == 0)
            {
                // 1 1 0
                return input[index + 1 + input.DimX] * x2 * y2
                    + input[index + input.DimX] * (1.0 - x2) * y2
                    + input[index + 1] * x2 * (1.0 - y2);
            }
            return 0;
        }
       	
        // Expects pixel coordinates.
        private static int Linear(this Volume3D<int> input, double pixelX, double pixelY, double pixelZ, int outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            // local copies to help the compiler in subsequent optimizations
            var dimX = input.DimX;
            var dimY = input.DimY;
            var dimZ = input.DimZ;
            var dimXY = input.DimXY;

            // boundary check
            if (pixelX < 0 || pixelY < 0 || pixelZ < 0 || pixelX >= dimX - 1 || pixelY >= dimY - 1 || pixelZ >= dimZ - 1)
            {
                return input.LinearOutside(pixelX, pixelY, pixelZ, outsideValue);
            }

            // everything is inside
            int ind = xx + yy * dimX + zz * dimXY;
            var interpolation =
                  input[ind] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2)
                + input[ind + 1] * x2 * (1.0 - y2) * (1.0 - z2)
                + input[ind + dimX] * (1.0 - x2) * y2 * (1.0 - z2)
                + input[ind + dimXY] * (1.0 - x2) * (1.0 - y2) * z2
                + input[ind + 1 + dimXY] * x2 * (1.0 - y2) * z2
                + input[ind + dimX + dimXY] * (1.0 - x2) * y2 * z2
                + input[ind + 1 + dimX] * x2 * y2 * (1.0 - z2)
                + input[ind + 1 + dimX + dimXY] * x2 * y2 * z2;
			return (int)Math.Round(interpolation);
        }

        private static void AdjustXyz(this Volume3D<int> input, double pixelX, double pixelY, double pixelZ,
            ref double x2, ref int xx,
            ref double y2, ref int yy,
            ref double z2, ref int zz)
        {
            if (pixelX < 0)
            {
                x2 = 0;
                xx = 0;
            }
            else if (pixelX > input.DimX - 1)
            {
                x2 = 0;
            }

            if (pixelY < 0)
            {
                y2 = 0;
                yy = 0;
            }
            else if (pixelY > input.DimY - 1)
            {
                y2 = 0;
            }

            if (pixelZ < 0)
            {
                z2 = 0;
                zz = 0;
            }
            else if (pixelZ > input.DimZ - 1)
            {
                z2 = 0;
            }
        }

		/// <summary>
		/// Creates a new volume of the given size, using linear interpolation to create the voxels in the new volume.
		/// </summary>
        public static Volume3D<int> ResampleLinear(this Volume3D<int> input, int dimX, int dimY, int dimZ)
        {
            double spacingX = input.SpacingX * (input.DimX - 1) / (dimX - 1);
            double spacingY = input.SpacingY * (input.DimY - 1) / (dimY - 1);
            double spacingZ = input.SpacingZ * (input.DimZ - 1) / (dimZ - 1);
			// When using linear resampling, the default value will not be used, can hence set to a default.
			var outsideValue = default(int);
            var output = new Volume3D<int>(dimX, dimY, dimZ, spacingX, spacingY, spacingZ, input.Origin, input.Direction);
            GenericResampling.ResampleImage(input, output, outsideValue, (x, y, z) => input.Linear(x, y, z, 0));
            return output;
        }
        

        private static int LinearOutside(this Volume3D<int> input, double pixelX, double pixelY, double pixelZ, int outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            if (pixelX < -0.5 || pixelY < -0.5 || pixelZ < -0.5
                || pixelX >= input.DimX - 0.5
                || pixelY >= input.DimY - 0.5
                || pixelZ >= input.DimZ - 0.5)
            {
                return outsideValue;
            }

            input.AdjustXyz(pixelX, pixelY, pixelZ, ref x2, ref xx, ref y2, ref yy, ref z2, ref zz);

            int index = xx + yy * input.DimX + zz * input.DimXY;

            // this is always valid, return only this if x==_dimX and y==_dimY and z==_dimZ
            double value = input[index] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2);

            // check all cases where at least one of the x2,y2,z2 values is 0, but not all are 0.
            value += input.ValueAdjustment(x2, y2, z2, index);

            return  (int)Math.Round(value);
        }

        private static double ValueAdjustment(this Volume3D<int> input, double x2, double y2, double z2, int index)
        {
            if (x2 == 0 && y2 == 0 && z2 != 0)
            {
                // 0 0 1
                return input[index + input.DimXY] * z2;
            }
            if (x2 == 0 && y2 != 0 && z2 == 0)
            {
                // 0 1 0
                return input[index + input.DimX] * y2;
            }
            if (x2 != 0 && y2 == 0 && z2 == 0)
            {
                // 1 0 0
                return input[index + 1] * x2;
            }
            if (x2 == 0 && y2 != 0 && z2 != 0)
            {
                // 0 1 1
                return input[index + input.DimX + input.DimXY] * y2 * z2
                    + input[index + input.DimX] * y2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - y2) * z2;
            }
            if (x2 != 0 && y2 == 0 && z2 != 0)
            {
                // 1 0 1
                return input[index + 1 + input.DimXY] * x2 * z2
                    + input[index + 1] * x2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - x2) * z2;
            }
            if (x2 != 0 && y2 != 0 && z2 == 0)
            {
                // 1 1 0
                return input[index + 1 + input.DimX] * x2 * y2
                    + input[index + input.DimX] * (1.0 - x2) * y2
                    + input[index + 1] * x2 * (1.0 - y2);
            }
            return 0;
        }
       	
        // Expects pixel coordinates.
        private static short Linear(this Volume3D<short> input, double pixelX, double pixelY, double pixelZ, short outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            // local copies to help the compiler in subsequent optimizations
            var dimX = input.DimX;
            var dimY = input.DimY;
            var dimZ = input.DimZ;
            var dimXY = input.DimXY;

            // boundary check
            if (pixelX < 0 || pixelY < 0 || pixelZ < 0 || pixelX >= dimX - 1 || pixelY >= dimY - 1 || pixelZ >= dimZ - 1)
            {
                return input.LinearOutside(pixelX, pixelY, pixelZ, outsideValue);
            }

            // everything is inside
            int ind = xx + yy * dimX + zz * dimXY;
            var interpolation =
                  input[ind] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2)
                + input[ind + 1] * x2 * (1.0 - y2) * (1.0 - z2)
                + input[ind + dimX] * (1.0 - x2) * y2 * (1.0 - z2)
                + input[ind + dimXY] * (1.0 - x2) * (1.0 - y2) * z2
                + input[ind + 1 + dimXY] * x2 * (1.0 - y2) * z2
                + input[ind + dimX + dimXY] * (1.0 - x2) * y2 * z2
                + input[ind + 1 + dimX] * x2 * y2 * (1.0 - z2)
                + input[ind + 1 + dimX + dimXY] * x2 * y2 * z2;
			return (short)Math.Round(interpolation);
        }

        private static void AdjustXyz(this Volume3D<short> input, double pixelX, double pixelY, double pixelZ,
            ref double x2, ref int xx,
            ref double y2, ref int yy,
            ref double z2, ref int zz)
        {
            if (pixelX < 0)
            {
                x2 = 0;
                xx = 0;
            }
            else if (pixelX > input.DimX - 1)
            {
                x2 = 0;
            }

            if (pixelY < 0)
            {
                y2 = 0;
                yy = 0;
            }
            else if (pixelY > input.DimY - 1)
            {
                y2 = 0;
            }

            if (pixelZ < 0)
            {
                z2 = 0;
                zz = 0;
            }
            else if (pixelZ > input.DimZ - 1)
            {
                z2 = 0;
            }
        }

		/// <summary>
		/// Creates a new volume of the given size, using linear interpolation to create the voxels in the new volume.
		/// </summary>
        public static Volume3D<short> ResampleLinear(this Volume3D<short> input, int dimX, int dimY, int dimZ)
        {
            double spacingX = input.SpacingX * (input.DimX - 1) / (dimX - 1);
            double spacingY = input.SpacingY * (input.DimY - 1) / (dimY - 1);
            double spacingZ = input.SpacingZ * (input.DimZ - 1) / (dimZ - 1);
			// When using linear resampling, the default value will not be used, can hence set to a default.
			var outsideValue = default(short);
            var output = new Volume3D<short>(dimX, dimY, dimZ, spacingX, spacingY, spacingZ, input.Origin, input.Direction);
            GenericResampling.ResampleImage(input, output, outsideValue, (x, y, z) => input.Linear(x, y, z, 0));
            return output;
        }
        

        private static short LinearOutside(this Volume3D<short> input, double pixelX, double pixelY, double pixelZ, short outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            if (pixelX < -0.5 || pixelY < -0.5 || pixelZ < -0.5
                || pixelX >= input.DimX - 0.5
                || pixelY >= input.DimY - 0.5
                || pixelZ >= input.DimZ - 0.5)
            {
                return outsideValue;
            }

            input.AdjustXyz(pixelX, pixelY, pixelZ, ref x2, ref xx, ref y2, ref yy, ref z2, ref zz);

            int index = xx + yy * input.DimX + zz * input.DimXY;

            // this is always valid, return only this if x==_dimX and y==_dimY and z==_dimZ
            double value = input[index] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2);

            // check all cases where at least one of the x2,y2,z2 values is 0, but not all are 0.
            value += input.ValueAdjustment(x2, y2, z2, index);

            return  (short)Math.Round(value);
        }

        private static double ValueAdjustment(this Volume3D<short> input, double x2, double y2, double z2, int index)
        {
            if (x2 == 0 && y2 == 0 && z2 != 0)
            {
                // 0 0 1
                return input[index + input.DimXY] * z2;
            }
            if (x2 == 0 && y2 != 0 && z2 == 0)
            {
                // 0 1 0
                return input[index + input.DimX] * y2;
            }
            if (x2 != 0 && y2 == 0 && z2 == 0)
            {
                // 1 0 0
                return input[index + 1] * x2;
            }
            if (x2 == 0 && y2 != 0 && z2 != 0)
            {
                // 0 1 1
                return input[index + input.DimX + input.DimXY] * y2 * z2
                    + input[index + input.DimX] * y2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - y2) * z2;
            }
            if (x2 != 0 && y2 == 0 && z2 != 0)
            {
                // 1 0 1
                return input[index + 1 + input.DimXY] * x2 * z2
                    + input[index + 1] * x2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - x2) * z2;
            }
            if (x2 != 0 && y2 != 0 && z2 == 0)
            {
                // 1 1 0
                return input[index + 1 + input.DimX] * x2 * y2
                    + input[index + input.DimX] * (1.0 - x2) * y2
                    + input[index + 1] * x2 * (1.0 - y2);
            }
            return 0;
        }
       	
        // Expects pixel coordinates.
        private static byte Linear(this Volume3D<byte> input, double pixelX, double pixelY, double pixelZ, byte outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            // local copies to help the compiler in subsequent optimizations
            var dimX = input.DimX;
            var dimY = input.DimY;
            var dimZ = input.DimZ;
            var dimXY = input.DimXY;

            // boundary check
            if (pixelX < 0 || pixelY < 0 || pixelZ < 0 || pixelX >= dimX - 1 || pixelY >= dimY - 1 || pixelZ >= dimZ - 1)
            {
                return input.LinearOutside(pixelX, pixelY, pixelZ, outsideValue);
            }

            // everything is inside
            int ind = xx + yy * dimX + zz * dimXY;
            var interpolation =
                  input[ind] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2)
                + input[ind + 1] * x2 * (1.0 - y2) * (1.0 - z2)
                + input[ind + dimX] * (1.0 - x2) * y2 * (1.0 - z2)
                + input[ind + dimXY] * (1.0 - x2) * (1.0 - y2) * z2
                + input[ind + 1 + dimXY] * x2 * (1.0 - y2) * z2
                + input[ind + dimX + dimXY] * (1.0 - x2) * y2 * z2
                + input[ind + 1 + dimX] * x2 * y2 * (1.0 - z2)
                + input[ind + 1 + dimX + dimXY] * x2 * y2 * z2;
			return (byte)Math.Round(interpolation);
        }

        private static void AdjustXyz(this Volume3D<byte> input, double pixelX, double pixelY, double pixelZ,
            ref double x2, ref int xx,
            ref double y2, ref int yy,
            ref double z2, ref int zz)
        {
            if (pixelX < 0)
            {
                x2 = 0;
                xx = 0;
            }
            else if (pixelX > input.DimX - 1)
            {
                x2 = 0;
            }

            if (pixelY < 0)
            {
                y2 = 0;
                yy = 0;
            }
            else if (pixelY > input.DimY - 1)
            {
                y2 = 0;
            }

            if (pixelZ < 0)
            {
                z2 = 0;
                zz = 0;
            }
            else if (pixelZ > input.DimZ - 1)
            {
                z2 = 0;
            }
        }

		/// <summary>
		/// Creates a new volume of the given size, using linear interpolation to create the voxels in the new volume.
		/// </summary>
        public static Volume3D<byte> ResampleLinear(this Volume3D<byte> input, int dimX, int dimY, int dimZ)
        {
            double spacingX = input.SpacingX * (input.DimX - 1) / (dimX - 1);
            double spacingY = input.SpacingY * (input.DimY - 1) / (dimY - 1);
            double spacingZ = input.SpacingZ * (input.DimZ - 1) / (dimZ - 1);
			// When using linear resampling, the default value will not be used, can hence set to a default.
			var outsideValue = default(byte);
            var output = new Volume3D<byte>(dimX, dimY, dimZ, spacingX, spacingY, spacingZ, input.Origin, input.Direction);
            GenericResampling.ResampleImage(input, output, outsideValue, (x, y, z) => input.Linear(x, y, z, 0));
            return output;
        }
        

        private static byte LinearOutside(this Volume3D<byte> input, double pixelX, double pixelY, double pixelZ, byte outsideValue)
        {
            int xx = (int)pixelX;
            int yy = (int)pixelY;
            int zz = (int)pixelZ;

            double x2 = pixelX - xx, y2 = pixelY - yy, z2 = pixelZ - zz;

            if (pixelX < -0.5 || pixelY < -0.5 || pixelZ < -0.5
                || pixelX >= input.DimX - 0.5
                || pixelY >= input.DimY - 0.5
                || pixelZ >= input.DimZ - 0.5)
            {
                return outsideValue;
            }

            input.AdjustXyz(pixelX, pixelY, pixelZ, ref x2, ref xx, ref y2, ref yy, ref z2, ref zz);

            int index = xx + yy * input.DimX + zz * input.DimXY;

            // this is always valid, return only this if x==_dimX and y==_dimY and z==_dimZ
            double value = input[index] * (1.0 - x2) * (1.0 - y2) * (1.0 - z2);

            // check all cases where at least one of the x2,y2,z2 values is 0, but not all are 0.
            value += input.ValueAdjustment(x2, y2, z2, index);

            return  (byte)Math.Round(value);
        }

        private static double ValueAdjustment(this Volume3D<byte> input, double x2, double y2, double z2, int index)
        {
            if (x2 == 0 && y2 == 0 && z2 != 0)
            {
                // 0 0 1
                return input[index + input.DimXY] * z2;
            }
            if (x2 == 0 && y2 != 0 && z2 == 0)
            {
                // 0 1 0
                return input[index + input.DimX] * y2;
            }
            if (x2 != 0 && y2 == 0 && z2 == 0)
            {
                // 1 0 0
                return input[index + 1] * x2;
            }
            if (x2 == 0 && y2 != 0 && z2 != 0)
            {
                // 0 1 1
                return input[index + input.DimX + input.DimXY] * y2 * z2
                    + input[index + input.DimX] * y2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - y2) * z2;
            }
            if (x2 != 0 && y2 == 0 && z2 != 0)
            {
                // 1 0 1
                return input[index + 1 + input.DimXY] * x2 * z2
                    + input[index + 1] * x2 * (1.0 - z2)
                    + input[index + input.DimXY] * (1.0 - x2) * z2;
            }
            if (x2 != 0 && y2 != 0 && z2 == 0)
            {
                // 1 1 0
                return input[index + 1 + input.DimX] * x2 * y2
                    + input[index + input.DimX] * (1.0 - x2) * y2
                    + input[index + 1] * x2 * (1.0 - y2);
            }
            return 0;
        }
       	
       
    }
}
