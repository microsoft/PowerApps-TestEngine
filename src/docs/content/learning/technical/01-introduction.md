---
title: 01 - Introduction
---

As a business leader or low-code maker who supports the business, you need to ensure the reliability, performance, and security of your Power Platform solutions. This means you can't rely solely on manual testing processes, which can be time-consuming and prone to errors. You want to build a solution that quickly meets your business needs while ensuring high quality. The Power Apps Test Engine can help you achieve this without writing extensive code.

In this workshop, you’ll learn how to:

- Understand the importance of automated testing in low-code development, ensuring your solutions are reliable and secure.
- Utilize the Power Apps Test Engine to automate your testing processes, reducing manual effort and increasing efficiency.
- Leverage AI capabilities to enhance your testing strategies, providing intelligent insights and automating repetitive tasks.
- Record and run tests for your deployed applications, ensuring they perform as expected in real-world scenarios.
- Use Power Fx statements to create comprehensive test scenarios, validating the functionality and performance of your applications.
- Address known issues and limitations, ensuring your testing processes are robust and effective.

By the end of this workshop, you'll be equipped with the knowledge and tools to implement automated testing in your Power Platform solutions, helping you deliver high-quality applications quickly and efficiently.

## Interactive Examples

Where new concepts are introduced a interactive Power Fx window will be available to try the experience in your browser without installing any components or requiring access to a Power Platform Environment. Ideally this makes the process of learning and applying new concepts quick and interactive.

> NOTES:
> 1. If the value does not match the test will return "One or more errors occurred. (Exception has been thrown by the target of an invocation.)"
> 2. Reload the page to reset the sample to the default state

{{% powerfx-interactive %}}
Assert(1 = 1, "Unexpected value");
{{% /powerfx-interactive %}}

You can also try the [Learning Playground](/PowerApps-TestEngine/learning/playground?title=boolean-expressions)

## Pre-requisites

Before you get started depending on being an early adopter, beta tester or general availability user from [Ring Deployment Model](../context/ring-deployment-model.md) you will require different components installed to enable you to use this learning module.

### Inner Ring (Canary Users)

Follow the instructions on [Get Started Now](../context/get-started-now.md) ensure that you have the tools installs, verification checks and the source code cloned and compiled on your local machine.

> NOTE: Once the pac test run command is updated `pac test run` will be able to use instead of local compiled version of the Power Apps Test Engine. 

### Second Ring (Beta Testers)

Currently the features of Test Engine are not available for second ring (beta testers). The features required will soon be available using the Power Platform Command Line and enabling the Experimental namespace. 

> NOTE: Once the pac test run command is updated we will update setup notes here

### Outer Ring (General Availability)

Currently the features of Test Engine are not available for outer ring. 

> Check back here for announcements on when features used by this learning module are generally available as part of the Power Platform Command Line Interface. 

### Power Platform 

<a href="/PowerApps-TestEngine/learning/02-getting-setup" class="btn btn--primary">Get setup</a>