///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Contours
{
    using System;
    using System.Drawing;

    /// <summary>
    /// Contains arithmetic operations for the <see cref="PointF"/> class.
    /// </summary>
    public static class PointExtensions
    {
        /// <summary>
        /// Computes a point that is the coordinate-wise subtraction of the arguments.
        /// In the result, X will be equal to this.X - p2.X.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static PointF Subtract (this PointF p1, PointF p2)
            => new PointF(p1.X - p2.X, p1.Y - p2.Y);

        /// <summary>
        /// Computes a point that is the coordinate-wise addition of the arguments.
        /// In the result, X will be equal to this.X + p2.X.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static PointF Add(this PointF p1, PointF p2)
            => new PointF(p1.X + p2.X, p1.Y + p2.Y);

        /// <summary>
        /// Computes the dot product of two points, interpreting them as vectors:
        /// For vectors (x1, y1) and (x2, y2), the dot product is
        /// x1 * x2 + y1 + y2.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static float DotProduct(this PointF p1, PointF p2)
            => p1.X * p2.X + p1.Y * p2.Y;

        /// <summary>
        /// Computes a point that is the component-wise multiplication of
        /// the arguments with the given scale.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static PointF Multiply(this PointF p, float scale)
            => new PointF(p.X * scale, p.Y * scale);

        /// <summary>
        /// Gets the sum of squares of the coordinates of the point
        /// (effectively treating it as a vector rather than a point).
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static float LengthSquared (this PointF p)
        {
            return p.X * p.X + p.Y * p.Y;
        }

        /// <summary>
        /// Treating the point as a vector, gets a vector that has the same direction
        /// as the argument but with length 1.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static PointF Normalize(this PointF p)
        {
            var length2 = p.LengthSquared();
            if (length2 == 0)
            {
                throw new ArgumentException("The point given has both coordinates set to 0, and hence can't be normalized.");
            }

            var length = (float)Math.Sqrt(length2);
            return new PointF(p.X / length, p.Y / length);
        }

        /// <summary>
        /// Creates a new instance of <see cref="PointF"/> from the arguments.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static PointF FromDouble(double x, double y)
            => new PointF((float)x, (float)y);
    }
}
