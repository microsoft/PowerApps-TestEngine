// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerApps.TestEngine.System;

namespace testengine.plugin.copilot
{
    /// <summary>
    /// Create an in memory version of environment variables so that test engine state can managed
    /// </summary>
    public class InMemoryEnvironment : IEnvironmentVariable
    {
        public Dictionary<string, string> Values { get; private set; } = new Dictionary<string, string>();
        public string GetVariable(string name)
        {
            return Values.ContainsKey(name) ? Values[name] : null;
        }
    }
}
