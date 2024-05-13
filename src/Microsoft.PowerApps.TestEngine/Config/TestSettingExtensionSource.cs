using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Config
{
    public class TestSettingExtensionSource
    {
        public TestSettingExtensionSource()
        {
            EnableFileSystem = true;
            InstallSource.Add(Path.GetDirectoryName(this.GetType().Assembly.Location));
        }

        public bool EnableNuGet { get; set; } = false;

        public bool EnableFileSystem { get; set; } = false;

        public List<string> InstallSource { get; set; } = new List<string>();
    }
}
