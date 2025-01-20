---
title: Storage State Deep Dive
---

One of the key elements of automated discussion using the multiple profiles of automated testing of Power apps is the security model to allow login and the security around these credentials. Understanding this deep dive is critical to comprehend how the login credential process works, how login tokens are encrypted, and how this relates to the Multi-Factor Authentication (MFA) process. Additionally, we will explore the controls that the Entra security team can put in in place and the security model across Test Engine, Playwright, Data Protection API, OAuth Login to Dataverse, Dataverse, and Key Value Store.

> This section is intended for discussion and review purposes. Each organization should thoroughly review and determine how this information fits into their specific security and compliance processes. The details provided here are meant to offer a comprehensive understanding of the security model for automated testing of Power apps, including login credentials, token encryption, and Multi-Factor Authentication (MFA). It is crucial for each organization to assess and adapt these insights to align with their unique security requirements and protocols.

## Security Context

This discussion fits into wider [Secure First Initiative](./security-first-initiative.md) landscape which is a comprehensive approach to ensuring that security is embedded in every aspect of our solutions. This initiative is built on three core principles: Secure By Design, Secure By Default, and Secure in Operations. By integrating these principles, we aim to create robust, resilient, and secure applications that can withstand evolving cyber threats.

### Security Principles

In today's digital landscape, security is paramount. The Secure First Initiative (SFI) emphasizes a proactive approach to security, ensuring that every layer of our solutions is fortified against potential threats. This involves a culture of continuous improvement, robust governance, and adherence to best practices across all stages of development and operations. Let's look at the key principles.

#### Secure By Design

Security is a fundamental aspect of our design process. We build security-first tests to ensure that our solutions are inherently secure from the outset. Looking specifically at the power platform through the lens of the Power Apps Test Engine, this includes:

- Building on Security concepts in Microsoft Dataverse
- User Profiles: Allow support for multiple profiles to verify how the solution reacts when an application is not shared.

#### Secure By Default

The aim of solutions come with security protections enabled and enforced by default. This means:

- Default Security Settings: Ensuring that security settings are enabled by default, requiring no additional configuration.
- Automated Testing: Using the Power Apps Test Engine to create and verify tests as part of the build and release process.
- Least Privilege Principle: Allowing default roles and permissions to follow the principle of least privilege, and testing that minimizes access rights for users to the bare minimum necessary.

#### Secure in Operations

Continuous security validation and verification are crucial to maintaining a secure operational environment. This involves:

- Continuous Integration and Deployment (CI/CD): Ability to create an share test profiles inside CI/CD pipelines to continuously validate and verify security configurations.
- Multi-Factor Authentication (MFA) and Conditional Access: Ensuring that solutions work seamlessly with MFA and Conditional Access policies. 

## End to End Process

With that context in place lets explore the possible end to end process at a high level. 

{% include feature_row type=center %}

### Local Windows

