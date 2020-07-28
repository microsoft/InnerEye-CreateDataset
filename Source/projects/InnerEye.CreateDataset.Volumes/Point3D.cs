///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public struct Point3D
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point3D(IReadOnlyList<double> data)
        {
            if (data.Count == 3)
            {
                X = data[0];
                Y = data[1];
                Z = data[2];
            }
            else
            {
                throw new Exception("Point3D struct: data size does not match point size");
            }
        }

        /// <summary>
        /// Returns a zero Point3D.
        /// </summary>
        /// <returns>The zero Point3D.</returns>
        public static Point3D Zero()
        {
            return new Point3D(0,0,0);
        }

        public double this[int row]
        {
            get { return Data[row]; }
            set
            {
                switch (row)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                }
            }
        }

        public double[] Data
        {
            get
            {
                return new [] { X, Y, Z };
            }

            set
            {
                if (value.Length == 3)
                {
                    X = value[0];
                    Y = value[1];
                    Z = value[2];
                }
                else
                {
                    throw new Exception("Point3D struct: data size does not match point size");
                }
            }
        }

        public static Point3D operator -(Point3D a)
        {
            return new Point3D(-a.X, -a.Y, -a.Z); 
        }

        public static Point3D operator +(Point3D a, Point3D b)
        {
            return new Point3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Point3D operator -(Point3D a, Point3D b)
        {
            return new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Point3D operator *(Point3D a, Point3D b)
        {
            return new Point3D(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static Point3D operator /(Point3D a, Point3D b)
        {
            return new Point3D(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        }

        public static Point3D operator *(Point3D a, double factor)
        {
            return new Point3D(a.X * factor, a.Y * factor, a.Z * factor);
        }

        public static Point3D operator *(double factor, Point3D a)
        {
            return new Point3D(a.X * factor, a.Y * factor, a.Z * factor);
        }

        public static Point3D operator /(Point3D a, double factor)
        {
            return new Point3D(a.X / factor, a.Y / factor, a.Z / factor);
        }

        public static Point3D operator *(Matrix3 transform, Point3D a)
        {
            var matrix = transform.Data;

            return new Point3D(
                a.X*matrix[0] + a.Y*matrix[3] + a.Z*matrix[6],
                a.X*matrix[1] + a.Y*matrix[4] + a.Z*matrix[7],
                a.X*matrix[2] + a.Y*matrix[5] + a.Z*matrix[8]);
        }

        public static Point3D operator *(Matrix4 transform, Point3D a)
        {
            var matrix = transform.Data;
            var output = new Point3D(
                a.X*matrix[0] + a.Y*matrix[4] + a.Z*matrix[8] + matrix[12],
                a.X*matrix[1] + a.Y*matrix[5] + a.Z*matrix[9] + matrix[13],
                a.X*matrix[2] + a.Y*matrix[6] + a.Z*matrix[10] + matrix[14]);

            var w = a.X * matrix[3] + a.Y * matrix[7] + a.Z * matrix[11] + matrix[15];

            if (w != 0)
            {
                output.X /= w; output.Y /= w; output.Z /= w;
            }

            return output;
        }

        public static double DotProd(Point3D a, Point3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Point3D CrossProd(Point3D a, Point3D b)
        {
            return new Point3D(
                a.Y*b.Z - a.Z*b.Y,
                -(a.X*b.Z - a.Z*b.X),
                a.X*b.Y - a.Y*b.X );
        }

        public void Clear()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        /// <summary>
        /// Gets the Euclidean norm (square root of the sum of squares) of the vector.
        /// </summary>
        /// <returns></returns>
        public double Norm()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public double SquareNorm()
        {
            return X * X + Y * Y + Z * Z;
        }

        public override string ToString()
        {
            return $"x={X},y={Y},z={Z}";
        }
    }
}