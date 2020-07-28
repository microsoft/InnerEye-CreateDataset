/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#pragma once

#include <vector>

#include <pmmintrin.h>
#include <xmmintrin.h>
#include <immintrin.h>

#include "AlignmentAllocator.h"

namespace createdataset
{
  // Fast convolution of 1D vector with 1D kernel using SSE intrinsics.
  class SseConvolver
  {
    std::vector<std::vector<float, AlignmentAllocator<float, 32>>> in_aligned;
    std::vector<__m128, AlignmentAllocator<__m128, 32>> kernel_aligned_;
    std::vector<float> kernel_;
    int kernel_radius_;
    int padBytes_;
    int length_;

  public:
    SseConvolver(const float* kernel, int kernel_length, int length) :
      in_aligned(4),
      kernel_radius_(kernel_length / 2),
      length_(length)
    {
      if (length <= kernel_length)
        throw std::exception("Kernel too big for image.");

      // Pad kernel to multiple of 4 bytes
      padBytes_ = (4 - kernel_length % 4) % 4;
      kernel_.resize(kernel_length + padBytes_, 0.0f);
      memcpy(&kernel_[0], kernel, kernel_length*sizeof(float));

      // Allocate aligned storage for the kernel
      kernel_aligned_.resize(kernel_.size());

      // Repeat the kernel across the vector
      std::vector<float, AlignmentAllocator<float, 32>> kernel_block(4);
      for (int i = 0; i < kernel_length; i++)
      {
        kernel_block[0] = kernel[i];
        kernel_block[1] = kernel[i];
        kernel_block[2] = kernel[i];
        kernel_block[3] = kernel[i];

        kernel_aligned_[i] = _mm_load_ps(&kernel_block[0]);
      }

      // Allocate aligned storage for 4 copies of input data, padded by same amount as kernel
      for (int l = 0; l < 4; l++)
        in_aligned[l].resize(length + padBytes_, 0.0f);
    }

    void convolve(const float* in, float* out)
    {
      // Must make four copies of the data for four different memory alignments
      for (int i = 0; i < 4; i++)
        memcpy(&(in_aligned[i][0]), in + i, (length_ - i)*sizeof(float));

      // Need to do the left hand side as a special case

      // Now perform the convolution using SSE
      for (int i = 0; i < length_ + padBytes_ - (int)(kernel_.size()); i += 4)
      {
        __m128 accumulator = _mm_setzero_ps();

        for (int k = 0; k < (int)(kernel_.size()); k += 4)
        {
          int data_offset = i + k;

          for (int l = 0; l < 4; l++) // compiler will unroll
          {
            // TODO: Compare a version where we shift the kernel and't don't make four copies of data
            __m128 data_block = _mm_load_ps(&(in_aligned[l][0]) + data_offset);
            __m128 products = _mm_mul_ps(kernel_aligned_[k + l], data_block);

            accumulator = _mm_add_ps(accumulator, products);
          }
        }
        _mm_storeu_ps(out + kernel_radius_ + i, accumulator); // offset by original kernel width

        // NB. Could deal with non multiple of 4 kernel as special case here instead of padding in constructor
      }

      // Need to do the last value as a special case
      int i = length_ - (int)(kernel_.size());
      out[i] = 0.0;
      for (int k = 0; k < (int)(kernel_.size()); k++)
        out[i + kernel_radius_] += in_aligned[0][i + k] * kernel_[kernel_.size() - k - 1];
    }
  };

}
