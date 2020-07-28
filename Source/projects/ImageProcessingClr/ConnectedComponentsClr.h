/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#pragma once

namespace InnerEye { namespace CreateDataset {  namespace ImageProcessing
{
  public value struct ComponentStatistics
  {
  public:
    unsigned long PixelCount;
    unsigned char InputLabel;
  };

  public ref class ConnectedComponents
  {
  public:
    // Find connected components in 3D volume using one pass unite-find approach and
    // label associated voxels in output volume. Voxels with the specified background colour
    // are all assigned the background label.
    // Returns the number of connected components (6-connected (aka face) components ie: diagnoal points are considered separate components) found, including the background class.
    static int Find3d(array<unsigned char>^ image, int width, int height, int depth, unsigned char backgroundColour, array<unsigned short>^ result);

    static array<ComponentStatistics>^ Find3dWithStatistics(array<unsigned char>^ image, int width, int height, int depth, unsigned char backgroundColour, array<unsigned short>^ result);
    // NB could easily extend to support different pixel types, image padding, etc.
  };
} } }