using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Config
{
    public interface IUserCertificateProvider
    {
        public string Name { get; }
        public X509Certificate2 RetrieveCertificateForUser(string username);
    }
}
