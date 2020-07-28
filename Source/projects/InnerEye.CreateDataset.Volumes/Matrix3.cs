///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Linq;

    /// <summary>
    /// Encodes a 3x3 matrix over double precision numbers
    /// </summary>
    [Serializable]
    public class Matrix3
    {
        /// <summary>
        /// Construct the zero matrix
        /// </summary>
        public Matrix3()
        {
            Data = new double[9];
        }

        /// <summary>
        /// Copy the given matrix by value.
        /// </summary>
        /// <param name="matrix"></param>
        public Matrix3(Matrix3 matrix)
        {
            Data = new double[9];
            Array.Copy(matrix.Data, Data, Data.Length);
        }

        /// <summary>
        /// Copy the given data into a matrix. data is accessed by [i*3 + j] where i is the column index and j the row index. 
        /// </summary>
        /// <param name="data"></param>
        public Matrix3(double[] data)
        {
            if (data.Length == 9)
            {
                Data = new double[9];
                Array.Copy(data, Data, Data.Length);
            }
            else
            {
                throw new Exception("Matrix3 struct: data size does not match matrix size");
            }
        }

        /// <summary>
        /// Construct a matrix from 3 row vectors. 
        /// </summary>
        /// <param name="r0"></param>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        public static Matrix3 FromRows(Point3D r0, Point3D r1, Point3D r2)
        {
            return new Matrix3(new double[]
            {
                // NB column major organization
                r0.X, r1.X, r2.X, // column 0
                r0.Y, r1.Y, r2.Y, // column 1
                r0.Z, r1.Z, r2.Z  // column 2
            });
        }

        /// <summary>
        /// Construct a matrix from the 3 column vectors. 
        /// </summary>
        /// <param name="c0"></param>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static Matrix3 FromColumns(Point3D c0, Point3D c1, Point3D c2)
        {
            return new Matrix3(new double[]
            {
                // NB row major organization
                c0.X, c0.Y, c0.Z, // column 0
                c1.X, c1.Y, c1.Z, // column 1
                c2.X, c2.Y, c2.Z  // column 2
            });
        }

        /// <summary>
        /// Construct a matrix with [m00,m11,m22] along the diagonal. 
        /// </summary>
        /// <param name="m00"></param>
        /// <param name="m11"></param>
        /// <param name="m22"></param>
        /// <returns></returns>
        public static Matrix3 Diag(double m00, double m11, double m22)
        {
            return new Matrix3(new double[]
            {
                // NB row major organization
                m00, 0.0, 0.0, // column 0
                0.0, m11, 0.0, // column 1
                0.0, 0.0, m22  // column 2
            });
        }

        /// <summary>
        /// Return the matrix element [i,j] where i is the row index and j the column index. 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public double this[int row, int column]
        {
            get { return Data[row + column * 3]; }
            set { Data[row + column * 3] = value; }
        }

        /// <summary>
        /// Read/Write access to the underlying array stored in column major order
        /// </summary>
        public double[] Data { get; }

        /// <summary>
        /// Set this matrix to zero.
        /// </summary>
        public void Zero()
        {
            for (var i = 0; i < 9; i++)
            {
                Data[i] = 0;
            }
        }

        /// <summary>
        /// Return a new identity matrix
        /// </summary>
        /// <returns></returns>
        public static Matrix3 CreateIdentity()
        {
            var matrix = new Matrix3();
            matrix.Identity();
            return matrix;
        }

        /// <summary>
        /// Set this matrix to the identity
        /// </summary>
        public void Identity()
        {
            Data[0] = 1; Data[3] = 0; Data[6] = 0;
            Data[1] = 0; Data[4] = 1; Data[7] = 0;
            Data[2] = 0; Data[5] = 0; Data[8] = 1;
        }

        /// <summary>
        /// Return the transpose of this matrix. 
        /// </summary>
        /// <returns></returns>
        public Matrix3 Transpose()
        {
            var result = new Matrix3(new double[] {
                Data[0], Data[3], Data[6],
                Data[1], Data[4], Data[7],
                Data[2], Data[5], Data[8]
            });

            return result;
        }

        /// <summary>
        /// Returns the inverse of this matrix. 
        /// </summary>
        /// <returns></returns>
        public Matrix3 Inverse()
        {
            var result = new Matrix3
            {
                [0, 0] = this[1, 1] * this[2, 2] - this[1, 2] * this[2, 1],
                [1, 0] = this[1, 2] * this[2, 0] - this[1, 0] * this[2, 2],
                [2, 0] = this[1, 0] * this[2, 1] - this[1, 1] * this[2, 0]
            };

            var det = this[0, 0] * result[0, 0] + this[0, 1] * result[1, 0] + this[0, 2] * result[2, 0];

            result[0, 0] /= det;
            result[1, 0] /= det;
            result[2, 0] /= det;

            result[0, 1] = (this[0, 2] * this[2, 1] - this[0, 1] * this[2, 2]) / det;
            result[1, 1] = (this[0, 0] * this[2, 2] - this[0, 2] * this[2, 0]) / det;
            result[2, 1] = (this[0, 1] * this[2, 0] - this[0, 0] * this[2, 1]) / det;
            result[0, 2] = (this[0, 1] * this[1, 2] - this[0, 2] * this[1, 1]) / det;
            result[1, 2] = (this[0, 2] * this[1, 0] - this[0, 0] * this[1, 2]) / det;
            result[2, 2] = (this[0, 0] * this[1, 1] - this[0, 1] * this[1, 0]) / det;

            return result;
        }

        /// <summary>
        /// Return a copy of ith column of this matrix by value.
        /// </summary>
        /// <param name="i">[0,1,2]</param>
        /// <returns></returns>
        public Point3D Column(int i)
        {
            return new Point3D(Data[i * 3], Data[i * 3 + 1], Data[i * 3 + 2]);
        }

        /// <summary>
        /// Return a copy of the jth row of this matrix by value
        /// </summary>
        /// <param name="j">[0,1,2]</param>
        /// <returns></returns>
        public Point3D Row(int j)
        {
            return new Point3D(Data[j], Data[j + 3], Data[j + 6]);
        }

        /// <summary>
        /// Return the matrix product A.B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix3 operator *(Matrix3 a, Matrix3 b)
        {
            var result = new Matrix3();

            for (var i = 0; i < 3; i++)
            {
                result.Data[i * 3 + 0] = a.Data[0] * b.Data[i * 3 + 0] + a.Data[3] * b.Data[i * 3 + 1] + a.Data[6] * b.Data[i * 3 + 2];
                result.Data[i * 3 + 1] = a.Data[1] * b.Data[i * 3 + 0] + a.Data[4] * b.Data[i * 3 + 1] + a.Data[7] * b.Data[i * 3 + 2];
                result.Data[i * 3 + 2] = a.Data[2] * b.Data[i * 3 + 0] + a.Data[5] * b.Data[i * 3 + 1] + a.Data[8] * b.Data[i * 3 + 2];
            }

            return result;
        }

        /// <summary>
        /// Returns true if and only if left is exactly the same as right.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(Matrix3 left, Matrix3 right)
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

            for (var i = 0; i < 9; i++)
            {
                if (Math.Abs(left.Data[i] - right.Data[i]) > 0)
                {
                    equal = false;
                }
            }

            return equal;
        }

        /// <summary>
        /// Return true if left is not exactly the same as right
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(Matrix3 left, Matrix3 right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Return true if obj is a matrix and contains exactly the same elements as this
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var matrix = (Matrix3)obj;

            return (this == matrix);
        }

        /// <summary>
        /// Uses the default Array GetHasCode over Double.GetHashCode. The implementation of array hashing in MS ReferenceSources 
        /// sugest it uses only the last 8 elements of the array for the hash. So equivalence is not likely to mean equality
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Data?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Return true is this is exactly value wise equal to other. 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(Matrix3 other)
        {
            return Equals(Data, other.Data);
        }

        /// <summary>
        /// Returns true if the dot product of pairs of columns are all &lt epsilon 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static bool IsOrthogonalBasis(Matrix3 m, double epsilon)
        {
            var x = m.Column(0);
            var y = m.Column(1);
            var z = m.Column(2);

            return
                Math.Abs(Point3D.DotProd(x, y)) < epsilon &&
                Math.Abs(Point3D.DotProd(y, z)) < epsilon &&
                Math.Abs(Point3D.DotProd(z, x)) < epsilon;
        }

        /// <summary>
        /// Returns true if IsOrthogonalBasis(m, epsilon) and the squared length of all 
        /// columns is within an epsilon of 1.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        /// <remarks>Its not really fair to use the same epsilons!</remarks>
        public static bool IsOrthonormalBasis(Matrix3 m, double epsilon)
        {
            var x = m.Column(0);
            var y = m.Column(1);
            var z = m.Column(2);

            return
                IsOrthogonalBasis(m, epsilon) &&
                Math.Abs(Point3D.DotProd(x, x) - 1) < epsilon &&
                Math.Abs(Point3D.DotProd(y, y) - 1) < epsilon &&
                Math.Abs(Point3D.DotProd(z, z) - 1) < epsilon;
        }

        /// <summary>
        /// Round each element of the matrix to the nearest integer. You should really 
        /// ask yourself why, if you ever need to do this. 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix3 ElementWiseRound(Matrix3 m)
        {
            return new Matrix3(m.Data.Select(v => Math.Round(v)).ToArray());
        }
    }
}