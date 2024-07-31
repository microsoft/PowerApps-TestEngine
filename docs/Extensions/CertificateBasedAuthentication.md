# Certificate based authentication mechanism

Certificate-based authentication (CBA) in Power Apps environments provides a robust way to secure access to your apps and data by using digital certificates instead of traditional username and password credentials. 
This document outlines the feature to configure and author tests that use CBA allowing the Test engine to login and succeed in running the tests.

An additional option to the existing "UserAuth" ("-u") input option is available where the value for this flag can be set to "certificate" (Previously only "browser" and "environment" options were available). With certificate option the username for the user that runs the test is still fetched from the environment variable configured from the test YAML file, however the certificate fetched using the IUserCertificateProvider implementation that can be plugged in as a MEF module. IUserCertificateProvider provides a X509Certificate2 object for a given username.

Multiple certificate stores can be added in that offload certificate retrieval into a separate module. A default certificate provider is implemented and can be configured as part of the test configuration using input option "UserAuthType" ("-a") with value defaulting to "localcert". This is a sample implementation that fetches all the pfx files from the "LocalCertificates" folder at the base of the executable and creates a dictionary that has the pfx file name as key and the file as value. Another such implemntation is "certstore" that fetches the certificate from the local certificate store based on the subjectname, here the expectation is that the username and subject name or subject alternate name will match.

Thus to enable usage of CBA the following changes are required in the YAML. 
1. Configure UserAuth as certificate
2. Configure UserAuthType as localcert (optional unless a different IUserCertificateProvider implementation is provided)
3. If localcert is used then generate a folder LocalCertificates at the executable base and place the pfx for the certificate in it with the file name of the pfx as the username.