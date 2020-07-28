///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using NUnit.Framework;
using System;

namespace InnerEye.Tests.Common
{
    public static class TestingExtension
    {
        public static void Throws<T>(Action task, string expectedMessage = "") where T : Exception
        {
            try
            {
                task();
            }
            catch (Exception ex)
            {
                if (expectedMessage != "")
                {
                    Assert.AreEqual(expectedMessage, ex.Message);
                }
                Assert.AreEqual(typeof(T), ex.GetType());
                return;
            }

            if (typeof(T).Equals(new Exception().GetType()))
            {
                Assert.Fail("Expected exception but no exception was thrown.");
            }
            else
            {
                Assert.Fail(string.Format("Expected exception of type {0} but no exception was thrown.", typeof(T)));
            }
        }
    }
}
