///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;

    public class Matrix4
    {
        public Matrix4()
        {
            Data = new double[16];
        }

        public Matrix4(Matrix4 matrix)
        {
            Data = new double[16];
            Array.Copy(matrix.Data, this.Data, this.Data.Length);
        }
        
        public double this[int row, int column]
        {
            get { return Data[row + column * 4]; }
            set { Data[row + column * 4] = value; }
        }
        
        public double[] Data { get; }

        public void Zero()
        {
            for (var i = 0; i < 16; i++)
            {
                Data[i] = 0;
            }
        }

        public void Identity()
        {
            for (var i = 0; i < 16; i++)
            {
                Data[i] = 0;
            }

            Data[0] = 1;
            Data[5] = 1;
            Data[10] = 1;
            Data[15] = 1;
        }
        
        public static Matrix4 operator *(Matrix4 a, Matrix4 b)
        {
            var result = new Matrix4();

            for (var i = 0; i < 4; i++)
            {
                result.Data[i * 4 + 0] = a.Data[0] * b.Data[i * 4 + 0] + a.Data[4] * b.Data[i * 4 + 1] + a.Data[8] * b.Data[i * 4 + 2] + a.Data[12] * b.Data[i * 4 + 3];
                result.Data[i * 4 + 1] = a.Data[1] * b.Data[i * 4 + 0] + a.Data[5] * b.Data[i * 4 + 1] + a.Data[9] * b.Data[i * 4 + 2] + a.Data[13] * b.Data[i * 4 + 3];
                result.Data[i * 4 + 2] = a.Data[2] * b.Data[i * 4 + 0] + a.Data[6] * b.Data[i * 4 + 1] + a.Data[10] * b.Data[i * 4 + 2] + a.Data[14] * b.Data[i * 4 + 3];
                result.Data[i * 4 + 3] = a.Data[3] * b.Data[i * 4 + 0] + a.Data[7] * b.Data[i * 4 + 1] + a.Data[11] * b.Data[i * 4 + 2] + a.Data[15] * b.Data[i * 4 + 3];
            }

            return result;
        }
    }
}