The first and simplest option to secure storage state is using Windows Data Protection API. While this option has less components it does have limitations as it requires Microsoft Windows together the user account and machine to secure the files locally. If you are looking to execute on a different operating system or execute your tests in the context of pipelines the [Dataverse Data Protection](#data-protection-dataverse) approach will be a better approach.

![Diagram of end to end process of Browser storage state using local storage and Windows Data Protection API](/PowerApps-TestEngine/context/media/test-engine-security-storage-state-local.png)

1. The current storage state user authentication provider is extended to Save and load the Playwright browser context as Encrypted values
2. Make use of the [Windows Data Projection API](https://learn.microsoft.com/dotnet/standard/security/how-to-use-data-protection) to protect and unprotect the saved state at rest.
3. Save encrypted json file or retrieve un protected JSON to the file system for use in other test sessions

### Data Protection Dataverse

The next approach makes use of a more comprehensive data protection approach that allows saved user persona state to be shared across multiple machines. This approach adopts a multi level approach to allow for a wider set of operating systems and execution environments.

![Diagram of end to end process of Browser storage state using Microsoft Data Protection and Dataverse Data store for secure values](/PowerApps-TestEngine/context/media/test-engine-security-storage-state.png)

1. The current storage state user authentication provider is extended to Save and load the Playwright browser context as Encrypted values
2. Make use of the Microsoft.AspNetCore.DataProtection package to offer Windows Data Protection (DAPI) or Certificate public / private encryption
3. Use the current logged in Azure CLI session to obtain an access token to the Dataverse instance where Test Engine Key values and encrypted key data is stored
4. Use a custom xml repository that provides the ability query and create Data Protection state by implementing IXmlRepository
5. Store XML state of data protection in Dataverse Table. Encryption of XML State managed by Data Protection API and selected protection providers
6. Make use of Dataverse Security model, sharing and auditing features are enabled to control access and record access to key and key data. Data Protection API is used to decrypt values and apply the state json to other test sessions.
7. Use the Data Protection API to decrypt the encrypted value using Windows Data Protection API (DAPI) or X.509 certificate private key.
8. A future option could also consider adding integration with [Azure Key Vault](https://learn.microsoft.com/aspnet/core/security/key-vault-configuration?view=aspnetcore-9.0)

## Tell Me More

Lets explore each of these layers in more detail.

### Playwright

Test Engine web based tests encapsulate browser automation using [Playwright](https://playwright.dev/dotnet/). The key class in this process is [BrowserContext](https://playwright.dev/dotnet/docs/api/class-browsercontext#browser-context-storage-state) that allows provide a way to operate multiple independent browser sessions. Specifically the key element is the use of storage state for this browser context, contains current cookies and local storage snapshot to allow interactive and headless login.

### Initial Login

Using the storage state provider of Test Engine the initial login must be part of an interactive login process. This process will attempt to start a test session. When no previous storage state is available or valid the interactive user will be prompted to be logged in via Entra before allowing access to the requested page. This login process will use what ever Multi Factor and Conditional Access policies that have been applied to validate the user login. 

Including some further on possible settings for background reading:

1. Configured [Authentication Methods](https://learn.microsoft.com/entra/identity/authentication/concept-authentication-methods)
2. [How it works: Microsoft Entra multifactor authentication](https://learn.microsoft.com/entra/identity/authentication/concept-mfa-howitworks)
3. [Reauthentication prompts and session lifetime for Microsoft Entra multifactor authentication](https://learn.microsoft.com/entra/identity/authentication/concepts-azure-multi-factor-authentication-prompts-session-lifetime)
4. [Web browser cookies used in Microsoft Entra authentication](https://learn.microsoft.com/entra/identity/authentication/concept-authentication-web-browser-cookies)
5. [What is Conditional Access?](https://learn.microsoft.com/entra/identity/conditional-access/overview)
6. [Configure adaptive session lifetime policies](https://learn.microsoft.com/entra/identity/conditional-access/howto-conditional-access-session-lifetime)
7. [Require a compliant device, Microsoft Entra hybrid joined device, or multifactor authentication for all users](https://learn.microsoft.com/entra/identity/conditional-access/policy-alt-all-users-compliant-hybrid-or-mfa)
8. [Microsoft Defender for Cloud Apps overview](https://learn.microsoft.com/defender-cloud-apps/what-is-defender-for-cloud-apps)

The goal of the login process it work with organization defined login process so that defined authentication methods are validated.

### Storage State

After a successful login process The storage state of the browser context can contain [cookies](https://learn.microsoft.com/entra/identity/authentication/concept-authentication-web-browser-cookies) that are used to authenticate later sessions. 

The [How to: Use Data Protection](https://learn.microsoft.com/dotnet/standard/security/how-to-use-data-protection) provides more information and details on this approach.

### Windows Data Protection API

.NET provides access to the data protection API (DPAPI), which allows you to encrypt data using information from the current user account or computer. When you use the DPAPI, you alleviate the difficult problem of explicitly generating and storing a cryptographic key.

### Data Protection API

The [Microsoft.AspNetCore.DataProtection](https://learn.microsoft.com/aspnet/core/security/data-protection/introduction?view=aspnetcore-9.0) package offers Windows Data Protection (DAPI) or Certificate public/private encryption. This ensures that sensitive data, such as login tokens, is securely encrypted and stored.

Key information to review for readers that are unfamiliar with Data Protection API:

- [Authenticated encryption details in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/authenticated-encryption-details?view=aspnetcore-9.0) with ES-256-CBC + HMACSHA256
- [Key management in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-9.0)
- [Custom key repository](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-9.0&tabs=visual-studio#custom-key-repository)
- [Windows DPAPI key encryption at rest](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-encryption-at-rest?view=aspnetcore-9.0#windows-dpapi) encryption mechanism for data that's never read outside of the current machine. Only applies to Windows deployments.
- [X.509 certificate key encryption at rest](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-encryption-at-rest?view=aspnetcore-9.0#x509-certificate)

#### Encryption 

By default data values are encrypted with ES-256-CBC and HMACSHA256. 

ES-256-CBC stands for "AES-256 in Cipher Block Chaining (CBC) mode." AES (Advanced Encryption Standard) is a widely used encryption algorithm that ensures data confidentiality. The "256" refers to the key size, which is 256 bits. CBC mode is a method of encrypting data in blocks, where each block of plaintext is XORed with the previous ciphertext block before being encrypted. This ensures that identical plaintext blocks will produce different ciphertext blocks, enhancing security.

HMACSHA256 stands for "Hash-based Message Authentication Code using SHA-256." HMAC is a mechanism that provides data integrity and authenticity by combining a cryptographic hash function (in this case, SHA-256) with a secret key. SHA-256 is a member of the SHA-2 family of cryptographic hash functions, producing a 256-bit hash value.

When combined, ES-256-CBC + HMACSHA256 provides both encryption and authentication:

- Encryption with AES-256-CBC: The data is encrypted using AES-256 in CBC mode, ensuring that the data is confidential and cannot be read by unauthorized parties.
- Authentication with HMACSHA256: An HMAC is generated using SHA-256 and a secret key. This HMAC is appended to the encrypted data. When the data is received, the HMAC can be recalculated and compared to the appended HMAC to verify that the data has not been tampered with.

This combination ensures that the data is both encrypted (confidential) and authenticated (integrity and authenticity), providing a robust security mechanism.

### Dataverse Integration

The current logged-in Azure CLI session is used to obtain an access token to the Dataverse instance where Test Engine key values and encrypted key data are stored. The Dataverse security model, sharing, and auditing features are enabled to control access and record access to key and key data.

Further reading for readers unfamiliar with Azure CLI Login, Access Token and Dataverse security model:
1. [az login](https://learn.microsoft.com/cli/azure/reference-index?view=azure-cli-latest#az-login)
2. [az account get-access-token](https://learn.microsoft.com/cli/azure/account?view=azure-cli-latest#az-account-get-access-token) using Azure CLI to obtain access token. In this case it is used to obtain access token to integrate with Dataverse custom XML repository
3. [Microsoft.PowerPlatform.Dataverse.Client](https://www.nuget.org/packages/Microsoft.PowerPlatform.Dataverse.Client/) used to access Custom XML Repository using the obtained access token
4. [Security concepts in Microsoft Dataverse](https://learn.microsoft.com/power-platform/admin/wp-security-cds) where using User or Team owned records.
5. [Granting permission to tables in Dataverse for Microsoft Teams](https://learn.microsoft.com/power-apps/teams/dataverse-for-teams-table-permissions)
6. [Record sharing](https://learn.microsoft.com/power-platform/admin/wp-security-cds#record-sharing) Individual records can be shared on a one-by-one basis with another user (interactive or application user).
7. [Column-level security in Dataverse](https://learn.microsoft.com/power-platform/admin/wp-security-cds#column-level-security-in-dataverse)
8. [System and application users](https://learn.microsoft.com/power-platform/admin/system-application-users). Specifically [application users](https://learn.microsoft.com/power-platform/admin/create-users#create-an-application-user) that could be used from the context of a CI/CD process.

### Custom XML Repository

A custom XML repository provides the ability to query and create Data Protection state by implementing [IXmlRepository](https://learn.microsoft.com/aspnet/core/security/data-protection/extensibility/key-management?view=aspnetcore-8.0#ixmlrepository). The XML state of data protection is stored in a Dataverse table, with encryption managed by the Data Protection API and selected protection providers.

Note the IXmlRepository don't need to parse the XML passing through them. They treat the XML documents as opaque and let higher layers of the Data Protection API worry about generating and parsing the documents.

### Decryption Process

The Data Protection API is used to decrypt the encrypted value using Windows Data Protection API (DAPI) or X509 certificate private key. This decrypted value is then applied to other test sessions.

## Sample

The following sample is a conceptual overview of how values are Protected and Unprotected using Dataverse as the IXmlRepository

```csharp
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

public class Program { 
    public static void Main(string[] args)
    {
        ServiceProvider services = null;

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(configure => configure.AddConsole());

        var api = new Uri("https://contoso.crm.dynamics.com/");

        string keyName = "Sample";

        // Configure Dataverse connection
        var serviceClient = new ServiceClient(api, (url) => Task.FromResult(AzureCliHelper.GetAccessToken(api)));

        serviceCollection.AddSingleton<IOrganizationService>(serviceClient);

        serviceCollection.AddDataProtection()
            .ProtectKeysWithCertificate(GetCertificateFromStore("localhost"))
            //.ProtectKeysWithDpapi()
            .AddKeyManagementOptions(options =>
            {
                options.XmlRepository = new DataverseKeyStore(services?.GetRequiredService<ILogger<Program>>(), serviceClient, keyName);
            });
        services = serviceCollection.BuildServiceProvider();
        var protector = services.GetDataProtector("ASP Data Protection");


        string valueName = string.Empty;
        while ( string.IsNullOrEmpty(valueName))
        {
            Console.WriteLine("Variable Name");
            valueName = Console.ReadLine();
        }
        
        var matches = FindMatch(serviceClient, keyName, valueName);

        if (matches.Count() == 0)
        {
            string newValue = string.Empty;
            while ( string.IsNullOrEmpty(newValue))
            {
                Console.WriteLine("Value does not exist. What would you like the value to be?");
                newValue = Console.ReadLine();
            }
            
            StoreValue(serviceClient, keyName, valueName, protector.Protect(newValue));

            Console.WriteLine($"Saved value for {valueName}");
        } 
        else
        {
            string data = protector.Unprotect(matches.First().Data);
            Console.WriteLine($"Value {valueName}: {data}");
        }

        Console.ReadLine();
    }
}
```

### DataverseKeyStore

Possible code to query and store encryption key values encrypted via DAPI or public key of certificate. This layer just passes the opaque xml to other components interact with.

```csharp
public class DataverseKeyStore : IXmlRepository
{
    private readonly ILogger<Program>? _logger;
    private readonly IOrganizationService _service;
    private string _friendlyName;

    public DataverseKeyStore(ILogger<Program>? logger, IOrganizationService organizationService, string friendlyName)
    {
        _logger = logger;
        _service = organizationService;
        _friendlyName = friendlyName;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        // Retrieve keys from Dataverse
        var query = new QueryExpression("te_key")
        {
            ColumnSet = new ColumnSet("te_xml"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("te_name", ConditionOperator.Equal, _friendlyName)
                }
            }
        };

        var keys = _service.RetrieveMultiple(query)
        .Entities
        .Select(e => XElement.Parse(e.GetAttributeValue<string>("te_xml")))
        .ToList();

        return keys.AsReadOnly();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        var keyEntity = new Entity("te_key")
        {
            ["te_name"] = _friendlyName,
            ["te_xml"] = element.ToString(SaveOptions.DisableFormatting)
        };

        _service.Create(keyEntity);
    }
}
```

### Saving and Locating Encrypted Data

The following gives a simple view of possible code to store and retreive encrypted values from dataverse

```csharp
public static IReadOnlyCollection<ProtectedKeyValue> FindMatch(IOrganizationService service, string keyName, string? valueName)
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
    .Select(e => new ProtectedKeyValue {
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
    var keyEntity = new Entity("te_keydata")
    {
        ["te_keyname"] = keyName,
        ["te_valuename"] =  valueName,
        ["te_data"] = data,
    };

    service.Create(keyEntity);
}
```