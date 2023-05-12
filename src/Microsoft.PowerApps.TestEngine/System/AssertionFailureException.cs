// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.PowerApps.TestEngine.System
{
    public class AssertionFailureException : Exception
    {
        public AssertionFailureException()
        {
        }

        public AssertionFailureException(string message)
            : base(message)
        {
        }

        public AssertionFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
