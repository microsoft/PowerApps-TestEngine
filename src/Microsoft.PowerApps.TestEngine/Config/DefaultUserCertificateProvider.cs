using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Config
{
    public class DefaultUserCertificateProvider : IUserCertificateProvider
    {
        public string Name => "default";

        public X509Certificate2 RetrieveCertificateForUser(string userIdentifier)
        {
            return null;
        }
    }
}
