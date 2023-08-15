// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.System
{
    public class UserAppException : Exception
    {

        public UserAppException()
        {
        }

        public UserAppException(string message)
            : base(message)
        {
        }

        public UserAppException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
