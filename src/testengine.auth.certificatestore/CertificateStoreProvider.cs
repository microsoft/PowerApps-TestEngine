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
    public class CertificateStoreProvider : IUserCertificateProvider
    {
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; private set; } = new string[] { "TestEngine" };

        internal static Func<X509Store> GetCertStore = () => new X509Store(StoreName.My, StoreLocation.CurrentUser);

        public string Name { get { return "certstore"; } }

        [ImportingConstructor]
        public CertificateStoreProvider(IFileSystem fileSystem)
        {
        }

        public X509Certificate2? RetrieveCertificateForUser(string userIdentifier)
        {
            if (string.IsNullOrEmpty(userIdentifier))
            {
                return null;
            }
            userIdentifier = userIdentifier.Trim();

            X509Store store = GetCertStore();
            store.Open(OpenFlags.ReadOnly);

            try
            {
                foreach (X509Certificate2 certificate in store.Certificates)
                {
                    if (certificate.SubjectName.Name != null && certificate.SubjectName.Name.Equals(userIdentifier, StringComparison.OrdinalIgnoreCase))
                    {
                        return certificate;
                    }
                }

                return null;
            }
            finally
            {
                store.Close();
            }
        }
    }
}
