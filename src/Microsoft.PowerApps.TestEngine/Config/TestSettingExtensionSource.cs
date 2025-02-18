// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Config
{
    public class TestSettingExtensionSource
    {
        public TestSettingExtensionSource()
        {
            EnableFileSystem = false;
            InstallSource.Add(Path.GetDirectoryName(this.GetType().Assembly.Location));
        }

#if RELEASE
        public bool EnableNuGet { get; } = false;
        public bool EnableFileSystem { get; } = false;
        public List<string> InstallSource { get; } = new List<string>();
#else
        public bool EnableNuGet { get; set; } = false;

        public bool EnableFileSystem { get; set; } = false;

        public List<string> InstallSource { get; set; } = new List<string>();
#endif
    }
}
