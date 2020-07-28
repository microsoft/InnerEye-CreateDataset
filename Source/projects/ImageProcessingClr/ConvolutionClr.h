/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#pragma once

namespace InnerEye { namespace CreateDataset { namespace ImageProcessing {

  public enum class Direction
  {
    DirectionX=0, DirectionY=1, DirectionZ=2
  };

  public ref class Convolution
  {
  public:
    // TODO: Various improvements are possible:
    // * Take a ROI as argument to avoid the need to extract subregion from managed array.
    // * Support out-of-place convolution too?
    // * Support arbtirary kernel in array.

    static void Convolve(array<float>^ data, int width, int height, int depth, array<Direction>^ directions, array<float>^ sigmas);

    static void Convolve(array<unsigned char>^ data, int width, int height, int depth, array<Direction>^ directions, array<float>^ sigmas);

    static void Convolve(array<short>^ data, int width, int height, int depth, array<Direction>^ directions, array<float>^ sigmas);
  };
} } }