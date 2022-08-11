// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.System
{
    public class EnvironmentVariable : IEnvironmentVariable
    {
        public string GetVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }
    }
}
