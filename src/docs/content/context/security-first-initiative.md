---
title: Secure First Initiative
---

## Overview
The [Secure First Initiative (SFI)](https://www.microsoft.com/trust-center/security/secure-future-initiative) is a comprehensive approach to ensuring that security is embedded in every aspect of our solutions. This initiative is built on three core principles: **Secure By Design**, **Secure By Default**, and **Secure in Operations**. By integrating these principles, we aim to create robust, resilient, and secure applications that can withstand evolving cyber threats.

## Security Principles
In today's digital landscape, security is paramount. The Secure First Initiative (SFI) emphasizes a proactive approach to security, ensuring that every layer of our solutions is fortified against potential threats. This involves a culture of continuous improvement, robust governance, and adherence to best practices across all stages of development and operations. Lets look at the key principles.

### Secure By Design
Security is a fundamental aspect of our design process. We build security-first tests to ensure that our solutions are inherently secure from the outset. Looking specifically at the power platform through the lens of the Power Apps Test Engine this includes:

- **Security concepts in Microsoft Dataverse**: One of the key features of Dataverse is its rich [security model](https://learn.microsoft.com/power-platform/admin/wp-security-cds) that can adapt to many business usage scenarios and testing this behaves as expected. 
- **Application Sharing**: Testing how the solution reacts when an application is not shared.
- **Role Assignments**: Verifying the solution's behavior when required Power Platform Dataverse roles are not assigned.
- **Data Validation**: Ensuring that data input and output are validated as part of tests to validate behavior.

### Secure By Default

Our solutions come with security protections enabled and enforced by default. This means:

- **Default Security Settings**: Ensuring that security settings are enabled by default, requiring no additional configuration.
- **Automated Testing**: Using the Power Apps Test Engine to create and verify tests as part of the build and release process.
- **Least Privilege Principle**: Configuring default roles and permissions to follow the principle of least privilege, and test that minimizing access rights for users to the bare minimum necessary.

### Secure in Operations

Continuous security validation and verification are crucial to maintaining a secure operational environment. This involves:

- **Continuous Integration and Deployment (CI/CD)**: Implementing CI/CD pipelines to continuously validate and verify security configurations.
- **Simulation and Testing**: Allowing for the simulation of Dataverse and connectors to see how solution components react in edge or exception cases.
- **Multi-Factor Authentication (MFA) and Conditional Access**: Ensuring that solutions work seamlessly with MFA and Conditional Access policies. Have a look at [Authentication](../discussion/authentication.md) for further information.

## Role of Generative AI

[Generative AI](../discussion/generative-ai.md) with in are of Automated Testing plays a significant role in enhancing our security processes. It helps in:

- **Building Tests**: Assisting in the creation of comprehensive security tests.
- **Secure Solutions**: Helping to build secure solutions by default through intelligent recommendations and automation.

## Application Lifecycle Management (ALM)

Effective ALM processes are essential for maintaining security throughout the development lifecycle. This includes:

- **Transient Environments**: Using infrastructure as code to build on-demand environments that exist only for a limited time using [Infrastructure As Code](../examples/coe-kit-infrastructure-as-code.md), reducing the attack surface area and creating a verifiable base line to compare differences.
- **Source Control Integration**: Protecting the software supply chain with source control integration and scanning of release pipeline assets using tools like CodeQL and SonarQube.

## Security Context

### Microsoft's Security-First Culture

Culture is reinforced through daily behaviors. We have regular meetings between engineering executive vice presidents, SFI leaders, and all management levels ensure bottom-up, end-to-end problem-solving that ingrains security thinking into our everyday actions.

### Security Governance
We're elevating security governance with a new framework led by the chief information security officer. This will introduce a partnership with engineering teams to oversee SFI, manage risks, and report progress to leadership.

### Continuous Security Improvement
SFI empowers every employee at Microsoft to prioritize security, driven by a growth mindset of continuous improvement. We integrate feedback and learnings from incidents into our standards, enabling secure design and operations at scale.

### Paved Paths and Standards
Paved paths are best practices that optimize productivity, compliance, and security. These become standards when they enhance security or the developer experience. With SFI, we set and measure standards across all six prioritized security pillars.

### Pillars

#### Protect Identities and Secrets
Reduce the risk of unauthorized access by implementing and enforcing best-in-class standards across all identity and secrets infrastructure, plus user and application authentication and authorization.

#### Protect Tenants and Isolate Systems
Protect all Microsoft tenants and production environments using consistent, best-in-class security practices and strict isolation to minimize breadth of impact.

#### Protect Networks
Protect Microsoft production networks and implement network isolation of Microsoft and customer resources.

#### Protect Engineering Systems
Protect software assets and continuously improve code security through governance of the software supply chain and engineering systems infrastructure.

#### Monitor and Detect Cyberthreats
Provide comprehensive coverage and automatic detection of cyberthreats for Microsoft production infrastructure and services.

#### Accelerate Response and Remediation
Prevent exploitation of vulnerabilities discovered by external and internal entities through comprehensive and timely remediation.

## Conclusion
The Secure First Initiative is our commitment to delivering secure, resilient, and trustworthy solutions. By adhering to the principles of Secure By Design, Secure By Default, and Secure in Operations, we ensure that our applications are prepared to meet the highest security standards. Automated Testing can be one component of this overall strategy to protect your organization.
