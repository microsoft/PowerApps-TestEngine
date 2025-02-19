// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;

namespace Microsoft.PowerApps.TestEngine.System
{
    [Export(typeof(IEnvironmentVariable))]
    public class EnvironmentVariable : IEnvironmentVariable
    {
        public string GetVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }
    }
}
