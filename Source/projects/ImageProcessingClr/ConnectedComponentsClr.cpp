/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#include "ConnectedComponentsClr.h"

#include <cliext/vector>

#pragma managed(push, off)
#include "connectedComponents.h"
#pragma managed(pop)

namespace InnerEye {  namespace CreateDataset { namespace ImageProcessing {
      int ConnectedComponents::Find3d(
        array<unsigned char>^ image,
        int width, int height, int depth,
        unsigned char backgroundColour,
        array<unsigned short>^ output)
      {
        try
        {
          pin_ptr<unsigned char> inputBuffer = &image[0];
          pin_ptr<unsigned short> outputBuffer = &output[0];

          int inputLeap = width*height*sizeof(unsigned char), inputStride = width*sizeof(unsigned char);
          int outputLeap = width*height*sizeof(unsigned short), outputStride = width*sizeof(unsigned short);

          auto result = createdataset::findConnectedComponents3d<unsigned char, unsigned short>(
            width, height, depth,
            inputBuffer, inputLeap, inputStride, backgroundColour,
            outputBuffer, outputLeap, outputStride,
            0);

          return (int)(result.size());
        }
        catch (std::exception& oops)
        {
          throw gcnew System::Exception(gcnew System::String(oops.what()));
        }
      }

      array<ComponentStatistics>^ ConnectedComponents::Find3dWithStatistics(
        array<unsigned char>^ image,
        int width, int height, int depth,
        unsigned char backgroundColour,
        array<unsigned short>^ output)
      {
        try
        {
          pin_ptr<unsigned char> inputBuffer = &image[0];
          pin_ptr<unsigned short> outputBuffer = &output[0];

          int inputLeap = width*height*sizeof(unsigned char), inputStride = width*sizeof(unsigned char);
          int outputLeap = width*height*sizeof(unsigned short), outputStride = width*sizeof(unsigned short);

          auto result_ = createdataset::findConnectedComponents3d<unsigned char, unsigned short>(
            width, height, depth,
            inputBuffer, inputLeap, inputStride, backgroundColour,
            outputBuffer, outputLeap, outputStride,
            0);

          auto result = gcnew array<ComponentStatistics>(result_.size());
          pin_ptr<ComponentStatistics> p = &result[0];
          ::memcpy(p, &result_[0], result_.size()*sizeof(createdataset::ComponentStatistics<unsigned char,unsigned short>));

          return result;
        }
        catch (std::exception& oops)
        {
          throw gcnew System::Exception(gcnew System::String(oops.what()));
        }
      }
} } }