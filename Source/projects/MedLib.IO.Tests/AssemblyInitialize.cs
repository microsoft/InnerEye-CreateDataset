///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿using System.Diagnostics;
using NUnit.Framework;

/// <summary>
/// Contains code that is called by the test runner at assembly loading time.
/// This class must not be in a namespace for NUnit.
/// https://stackoverflow.com/questions/3188380/one-time-initialization-for-nunit
/// </summary>
[SetUpFixture]
public class AssemblyInitialize
{
    [OneTimeSetUp]
    public static void SetCurrentDirectory()
    {
        // This is necessary because NUnit3 sets current directory to c:\windows\system32,
        // but we use many paths that are relative to the build output directory.
        System.Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }
}
