///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace MedLib.IO.Tests
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using InnerEye.CreateDataset.Math;
    using InnerEye.CreateDataset.Volumes;

    using NUnit.Framework;

    [TestFixture]
    public class EuclideanDistanceTests
    {
        [TestCase(@"LoadTest1\\triangle.png")]
        public void EuclideanDistanceTest(string filename)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", filename);
            string resultPath = Path.GetDirectoryName(filePath) + @"\\result.png";
            
            var image = new Bitmap(filePath);
            byte[] mask = ImageToByte(image);

            var mask2d = new InnerEye.CreateDataset.Volumes.Volume2D<byte>(mask, image.Width, image.Height, 1, 1, new Point2D(), Matrix2.CreateIdentity());

            var contours = mask2d.ContoursWithHoles();
            mask2d.Fill(contours, (byte)1);

            var contourMask = new InnerEye.CreateDataset.Volumes.Volume2D<byte>(image.Width, image.Height, 1, 1, new Point2D(), Matrix2.CreateIdentity());

            foreach (var point in contours.SelectMany(x => x.ContourPoints))
            {
                var index = contourMask.GetIndex((int)point.X, (int)point.Y);
                contourMask[index] = 1;
            }

            var distanceMap = contourMask.EuclideanDistance();
#if DEBUG
            PrintByteArray(distanceMap.Array, image.Width, image.Height, resultPath);
#endif
        }

        public static void PrintByteArray(float[] img, int dimX, int dimY, string resultPath)
        {
            Bitmap plane = new Bitmap(dimX, dimY);

            for (int y = 0; y < dimY; y++)
            {
                for (int x = 0; x < dimX; x++)
                {
                    var index = x + y * dimX;
                    var colorValue = (int)img[index] == 1 ? 255 : 0;
                    plane.SetPixel(x, y, Color.FromArgb(colorValue, colorValue, colorValue));
                }
            }

            plane.Save(resultPath);
        }

        public static byte[] ImageToByte(Bitmap img)
        {
            var array = new byte[img.Width * img.Height];

            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    // Get the color of a pixel within myBitmap.
                    Color pixelColor = img.GetPixel(x, y);
                    var index = x + y * img.Width;
                    array[index] = (byte)(pixelColor.R == 0 ? 1 : 0);
                }
            }

            return array;
        }
    }
}
