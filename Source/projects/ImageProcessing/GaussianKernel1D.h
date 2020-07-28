/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#pragma once

#include <vector>

namespace createdataset
{
  class GaussianKernel1D
  {
  public:
    // Creates a Gaussian kernel with the specified sigma, truncates coefficients less than faction tol of max.
    GaussianKernel1D(float sigma, float tol = 0.001);

    // The radius of the kernel (array length size is 2*radius + 1)
    int getRadius() const;

    // A pointer to the beginning of the array of kernel coefficients.
    const float* getData() const;

    // Implementation
  private:
    int _radius;
    std::vector<float> _data;
  };
}