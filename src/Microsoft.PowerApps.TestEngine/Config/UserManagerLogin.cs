// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.Config
{
    public class UserManagerLogin : IUserManagerLogin
    {
        private readonly IUserCertificateProvider _userCertificateProvider;
        // add any other interfaces and create instances 
        //private readonly IEnvironmentVariable _environmentVariableProvider;

        public UserManagerLogin(IUserCertificateProvider userCertificateProvider)//, IEnvironmentVariable environmentVariableProvider)
        {
            _userCertificateProvider = userCertificateProvider;
            //_environmentVariableProvider = environmentVariableProvider;
        }

        public IUserCertificateProvider UserCertificateProvider => _userCertificateProvider;
        //public IEnvironmentVariable EnvironmentVariableProvider => _environmentVariableProvider;
    }
}
