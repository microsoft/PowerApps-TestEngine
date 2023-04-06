// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// Contains information on run of a case
    /// </summary>
    public class CaseInfo
    {
        public string name {get; set;}

        public bool casePassed {get; set;} = false;

        public string exception {get; set;}
    }
}
