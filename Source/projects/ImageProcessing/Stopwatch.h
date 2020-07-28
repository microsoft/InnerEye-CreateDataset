/*  ------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
 *  ------------------------------------------------------------------------------------------
 */

#pragma once

#include <windows.h> // TODO: Preferable to remove this from header

namespace createdataset
{
  class Stopwatch {
    LARGE_INTEGER frequency_;
    LARGE_INTEGER startTime_;
    LARGE_INTEGER stopTime_;

  public:
    Stopwatch();

    void Start();
    void Stop();

    float MilliSeconds() const;
  };
}