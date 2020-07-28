///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    /// <summary>
    /// Contains commandline options that are shared across all operation models of the runner:
    /// dataset creation, analysis.
    /// </summary>
    public class CommandlineShared
    {
        /// <summary>
        /// Creates a new command line option instance, with all properties set to their default values.
        /// </summary>
        public CommandlineShared() { }

        /// <summary>
        /// Checks if the command line options are valid. Throws exceptions if any issues are found.
        /// </summary>
        virtual public void Validate() { }
    }
}
