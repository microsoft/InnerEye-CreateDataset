/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#include "StdAfx.h"
#include "GaussianKernel1D.h"

#include <math.h>
#include <iostream>

namespace createdataset
{
  GaussianKernel1D::GaussianKernel1D(float sigma, float tol)
  {
    if (sigma < 0.0f)
      sigma = -sigma;

    if (tol < 0.0f)
      tol = -tol;

    // Because the chances are high that someone has #define'd PI.
    const float PACKAGE_PI = 3.141592f;

    _radius = static_cast<int>(floor(sigma * sqrt(2 * log(1 / tol))));
    _data.resize(2 * _radius + 1);

    float sum = 0.0;
    for (int x = -_radius; x <= _radius; x++)
    {
      _data[_radius + x]
        = static_cast<float>((1 / (sigma*sqrt(2 * PACKAGE_PI))) * exp(-0.5 * pow(x / sigma, 2)));

      sum += _data[_radius + x];
    }
  }

  int GaussianKernel1D::getRadius() const
  {
    return _radius;
  }

  const float* GaussianKernel1D::getData() const
  {
    return &_data[0];
  }
}