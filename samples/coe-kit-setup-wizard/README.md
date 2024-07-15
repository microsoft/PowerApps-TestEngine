# Overview

The Power Platform Center of Excellence (CoE) starter kit is made up of a number of Power Platform low code solution elements. Amoung these is a model driven application that can be used to setup and upgrade the CoE Starter Kit.

This sample includes Power Apps Test Engine tests that can be used to automate and test ket elemenets of the expected behaviour of the Setup and Upgrade Wizard

## Getting Started

To get started ensure that you have followed the [Build locally](../../README.md) to have a working version of the Power Apps Test Engine available

## Usage

You can execute this sample using the following commands

```bash
cd bin/Debug/PowerAppsTestEngine
dotnet PowerAppsTestEngine.dll -i ../../../samples/coe-kit-setup-wizard/testPlan.fx.yaml -u browser -p mda -d https://contoso.crm.dynamics.com/main.aspx?appid=06f88e88-163e-ef11-840a-0022481fcc8d&pagetype=custom&name=admin_initialsetuppage_d45cf
```
