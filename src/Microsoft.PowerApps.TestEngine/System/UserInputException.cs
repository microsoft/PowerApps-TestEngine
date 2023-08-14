// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.System
{
    public class UserInputException : Exception
    {
        public enum errorMapping
        {
            UserInputExceptionAppURL,
            UserInputExceptionInvalidFilePath,
            UserInputExceptionLoginCredential,
            UserInputExceptionTestConfig,
            UserInputExceptionYAMLFormat
        };

        public UserInputException()
        {
        }

        public UserInputException(string message)
            : base(message)
        {
        }

        public UserInputException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
