using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace testengine.user.certificate
{
    internal interface ICertificateAuthOptions
    {
        /// <summary>
        /// The certificate for the user as a pfx buffer or base64 string
        /// </summary>
        public string pfx { get; set; }

        /// <summary>
        /// Optional passphrase for the certificate. Note: Certificate retrieve from Key Vault do not have a password.
        /// </summary>
        public string? passphrase { get; set; }

        /// <summary>
        /// Optional.The authentication endpoint e.g.login.microsoftonline.com or login.microsoftonline.us
        /// </summary>
        public string? authEndpoint { get; set; }
    }
}
