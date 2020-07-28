///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

namespace InnerEye.CreateDataset.Volumes
{
    using System;
    using System.Collections.Generic;

    public struct Point2D
    {
        public double X { get; set; }

        public double Y { get; set; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point2D(IReadOnlyList<double> data)
        {
            if (data.Count == 2)
            {
                X = data[0];
                Y = data[1];
            }
            else
            {
                throw new Exception("Point2D struct: data size does not match point size");
            }
        }

        public double[] Data
        {
            get
            {
                return new [] { X, Y };
            }

            set
            {
                if (value.Length == 2)
                {
                    X = value[0];
                    Y = value[1];
                }
                else
                {
                    throw new Exception("Point3D struct: data size does not match point size");
                }
            }
        }

        public static Point2D operator +(Point2D a, Point2D b)
        {
            return new Point2D(a.X + b.X, a.Y + b.Y);
        }

        public static Point2D operator -(Point2D a, Point2D b)
        {
            return new Point2D(a.X - b.X, a.Y - b.Y);
        }

        public static Point2D operator *(Point2D a, Point2D b)
        {
            return new Point2D(a.X * b.X, a.Y * b.Y);
        }

        public static Point2D operator /(Point2D a, Point2D b)
        {
            return new Point2D(a.X / b.X, a.Y / b.Y);
        }

        public static Point2D operator *(Point2D a, double factor)
        {
            return new Point2D(a.X * factor, a.Y * factor);
        }

        public static Point2D operator *(double factor, Point2D a)
        {
            return new Point2D(a.X * factor, a.Y * factor);
        }

        public static Point2D operator /(Point2D a, double factor)
        {
            return new Point2D(a.X / factor, a.Y / factor);
        }

        public static Point2D operator *(Matrix2 transform, Point2D a)
        {
            var matrix = transform.Data;
            return new Point2D(a.X*matrix[0] + a.Y*matrix[2], a.X*matrix[1] + a.Y*matrix[3]);
        }

        public static Point2D operator *(Matrix3 transform, Point2D a)
        {
            var matrix = transform.Data;

            var output = new Point2D(
                a.X*matrix[0] + a.Y*matrix[3] + matrix[6],
                a.X*matrix[1] + a.Y*matrix[4] + matrix[7]);

            var w = a.X * matrix[2] + a.Y * matrix[5] + matrix[8];

            if (w != 0)
            {
                output.X /= w; output.Y /= w;
            }

            return output;
        }
        
        public void Clear()
        {
            X = 0;
            Y = 0;
        }

        public double Norm()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public double SquareNorm()
        {
            return X * X + Y * Y;
        }

        public static double DotProd(Point2D a, Point2D b)
        {
            return a.X*b.X + a.Y*b.Y;
        }
    }
}