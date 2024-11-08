using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Users
{
    public interface IConfigurableUserManager : IUserManager
    {
        public Dictionary<string, object> Settings { get; }
    }
}
