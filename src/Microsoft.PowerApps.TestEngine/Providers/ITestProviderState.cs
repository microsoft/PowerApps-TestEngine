// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public interface ITestProviderState
    {
        public object GetState();
    }
}
