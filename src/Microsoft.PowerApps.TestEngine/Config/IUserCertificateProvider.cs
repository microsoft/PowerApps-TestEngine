// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Config
{
    public interface IUserCertificateProvider
    {
        /// <summary>
        /// The namespace of namespaces that this provider relates to
        /// </summary>
        public string[] Namespaces { get; }

        public string Name { get; }
        public X509Certificate2 RetrieveCertificateForUser(string userIdentifier);
    }
}
