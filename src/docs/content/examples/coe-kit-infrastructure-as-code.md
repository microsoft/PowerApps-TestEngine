---
title: CoE Starter Kit -  Infrastructure As Code
---

The combination of Terraform and the CoE Starter Kit provides a powerful and flexible solution for managing and governing your Power Platform environments. By leveraging the power of infrastructure as code and the comprehensive tools provided by the CoE Starter Kit, you can ensure that your environments are always in a consistent and reliable state, enabling you to focus on delivering value to your users.

![Diagram that shows terraform and steps that will be automated as part of deployment](/PowerApps-TestEngine/examples/media/coe-kit-infrastructure-as-code.png)

## Terraform

In the ever-evolving landscape of technology, the need for efficient and repeatable processes has never been more critical. Enter [Terraform](https://www.terraform.io/), a powerful tool that has revolutionized the way we manage infrastructure. For those new to Terraform, it is an open-source infrastructure as code (IaC) software tool that allows you to define and provision data center infrastructure using a high-level configuration language. Terraform makes it easy to define and verify that created resources are consistent and repeatable, ensuring that your infrastructure is always in the desired state.

Using infrastructure as code is you to manage and provision infrastructure through code, which can be source-controlled and managed alongside your application code. This means that your infrastructure can be versioned, reviewed, and tested just like any other piece of software. With Terraform, you can define your infrastructure in a declarative configuration file, and Terraform will handle the rest, ensuring that your infrastructure is always in sync with your configuration.

One of the standout features of Terraform is its extensibility. There is a specific [Power Platform Terraform provider](https://registry.terraform.io/providers/microsoft/power-platform/latest/docs) that allows you to manage Power Platform resources using Terraform. This provider can be combined with other Terraform providers, enabling you to mix and match Power Platform resources with any other resources you want to deploy as part of your end-to-end process. This flexibility makes Terraform an invaluable tool for managing complex, multi-cloud environments.

## Setting Up a Test Tenant
One of the key tasks for the CoE Kit team is working with temporary tenants that exist only for a short period and are destroyed once testing is complete. A crucial element in this process for us has been automating the set up an empty tenant with Power Platform with test users and assigning licenses that enable expected "happy path" tests, edge cases, and exception cases. This setup is achieved using a Terraform bootstrap process.

### Getting started

It is highly recommended that you use the Dev Container to run the bootstrap script as the required tooling is pre-installed in the Dev Container.

Alternatively you can local install of tools to setup and run the scripts. Which you will need to install:
* [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/)
* [Terraform CLI](https://developer.hashicorp.com/terraform/cli)
* [Git Client](https://git-scm.com/downloads)
* [PowerShell](https://learn.microsoft.com/powershell/scripting/install/installing-powershell)
* [Power Platform CLI](https://learn.microsoft.com/power-platform/developer/cli/introduction?tabs=windows#install-microsoft-power-platform-cli)

Manual install steps

1. Clone the repository

```pwsh
https://github.com/Grant-Archibald-MS/power-platform-terraform-quickstarts
```

2. Open repository and development branch

```pwsh
cd power-platform-terraform-quickstarts
git checkout integration
```

3. Change to bootstrap folder

```pwsh
cd bootstrap
```

4. Ensure logged out of any current az cli session

```pwsh
az logout
```

5. Run the bootstrap script

```pwsh
pwsh .\bootstrap.ps1
```

6. Login to the Entra Portal

7. Open the created **Power Platform Admin Service** Application

8. Select **API Permissions**

9. Select **Grant Admin consent for" link

10. Login as the created application

```pwsh
az login --allow-no-subscriptions --scope api://power-platform_provider_terraform/.default
```

### Explain what is this Bootstrap Process?

In the context of setting up Power Platform Quick Start samples using Terraform, the bootstrap process refers to the initial set of operations that prepare and configure the necessary infrastructure and environment. This process is essential for ensuring that the environment is correctly set up and ready for further configuration and deployment.

### Why is the Bootstrap Process Important?

Initialization: The bootstrap process initializes the infrastructure required for the Power Platform environment. 

Configuration: During the bootstrap process, the environment is configured with the necessary settings and permissions. This ensures that all components are correctly set up and can communicate with each other.

Automation: By using Terraform for the bootstrap process, you can automate the setup and configuration of your environment. This reduces the risk of human error and ensures that the environment is set up consistently every time.

Security: The bootstrap process includes security configurations to ensure that the environment is secure and compliant with organizational policies. This helps protect the environment from unauthorized access and potential security threats.

### Understanding the Terraform Bootstrap Process

The Terraform bootstrap process is essential for provisioning the necessary infrastructure for your test tenant. This process involves several steps to ensure that the environment is correctly configured and ready for testing. The detailed steps for the [bootstrap process](https://github.com/microsoft/power-platform-terraform-quickstarts/blob/main/bootstrap/README.md) can be found in the Power Platform Terraform quick start. 

#### Understanding the Script

Lets have a look at the [PowerShell script](https://github.com/microsoft/power-platform-terraform-quickstarts/blob/main/bootstrap/bootstrap.ps1) that you can use to setup your tenant ready to apply terraform deployments of Power Platform resoures. This PowerShell script is designed to set up and configure a test tenant using Terraform and Azure CLI. It performs the following key tasks:

1. Check Terraform Installation: Verifies if Terraform is installed and available in the system's PATH. If not, it attempts to locate Terraform in the ProgramData directory.
2. Validate Input Parameters: Ensures that the required parameters (subscription_id and location) are provided.
3. Azure Login and Subscription Setup: Logs into Azure using device code authentication and sets the specified subscription.
3. Install Bicep: Installs the Bicep CLI, which is used for deploying Azure resources.
4. Deploy Terraform Backend Resources: Deploys the necessary backend resources for Terraform using a Bicep template and writes the configuration to a file.
5. Initialize and Apply Terraform Configuration: Initializes and applies the Terraform configuration to set up the test tenant.
6. Set Environment Variables: Extracts and sets environment variables for the Power Platform client ID, secret, and tenant ID.
5. Grant Permissions: Instructs the user to grant permissions to the new 'Power Platform Admin Service' service principal in the Azure portal.

##### Assumptions

1. The user has [Azure CLI installed](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) and configured.
2. The user has appropriate permissions to create and manage resources in the specified Azure subscription.
3. The user has basic knowledge of PowerShell and Terraform.

##### Entra Permissions Required

To execute the script you will need at least [Application Developer role](https://learn.microsoft.com/entra/identity/role-based-access-control/permissions-reference#application-developer) to create the Entra Application. This role allows users to register and manage applications in Azure AD, including granting consent for delegated permissions. The Application Developer role includes the following permissions:

- Create application registrations: Allows the user to register new applications in Azure AD.
- Manage application registrations: Allows the user to manage existing application registrations, including updating application settings and permissions.

> **NOTE**: 
> 1. The Application Developer role in Entra allows users to register and manage applications, but it does not have the rights to grant tenant-wide delegated permissions. Specifically, for granting tenant-level delegated permissions required, the Application Developer role alone is insufficient.
>
> 2. To grant tenant-level delegated permissions, such as user_impersonation for Dynamics CRM, you need to have higher administrative privileges. This typically requires the Global Administrator or a custom directory role in Entra.

#### Granting App Permissions

Once the application is created, a user with the required permissions will need to consent to the following permissions for the created application. You can read more on [Grant tenant-wide admin consent to an application](https://learn.microsoft.com/entra/identity/enterprise-apps/grant-admin-consent?pivots=portal) to understand the required permissions and process that consent can be provided.


| Permission | Description | Why |
|------------|-------------|-----|
| **Dynamics CRM** | | |
| `user_impersonation` | Access Dataverse as organization users | This permission allows the application to access Dataverse on behalf of the signed-in application.|
| **Power Platform API** | | |
| `AppManagement.ApplicationPackages.Install` | Install Application Packages | This permission allows the application to install application packages. It is necessary for applications that need to manage and deploy application packages within the Power Platform. |
| `AppManagement.ApplicationPackages.Read` | Read Application Packages | This permission allows the application to read application packages. It is required for applications that need to access information about installed application packages. |
| `Licensing.BillingPolicies.Read` | Read Billing Policies | This permission allows the application to read billing policies. It is important for applications that need to access billing information and policies. |
| `Licensing.BillingPolicies.ReadWrite` | Read and Write Billing Policies | This permission allows the application to read and write billing policies.  |
| **PowerApps Service** | | |
| `User` | Access the PowerApps Service API | This permission allows the application to access the PowerApps Service API. It is necessary for applications that need to interact with the PowerApps Service. |

## Setting up a new Test Tenant

As an optional step if you have created an empty test tenant and have completed the bootstrap process, you can follow the template provided in [Demo Tenant Sample](https://github.com/Grant-Archibald-MS/power-platform-terraform-quickstarts/blob/grant-archibald-ms/coe-kit-connections/quickstarts/103-demo-tenant). 

This template provide optional steps to do the following:
1. Create a set of sample test users with random passwords.
2. Create a Maker security group
3. Assign licenses for Power Apps Developer, Power Automate Premium and Microsoft 365 Business Premium to the Maker Security Group
4. Create a developer environment for each test user

> [NOTES]:
> 1. For developer environments [Power Apps Developer Plan](https://www.microsoft.com/en-us/power-platform/products/power-apps/free) could be applied
> 2. Microsoft 365 Business Premium licenses has been purchased from Microsoft 365 Admin portal market place https://admin.microsoft.com/Adminportal/Home#/catalog. The [Try or buy a Microsoft 365 for business subscription](https://learn.microsoft.com/microsoft-365/commerce/try-or-buy-microsoft-365) can help you with choices.
> 3. Power Automate Premium licenses has been purchased from Microsoft 365 Admin portal market place. The [Types of Power Automate licenses](https://learn.microsoft.com/power-platform/admin/power-automate-licensing/types) can help you select license choices.

### Post Bootstrap Steps

Follow the following post bootstrap steps to create 

1. Review the [103-demo-tenant](https://github.com/Grant-Archibald-MS/power-platform-terraform-quickstarts/blob/grant-archibald-ms/coe-kit-connections/quickstarts/103-demo-tenant)

2. Remember to clear your pac cli authentication and login to your tenant

```pwsh
pac auth clear
pac auth create --tenant 01234567-1111-2222-3333-44445555666
```

3. Install Terraform components

```pwsh
terraform init
```

4. See what is planned for you deployment

```pwsh
terraform plan -var-file=sample.tfvars.txt
```

5. Apply test users after reviewing plans

```pwsh
terraform apply -var-file=sample.tfvars.txt
```

## CoE Starter Kit Sample

Collaborating with the Power Platform quick start team the Power CAT Engineering team has collaborated to build a [CoE Starter Kit Quick start Terraform sample](https://github.com/microsoft/power-platform-terraform-quickstarts/tree/mawasile/coe-kit-iac-quickstart/quickstarts/202-coe-starter-kit). The sample demonstrates how to create an environment within a region, create connections required by the CoE Starter Kit as the user persona, import Power Platform dependencies of the Creator Kit, import a version of the CoE Starter Kit, and validate that the setup and upgrade process works as expected by running defined test cases. This ensures that your Power Platform environments are always in a consistent and reliable state.

## Automation Test Roadmap

Setting up and integrating a Terraform deployment model for the CoE Starter Kit is a crucial step in automating and simplifying the setup of environments with different settings. This approach allows you to move towards a matrix deployment model with multiple versions, enabling you to upgrade and validate functionality across multiple geographical regions. By automating these processes, you can ensure that your environments are always up-to-date and functioning correctly, reducing the risk of errors and improving overall efficiency.
