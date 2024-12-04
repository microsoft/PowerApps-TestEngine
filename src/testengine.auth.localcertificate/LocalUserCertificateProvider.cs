// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Security.Cryptography.X509Certificates;
using Microsoft.PowerApps.TestEngine.Config;

namespace testengine.auth
{
    /// <summary>
    /// Functions for interacting with the Power App
    /// </summary>
    [Export(typeof(IUserCertificateProvider))]
    public class LocalUserCertificateProvider : IUserCertificateProvider
    {
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; private set; } = new string[] { "Deprecated" };

        public string Name { get { return "localcert"; } }

        private Dictionary<string, X509Certificate2> emailCertificateDict = new Dictionary<string, X509Certificate2>();

        public LocalUserCertificateProvider()
        {
            var certDir = "LocalCertificates";
            var password = "";
            if (Directory.Exists(certDir))
            {
                string[] pfxFiles = Directory.GetFiles(certDir, "*.pfx");

                foreach (var pfxFile in pfxFiles)
                {
                    // Load the certificate
                    X509Certificate2 cert = new X509Certificate2(pfxFile, password);
                    emailCertificateDict.Add(cert.SubjectName.Name, cert);
                }
            }
        }

        public X509Certificate2? RetrieveCertificateForUser(string userIdentifier)
        {
            if (emailCertificateDict.TryGetValue(userIdentifier, out X509Certificate2 certificate))
            {
                return certificate;
            }
            else
            {
                return null;
            }
        }
    }
}
