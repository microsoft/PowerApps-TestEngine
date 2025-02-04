// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Users;

namespace testengine.common.user
{
    public class LoginState
    {
        public IConfigurableUserManager? Module { get; set; }

        public IPage? Page { get; set; }
        public string? UserEmail { get; set; }
        public string? DesiredUrl { get; set; }


        public bool IsError { get; set; }
        public bool FoundMatch { get; set; }
        public bool CallbackDesired { get; set; }
        public bool EmailHandled { get; set; }
        public string? MatchHost { get; set; }


        public Func<string, Task> CallbackDesiredUrlFound { get; set; } = null;
        public Func<Task> CallbackErrorFound { get; set; } = null;
    }
}
