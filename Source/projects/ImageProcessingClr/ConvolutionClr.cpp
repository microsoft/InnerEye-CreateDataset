/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#include "ConvolutionClr.h"

#pragma managed(push, off)
#include "convolution.h"
#include "GaussianKernel1D.h"
#pragma managed(pop)

namespace InnerEye {
  namespace CreateDataset {
    namespace ImageProcessing {

      void Convolution::Convolve(array<float>^ data, int width, int height, int depth, array<Direction>^ directions, array<float>^ sigmas)
      {
        if (directions->Length != sigmas->Length)
          throw gcnew System::Exception("Arrays of directions and sigmas should be of the same length.");

        int leap = width*height*sizeof(float), stride = width*sizeof(float), hop=sizeof(float);

        pin_ptr<float> buffer = &data[0];
        
        try
        {
          for (int d = 0; d < directions->Length; d++)
          {
            createdataset::GaussianKernel1D kernel(sigmas[d]);
            Direction direction = directions[d];
            createdataset::convolve1d<float>(width, height, depth, (unsigned char*)buffer, leap, stride, hop, (int)direction, kernel.getData(), kernel.getRadius());
          }
        }
        catch (std::exception& oops)
        {
          throw gcnew System::Exception(gcnew System::String(oops.what()));
        }
      }

      void Convolution::Convolve(array<unsigned char>^ data, int width, int height, int depth, array<Direction>^ directions, array<float>^ sigmas)
      {
        if (directions->Length != sigmas->Length)
          throw gcnew System::Exception("Arrays of directions and sigmas should be of the same length.");

        int leap = width*height*sizeof(unsigned char), stride = width*sizeof(unsigned char), hop = sizeof(unsigned char);

        pin_ptr<unsigned char> buffer = &data[0];

        try
        {
          for (int d = 0; d < directions->Length; d++)
          {
            createdataset::GaussianKernel1D kernel(sigmas[d]);
            Direction direction = directions[d];
            createdataset::convolve1d<unsigned char>(width, height, depth, (unsigned char*)buffer, leap, stride, hop, (int)direction, kernel.getData(), kernel.getRadius());
          }
        }
        catch (std::exception& oops)
        {
          throw gcnew System::Exception(gcnew System::String(oops.what()));
        }
      }

      void Convolution::Convolve(array<short>^ data, int width, int height, int depth, array<Direction>^ directions, array<float>^ sigmas)
      {
        if (directions->Length != sigmas->Length)
          throw gcnew System::Exception("Arrays of directions and sigmas should be of the same length.");

        int leap = width*height*sizeof(short), stride = width*sizeof(short), hop = sizeof(short);

        pin_ptr<short> buffer = &data[0];

        try
        {
          for (int d = 0; d < directions->Length; d++)
          {
            createdataset::GaussianKernel1D kernel(sigmas[d]);
            Direction direction = directions[d];
            createdataset::convolve1d<short>(width, height, depth, (unsigned char*)buffer, leap, stride, hop, (int)direction, kernel.getData(), kernel.getRadius());
          }
        }
        catch (std::exception& oops)
        {
          throw gcnew System::Exception(gcnew System::String(oops.what()));
        }
      }
    }
  }
}