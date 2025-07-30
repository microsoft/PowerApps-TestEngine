// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerApps.TestEngine.Users;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace testengine.user.storagestate
{
    /// <summary>
    /// Implement single sign on to a test resource from a login.microsoftonline.com resource assuming storage state stored in Dataverse using ASP.Net Data Protection API
    /// </summary>
    /// <remarks>
    ///Requires cookies 
    /// </remarks>
    [Export(typeof(IUserManager))]
    public class DataverseStorageStateUserManagerModule : StorageStateUserManagerModule
    {
        IEnvironmentVariable _environmentVariable;
        ITestState _testState;
        IUserCertificateProvider _userCertificateProvider;
        IDataProtector _userProtector;
        IOrganizationService _organizationService;
        ISingleTestInstanceState _singleTestInstanceState;
        ServiceProvider _services = null;
        bool _setup = false;

        public const string LOAD_SETTINGS = "LoadState";

        public const string DATAVERSE_KEY = "DataverseKey";

        public const string DATA_PROTECTION_URL = "DataProtectionUrl";

        public const string DATA_PROTECTION_CERTIFICATE_NAME = "DataProtectionCertificateName";

        public DataverseStorageStateUserManagerModule() : base()
        {
            if (Settings.ContainsKey(LOAD_SETTINGS))
            {
                Settings.Remove(LOAD_SETTINGS);
            }
            Settings.Add(LOAD_SETTINGS, LoadStateIfExists());
        }

        public void SetupState(IXmlRepository xmlRepository = null)
        {
            if (_setup)
            {
                return;
            }

            var serviceCollection = new ServiceCollection();

            ILogger logger = null;

            IOrganizationService serviceClient = null;

            foreach (var key in Settings.Keys)
            {
                if (Settings[key] is IEnvironmentVariable)
                {
                    _environmentVariable = (IEnvironmentVariable)Settings[key];
                }
                if (Settings[key] is ITestState)
                {
                    _testState = (ITestState)Settings[key];
                }
                if (Settings[key] is ISingleTestInstanceState)
                {
                    _singleTestInstanceState = (ISingleTestInstanceState)Settings[key];
                }
                if (Settings[key] is IUserCertificateProvider)
                {
                    _userCertificateProvider = (IUserCertificateProvider)Settings[key];
                }
                if (Settings[key] is IOrganizationService)
                {
                    serviceClient = (IOrganizationService)Settings[key];
                }
            }

            logger = _singleTestInstanceState?.GetLogger();

            var api = new Uri(_environmentVariable.GetVariable(DATA_PROTECTION_URL));

            var dataProtectionCertificate = _environmentVariable.GetVariable(DATA_PROTECTION_CERTIFICATE_NAME);

            _organizationService = serviceClient == null ? new ServiceClient(api, (url) => Task.FromResult(new AzureCliHelper().GetAccessToken(api))) : serviceClient;

            serviceCollection.AddSingleton<IOrganizationService>(_organizationService);
            serviceCollection.AddSingleton<ILogger>(logger);

            var cert = _userCertificateProvider.RetrieveCertificateForUser(dataProtectionCertificate);

            string dataProtectionKey = _environmentVariable.GetVariable(DATAVERSE_KEY);

            var keyName = !String.IsNullOrEmpty(dataProtectionKey) ? dataProtectionKey : Environment.MachineName;

            if (cert == null)
            {
                throw new InvalidDataException($"Certificate {dataProtectionCertificate} not found");
            }

            if (!PlatformHelper.IsWindows())
            {
                throw new ApplicationException();
            }

            serviceCollection.AddDataProtection()
                .SetApplicationName("TestEngine")
                .ProtectKeysWithCertificate(cert)
                .AddKeyManagementOptions(options =>
                {
                    options.XmlRepository = (xmlRepository == null) ? new DataverseKeyStore(_services?.GetRequiredService<ILogger>(), _organizationService, keyName) : xmlRepository;
                });
            _services = serviceCollection.BuildServiceProvider();

            _userProtector = _services.GetDataProtector("ASP Data Protection");

            Protect = (IFileSystem filesystem, string fileName) =>
            {
                SetupState();

                var content = filesystem.ReadAllText(fileName);
                filesystem.Delete(fileName);

                var keyName = _environmentVariable.GetVariable(DATAVERSE_KEY);
                var machine = !String.IsNullOrEmpty(keyName) ? keyName : Environment.MachineName;

                var protectedValue = _userProtector.Protect(content);

                StoreValue(_organizationService, machine, GetTestPersonaUserName(), protectedValue);
            };

            _setup = true;
        }

        public override string Name => "dataverse";

        private Func<IEnvironmentVariable, ISingleTestInstanceState, ITestState, IFileSystem, string> LoadStateIfExists()
        {
            return (IEnvironmentVariable environmentVariable, ISingleTestInstanceState singleTestInstanceState, ITestState testState, IFileSystem fileSystem) =>
            {
                SetupState();

                var keyName = _environmentVariable.GetVariable(DATAVERSE_KEY);
                var machine = !String.IsNullOrEmpty(keyName) ? keyName : Environment.MachineName;
                var persona = GetTestPersonaUserName();

                var matches = FindMatch(_organizationService, machine, $"{machine}-{persona}");

                if (matches.Count() > 0)
                {
                    return _userProtector.Unprotect(matches.First().Data);
                }

                return String.Empty;
            };
        }

        public static IReadOnlyCollection<ProtectedKeyValue> FindMatch(IOrganizationService service, string keyName, string valueName)
        {
            // Retrieve keys from Dataverse
            FilterExpression filter = new FilterExpression(LogicalOperator.And);
            filter.Conditions.Add(new ConditionExpression("te_keyname", ConditionOperator.Equal, keyName));
            filter.Conditions.Add(new ConditionExpression("te_valuename", ConditionOperator.Equal, valueName));

            var query = new QueryExpression("te_keydata")
            {
                ColumnSet = new ColumnSet("te_keyname", "te_valuename", "te_data"),
                Criteria = filter
            };

            var keys = service.RetrieveMultiple(query)
            .Entities
            .Select(static e => new ProtectedKeyValue
            {
                KeyId = e.Id.ToString(),
                KeyName = e["te_keyname"]?.ToString(),
                ValueName = e["te_valuename"]?.ToString(),
                Data = e["te_data"]?.ToString(),
            })
            .ToList();

            return keys.AsReadOnly();
        }

        public static void StoreValue(IOrganizationService service, string keyName, string valueName, string data)
        {
            FilterExpression filter = new FilterExpression(LogicalOperator.And);
            filter.Conditions.Add(new ConditionExpression("te_keyname", ConditionOperator.Equal, keyName));
            filter.Conditions.Add(new ConditionExpression("te_valuename", ConditionOperator.Equal, $"{keyName}-{valueName}"));

            var query = new QueryExpression("te_keydata")
            {
                ColumnSet = new ColumnSet("te_keyname", "te_valuename", "te_data"),
                Criteria = filter
            };

            var match = service.RetrieveMultiple(query).Entities;

            if (match.Count > 0)
            {
                // Update match
                var first = match.First();
                first["te_data"] = data;
                service.Update(first);
            }
            else
            {
                var keyEntity = new Entity("te_keydata")
                {
                    ["te_keyname"] = keyName,
                    ["te_valuename"] = $"{keyName}-{valueName}",
                    ["te_data"] = data,
                };

                service.Create(keyEntity);
            }
        }

        public string GetTestPersonaUserName()
        {
            var testSuiteDefinition = _testState.GetTestSuiteDefinition();
            var userConfig = _testState.GetUserConfiguration(testSuiteDefinition.Persona);

            if (string.IsNullOrEmpty(userConfig.EmailKey))
            {
                return String.Empty;
            }

            return _environmentVariable.GetVariable(userConfig.EmailKey);
        }
    }


}
