/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#include "stdafx.h"

#include "Stopwatch.h"

namespace createdataset
{
  Stopwatch::Stopwatch() {
    if (!::QueryPerformanceFrequency(&frequency_)) throw "Error with QueryPerformanceFrequency";
  }

  void Stopwatch::Start() {
    ::QueryPerformanceCounter(&startTime_);
  }

  void Stopwatch::Stop() {
    ::QueryPerformanceCounter(&stopTime_);
  }

  float Stopwatch::MilliSeconds() const {
    return (float)(stopTime_.QuadPart - startTime_.QuadPart) / (float)frequency_.QuadPart * 1000;
  }
}