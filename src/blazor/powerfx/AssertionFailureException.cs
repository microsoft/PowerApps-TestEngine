// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    [Serializable]
    internal class AssertionFailureException : Exception
    {
        public AssertionFailureException()
        {
        }

        public AssertionFailureException(string? message) : base(message)
        {
        }

        public AssertionFailureException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}