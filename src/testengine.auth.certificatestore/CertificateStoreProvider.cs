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
    public class CertificateStoreProvider : IUserCertificateProvider
    {
        internal static Func<X509Store> GetCertStore = () => new X509Store(StoreName.My, StoreLocation.CurrentUser);

        public string Name { get { return "certstore"; } }

        public CertificateStoreProvider()
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
                    if (certificate.SubjectName.Name != null && certificate.SubjectName.Name.Contains(userIdentifier, StringComparison.OrdinalIgnoreCase))
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
