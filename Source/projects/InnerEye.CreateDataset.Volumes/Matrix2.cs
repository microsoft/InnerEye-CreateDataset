///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;

    public class Matrix2
    {
        public Matrix2()
        {
            Data = new double[4];
        }

        public Matrix2(Matrix2 matrix)
        {
            Data = new double[4];
            Array.Copy(matrix.Data, Data, Data.Length);
        }

        public Matrix2(double[] data)
        {
            if (data.Length == 4)
            {
                Data = new double[4];
                Array.Copy(data, Data, Data.Length);
            }
            else
            {
                throw new Exception("Matrix2 struct: data size does not match matrix size");
            }
        }

        public double this[int row, int column]
        {
            get { return Data[row + column * 2]; }
            set { Data[row + column * 2] = value; }
        }

        public double[] Data { get; }

        public void Zero()
        {
            for (var i = 0; i < 4; i++)
            {
                Data[i] = 0;
            }
        }

        public void Identity()
        {       
            Data[0] = 1; Data[2] = 0;
            Data[1] = 0; Data[3] = 1;
        }

        public static Matrix2 CreateIdentity()
        {
            var matrix = new Matrix2();
            matrix.Identity();

            return matrix;
        }

        public Matrix2 Inverse()
        {
            var det = this[0, 0]*this[1, 1] - this[0, 1]*this[1, 0];

            var result = new Matrix2
            {
                [0, 0] = this[1, 1]/det,
                [1, 1] = this[0, 0]/det,
                [0, 1] = -this[1, 0]/det,
                [1, 0] = -this[0, 1]/det
            };

            return result;
        }

        public static Matrix2 operator *(Matrix2 a, Matrix2 b)
        {
            var result = new Matrix2();

            for (var i = 0; i < 2; i++)
            {
                result.Data[i * 2 + 0] = a.Data[0] * b.Data[i * 2 + 0] + a.Data[2] * b.Data[i * 2 + 1];
                result.Data[i * 2 + 1] = a.Data[1] * b.Data[i * 2 + 0] + a.Data[3] * b.Data[i * 2 + 1];
            }

            return result;
        }

        public static bool operator ==(Matrix2 left, Matrix2 right)
        {
            var equal = true;
            
            var leftNull = ReferenceEquals(left, null);
            var rightNull = ReferenceEquals(right, null);

            if (leftNull && rightNull)
            {
                return true;
            }
            else if (leftNull || rightNull)
            {
                return false;
            }

            for (var i = 0; i < 4; i++)
            {
                if (Math.Abs(left.Data[i] - right.Data[i]) > 0)
                {
                    equal = false;
                }
            }

            return equal;
        }

        public static bool operator !=(Matrix2 left, Matrix2 right)
        {
            return !(left==right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var matrix = (Matrix2)obj;

            return (this == matrix);
        }

        public override int GetHashCode()
        {
            return Data?.GetHashCode() ?? 0;
        }

        protected bool Equals(Matrix2 other)
        {
            return Equals(Data, other.Data);
        }
    }
}