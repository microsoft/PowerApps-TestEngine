---
title: Keeping up to date
---

Staying current with the latest features and updates in the Power Apps Test Engine allows you to leverage new capabilities and ensuring optimal performance. Here's how you can keep up to date based on the version of test engine you are using.

## Key Concept: Ring Deployment Model

The [Ring Deployment Model](./ring-deployment-model.md) is a phased approach to rolling out new features and updates. 

### Keeping Up to Date

#### Inner Ring (Canary Users)

As an Inner Ring user, you are among the first to test new features. Keeping up to date involves using source control to fetch the latest changes. Here’s how you can do it:

1. Open the cloned repository

2. Fetch the Latest Changes: Use git pull to get the latest updates from the repository.

```pwsh
git pull
```

3. Checkout the Desired Branch: Switch to the branch you need, such as integration or another feature branch, using git checkout integration. Example command checkout the integration branch the :

```pwsh
git checkout integration
```

#### Second Ring (Beta Testers)

Beta testers use the latest version of the Power Platform Command Line Interface (PAC CLI) to stay updated. Here’s how you can ensure you have the latest version:

1. Use the following command to update to the latest version.

```pwsh
pac install latest
```

2. Additionally, to use experimental features, include the following YAML configuration in your test settings:

```yaml
testSettings:
  allowPowerFxNamespaces:
    - Experimental
```

#### Outer Ring (General Availability)

For users in the Outer Ring, updates are automatically rolled out once they have been thoroughly tested and refined. Simply ensure your environment is set to receive updates, and you will get the latest stable features as they become available.
