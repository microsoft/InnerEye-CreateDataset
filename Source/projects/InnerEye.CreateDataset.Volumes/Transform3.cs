///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Volumes
{
    using System;

    /// <summary>
    /// Models a 3D affine transformation. Modelled as Basis.x + Origin. Where Basis is a 3x3 matrix and Origin a Point3D
    /// </summary>
    public class Transform3
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transform3"/> class.
        /// </summary>
        /// <param name="basis">The basis.</param>
        /// <param name="origin">The origin.</param>
        /// <exception cref="ArgumentNullException">basis</exception>
        public Transform3(Matrix3 basis, Point3D origin)
        {
            Basis = basis ?? throw new ArgumentNullException(nameof(basis));
            Origin = origin; 
        }

        /// <summary>
        /// Returns the identity transform
        /// </summary>
        /// <returns>The Transform3 identity.</returns>
        public static Transform3 Identity()
        {
            return new Transform3(Matrix3.CreateIdentity(), Point3D.Zero());
        }

        /// <summary>
        /// Gets the basis.
        /// </summary>
        /// <value>
        /// The basis.
        /// </value>
        public Matrix3 Basis { get; }

        /// <summary>
        /// Gets or sets the origin.
        /// </summary>
        /// <value>
        /// The origin.
        /// </value>
        public Point3D Origin { get; set; }

        /// <summary>
        /// Transforms the given Point3D
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <param name="point">The point.</param>
        public static Point3D operator *(Transform3 transform, Point3D point)
        {
            return transform.Basis * point + transform.Origin;
        }

        /// <summary>
        /// Compute the composite transform a*b which is the operation of first performing b then a.
        /// (a*b)x = a(b(x)), a*b*x = a*(b*x) 
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static Transform3 operator *(Transform3 a, Transform3 b)
        {
            return new Transform3(a.Basis * b.Basis, a * b.Origin); 
        }

        /// <summary>
        /// Transforms the specified Point3D.
        /// </summary>
        /// <param name="point">The Point3D.</param>
        /// <returns>The transformed Point3D.</returns>
        public Point3D Transform(Point3D point)
        {
            return this * point;
        }

        /// <summary>
        /// Computes the inverse of this transform. 
        /// </summary>
        /// <exception cref="DivideByZeroException">if Basis.Determinant == 0</exception>
        /// <returns>The inverse of this instance.</returns>
        public Transform3 Inverse()
        {
            var basisInverse = Basis.Inverse();
            return new Transform3(basisInverse, -(basisInverse * Origin)); 
        }
    }
}
