# Power Apps Test Engine

Welcome to the Power Apps Test Engine documentation. This repository contains all the necessary information to get started with the Power Apps Test Engine, a comprehensive and automated testing framework for Power Apps and beyond.

## Overview

The Power Apps Test Engine is a robust and modular framework designed to facilitate comprehensive testing of various components within the Power Platform. It consists of multiple layers, each with specific responsibilities, including the Test Engine, Test Definition, Test Results, and Browser. The engine also features an extensibility model for User Authentication, Providers, and Power Fx.

## Build from Source vs Power Platform Command Line

Currently the Power Platform Engineering team is working in branches of the open source GitHub repository to expand the set of features. To use this code it makes use of a “build from source” strategy where the .Net SDK needs to be installed to compile the modules that make up the console application to  run Test Engine tests.

This open-source version is licensed under a MIT license and any issues are files are GitHub Issues with out any official Microsoft Support program.

To make a contribution to the Test Engine a Contributor license agreement must be made. Pull requests can be made to test engine which are reviewed and if accepted merged into the Test Engine code base. 

Once the pac test run feature reach general availability pac test run is the Microsoft Supported method to run Test Engine test. Issue with running Power Platform tests using the pac test run command would have the standard Microsoft support channels.

## Getting Started

The open source repository provides [instructions](../README.md) on how to compile and run tests using the source code provided.

## Branching Strategy

The respository currently includes the following branching strategy

- [main](https://github.com/microsoft/PowerApps-TestEngine/tree/main) - The current code that includes changes that will be included in the Power Platform Commands Line features using ```pac test run``` on a tag.
- Feature branches - Work in progress branches that include ongoing development and changes prior to being merged.

## Further Reading

Other documentation that you can read to understand how the Power Apps Test Engine works and can be used in

- [Guidance](./Guidance/README.md)
- [Architecture](./Architecture.md)
- [Yaml Format](./Yaml/README.md)
- [Power FX Functions](./PowerFX/README.md)
