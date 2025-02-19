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
    public class CertificateEnvironmentProvider : IUserCertificateProvider
    {
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; private set; } = new string[] { "TestEngine" };

        public string Name { get { return "certenv"; } }

        private IEnvironmentVariable _environment { get; set; }

        [ImportingConstructor]
        public CertificateEnvironmentProvider(IEnvironmentVariable environment)
        {
            _environment = environment;
        }

        public X509Certificate2? RetrieveCertificateForUser(string userIdentifier)
        {
            if (string.IsNullOrEmpty(userIdentifier))
            {
                return null;
            }
            userIdentifier = userIdentifier.Trim();

            var base64Encoded = _environment.GetVariable(userIdentifier);

            if (string.IsNullOrEmpty(base64Encoded))
            {
                return null;
            }
        
            // Convert the base64 string to a byte array
            byte[] rawData = Convert.FromBase64String(base64Encoded);

            // Create a new X509Certificate2 object from the byte array
            return new X509Certificate2(rawData);
        }
    }
}
