///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO
{
    using System;

    /// <summary>
    /// Stores a color as a (Red, Green, Blue) tuple
    /// </summary>
    [Serializable]
    public struct RGBValue
    {
        /// <summary>
        /// The Red component of the color.
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// The Green component of the color.
        /// </summary>
        public byte G { get; set; }

        /// <summary>
        /// The Blue component of the color.
        /// </summary>
        public byte B { get; set; }

        /// <summary>
        /// Creates a new instance of the class from the (R, G, B) values.
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        public RGBValue(byte red, byte green, byte blue)
        {
            R = red;
            G = green;
            B = blue;
        }
    }
}
