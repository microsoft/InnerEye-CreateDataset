/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#pragma once

#include <vector>
#include <algorithm>
#include <memory>
#include <limits>

#include <omp.h>

#include "SseConvolver.h"

namespace createdataset
{
  // Templates used only via explicit instantiation to prevent application to unsupported types.
  template<typename T>
  inline void writerT(float x, T* iterator);

  template<typename T>
  inline float readerT(const T* iterator);

  template<>
  inline void writerT<float>(float x,float* iterator)
  {
    *iterator = x;
  }

  template<>
  inline float readerT <float> (const float* iterator)
  {
    return *iterator;
  }

  template<>
  inline void writerT<unsigned char>(float x, unsigned char* iterator)
  {
    *iterator = x <= 0.0f ? 0 : (x>255 ? 255 : (unsigned char)(x + 0.5f));
  }

  template<>
  inline float readerT<unsigned char>(const unsigned char* iterator)
  {
    return float(*iterator);
  }
  
  template<>
  inline void writerT<short>(float x, short* iterator)
  {
    short minimum = std::numeric_limits<short>::min(), maximum = std::numeric_limits<short>::max();
    *iterator = x <= minimum ? minimum : (x>maximum ? maximum : (short)(x + 0.5f));
  }

  template<>
  inline float readerT<short>(const short* iterator)
  {
    return float(*iterator);
  }

  // Simple, good implementation for reference
  inline void convolve_reference(const float* input, int width, const float* kernel, int kernelRadius, float* output)
  {
    const float* i = input;
    float* o = output;
    for (int u = 0; u < width - 2*kernelRadius; u++)
    {
      float sum = 0.0f;
      for (int k = 0; k < 2*kernelRadius+1; k++)
      {
        sum += kernel[k] * i[k];
      }
      *o++ = sum;
      i++;
    }
  }

  // Convolve 2D image of pixel type T with 1D kernel using multiple threads
  template<typename T, typename float(*Reader)(const T*) = readerT<T>, typename void(*Writer)(float, T*)=writerT<T> >
  void convolve(
    int width, int height,
    unsigned char* buffer, int hop, int stride,
    const float* kernel, int kernelRadius  )
  {
    const int threadCount = omp_get_max_threads();

    struct ThreadData {
      ThreadData(int width, const float* kernel, int kernelRadius) :
        inputRow(width+2*kernelRadius), outputRow(width) /*, convolver(kernel, 2 * kernelRadius + 1, width)*/
      {
      }

      std::vector<float> inputRow;
      std::vector<float> outputRow;
      //SseConvolver convolver;
    };

    std::vector<std::shared_ptr<ThreadData>> data(threadCount);
    for (int i = 0; i < threadCount; i++)
      data[i] = std::shared_ptr<ThreadData>(new ThreadData(width, kernel, kernelRadius));

#pragma omp parallel for num_threads(threadCount)
    for (int v = 0; v < height; v++)
    {
      int rank = omp_get_thread_num();
      ThreadData& t = *data[rank];

      float* p = &(t.inputRow[kernelRadius]);
      const unsigned char* q = buffer + v*stride;
      for (int u = 0; u < width; u++)
      {
        *p++ = Reader((T*)(q));
        q += hop;
      }
      // mirror edges
      for (int u = 0; u < kernelRadius; u++)
      {
        t.inputRow[kernelRadius - 1 - u] = t.inputRow[kernelRadius + u ];
        t.inputRow[kernelRadius + width + u] = t.inputRow[kernelRadius + width - 1 - u];
      }

      // Do the 1D convolution

      // SSE version: t.convolver.convolve(&(t.inputRow[0]), &(t.outputRow[0]));

      convolve_reference(
        &(t.inputRow[0]),
        width + 2*kernelRadius,
        kernel, kernelRadius,
        &(t.outputRow[0]));

      // Copy the result back to the input volume
      unsigned char* r = buffer + v*stride;
      const float * s = &t.outputRow[0];
      for (int u = 0; u < width; u++)
      {
        Writer(*s++, (T*)(r));
        r += hop;
      }
    }
  }

  // Convolve a 3D volume of pixel type T with a 1D kernel.
  template<typename T>
  void convolve1d(
    int width, int height, int depth,
    unsigned char* buffer, int leap, int stride, int hop,
    int direction,
    const float* kernel, int kernelWidth)
  {
    switch (direction)
    {
    case 0:
      for (int z = 0; z < depth; z++)
        createdataset::convolve<T>(width, height, &buffer[z*leap], hop, stride, kernel, kernelWidth);
            break;
    case 1:
      for (int z = 0; z < depth; z++)
        createdataset::convolve<T>(height, width, &buffer[z*leap], stride, hop, kernel, kernelWidth);
      break;
    case 2:
      for (int y = 0; y < height; y++)
        createdataset::convolve<T>(depth, width, &buffer[y*stride], leap, hop, kernel, kernelWidth);
      break;
    default:
      throw std::exception("Direction was out of range.");
    }
  }
}