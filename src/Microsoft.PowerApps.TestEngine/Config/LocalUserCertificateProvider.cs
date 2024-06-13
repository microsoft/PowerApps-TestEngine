using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.PowerApps.TestEngine.Config
{
    public class LocalUserCertificateProvider : IUserCertificateProvider
    {
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
                    try
                    {
                        // Load the certificate
                        X509Certificate2 cert = new X509Certificate2(pfxFile, password);
                        string fileName = Path.GetFileNameWithoutExtension(pfxFile);
                        emailCertificateDict.Add(fileName, cert);
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions (e.g., incorrect password, invalid certificate file)
                        Console.WriteLine($"Error loading certificate from file '{pfxFile}': {ex.Message}");
                    }
                }
            }
        }

        public X509Certificate2 RetrieveCertificateForUser(string username)
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
