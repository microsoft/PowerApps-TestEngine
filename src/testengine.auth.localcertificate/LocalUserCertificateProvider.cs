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
    public class LocalUserCertificateProvider: IUserCertificateProvider
    {
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
                    string fileName = Path.GetFileNameWithoutExtension(pfxFile);
                    emailCertificateDict.Add(fileName, cert);
                }
            }
        }

        public X509Certificate2? RetrieveCertificateForUser(string username)
        {
            if (emailCertificateDict.TryGetValue(username, out X509Certificate2 certificate))
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
