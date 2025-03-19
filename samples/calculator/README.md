# Overview

This Power Apps Test Engine sample demonstrates how to basic gallery of a canvas application

## Usage

2. Get the Environment Id and Tenant of the environment that the solution has been imported into

3. Create config.json file using tenant, environment and user1Email

```json
{
    "environmentId": "a0000000-1111-2222-3333-444455556666",
    "tenantId": "ccccdddd-1111-2222-3333-444455556666",
    "installPlaywright": false,
    "user1Email": "test@contoso.onmicosoft.com"
}
```

4. Execute the test

```pwsh
.\RunTests.ps1
```

## Different Variants of the Calculator Sample

The Calculator sample has two variants - one for `en-US` and another for locales that use commas `","` and periods `"."` differently. See below for usage -

1. The [testPlan.fx.yaml](testPlan.fx.yaml) sample supports the default number representation and is to be used with the [Calculator_1_0_0_2.zip](Calculator_1_0_0_2.zip) solution.
1. The [testPlanWithCommaForDecimal.fx.yaml](testPlanWithCommaForDecimal.fx.yaml) sample uses commas `","` for decimals and periods `"."` for thousand separator and supports locales such as some EU locales where this number format is used. This sample is to be used with the [Calculator2_1_0_0_2.zip](Calculator2_1_0_0_2.zip) solution to support this variation.
