// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
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

        public X509Certificate2? RetrieveCertificateForUser(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Username cannot be null or empty", nameof(username));
            }
            username = username.Trim();

            X509Store store = GetCertStore();
            store.Open(OpenFlags.ReadOnly);

            try
            {
                foreach (X509Certificate2 certificate in store.Certificates)
                {
                    if (certificate.SubjectName.Name != null && certificate.SubjectName.Name.Contains($"CN={username}", StringComparison.OrdinalIgnoreCase))
                    {
                        return certificate;
                    }

                    foreach (X509Extension extension in certificate.Extensions)
                    {
                        if (extension.Oid.Value.Equals("2.5.29.17")) //san
                        {
                            var asnEncodedData = new AsnEncodedData(extension.Oid, extension.RawData);
                            string sanString = asnEncodedData.Format(false);
                            if (sanString.Contains(username, StringComparison.OrdinalIgnoreCase))
                            {
                                return certificate;
                            }
                        }
                    }
                }

                throw new InvalidOperationException($"Certificate for user {username} not found.");
            }
            finally
            {
                store.Close();
            }
        }
    }
}
