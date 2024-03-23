// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.System
{
    public class UserInputException : Exception
    {
        // Error Mapping keys for hanlding user input exception
        // This can be identified by the event handler for specifically handling error messages for these scenarios
        public enum ErrorMapping
        {
            UserInputExceptionInvalidFilePath,
            UserInputExceptionInvalidOutputPath,
            UserInputExceptionInvalidTestSettings,
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
