// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Security.Cryptography.X509Certificates;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;

namespace testengine.auth
{
    /// <summary>
    /// Functions for interacting with the Power App
    /// </summary>
    [Export(typeof(IUserCertificateProvider))]
    public class LocalUserCertificateProvider : IUserCertificateProvider
    {
        public string Name { get { return "localcert"; } }

        private readonly IFileSystem _fileSystem;

        private Dictionary<string, X509Certificate2> emailCertificateDict = new Dictionary<string, X509Certificate2>();

        [ImportingConstructor]
        public LocalUserCertificateProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            var certDir = Path.Combine(_fileSystem.GetDefaultRootTestEngine(), "LocalCertificates");
            var password = "";
            if (_fileSystem.Exists(certDir))
            {
                string[] pfxFiles = _fileSystem.GetFiles(certDir, "*.pfx");
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
