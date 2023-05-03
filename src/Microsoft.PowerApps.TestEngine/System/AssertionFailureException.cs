// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerApps.TestEngine.System
{
    public class AssertionFailureException : Exception
    {
        public AssertionFailureException(string message)
            : base(message)
        {
        }
    }
}
