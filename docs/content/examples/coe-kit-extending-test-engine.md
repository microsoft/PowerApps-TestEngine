---
title: Extending the Test Engine to Support Testing of the CoE Starter Kit Setup and Upgrade Wizard
---

In this article, we will discuss how the CoE Starter Kit has made use of extensions to the test engine to support testing of Setup and Upgrade Wizard. This journey involves breaking down tests into smaller, more manageable steps, demonstrating the use of variables and collections, utilizing the Experimental namespace to overcome current limitations.

By following these guidelines and examples, you can effectively make use of the test engine to learn how to to apply similar approaches. This ensures thorough and reliable testing, ultimately leading to a more robust and user-friendly application.

## Example Test Cases

To start of the discussion lets look at a snippet of yaml and Power Fx testSteps used to test the application

```yaml
  testCases:
    - testCaseName: Step 1 - Confirm Pre-requisites
      testCaseDescription: Verify pre-requistes in place
      testSteps: |
        = 
        Experimental.ConsentDialog(Table({Text: "Center of Excellence Setup Wizard"}));
        Experimental.Pause();
        Set(configStep, 1); 
        Assert(configStep=1);
        Select(btnNext);
    - testCaseName: Step 2 - Configure communication methods
      testCaseDescription: Verify communication methods setup
      testSteps: |
        =
        Assert(configStep=2);
        Assert(CountRows(colCommunicate)=3);
        Experimental.SelectControl(Button3,1);
        Experimental.Pause(); 
```

## Test Case Design

Breaking tests into smaller steps is a crucial strategy for achieving better test isolation. By isolating test cases, it becomes easier to identify and fix issues, leading to more maintainable test scripts. This approach not only simplifies the testing process but also enhances the reliability of the tests. Let look at three concepts of isolation and look at sequential and parallel execution.

### Test Case Isolation

Test case isolation is a critical aspect of software testing that ensures each test case runs independently of others. This independence is essential for identifying and fixing issues efficiently, as it prevents one test case from interfering with another. By isolating test cases, we can achieve both sequential and parallel execution, which significantly enhances the reliability and speed of the tests.

### Sequential Test Execution
Sequential test execution involves running test cases one after the other in a specific order. This approach is beneficial when tests need to share a common state or when the outcome of one test case affects the next. 

For example, in the CoE Starter Kit Setup and Upgrade Wizard, the first test case, **"Step 1 - Confirm Pre-requisites"** verifies that the pre-requisites are in place. It involves steps such as displaying the consent dialog, pausing the test engine, setting the configStep variable to 1, and selecting the next button. 

The second test case, **"Step 2 - Configure communication methods"** builds on the state set by the first test case, verifying the setup of communication methods by asserting that the configStep variable is set to 2 and checking the colCommunicate collection. 

This sequential approach ensures that each step is executed in the correct order, maintaining the necessary state throughout the process.

### Parallel Test Execution

Parallel test execution, on the other hand, involves running test cases simultaneously. This approach is advantageous when test cases are independent and do not rely on the state or outcome of other tests. By executing tests in parallel, we can significantly reduce the overall time required for testing, which is particularly beneficial in large projects with extensive test suites. Parallel execution allows for faster feedback and quicker iterations, ultimately leading to a more efficient development process. To achieve parallel execution, each test case must set up its own environment, execute its steps, and clean up after itself, ensuring that tests can be run in any order without affecting the results.

## Variables

In the Setup and Upgrade wizard variables are an essential component of the application. The Power Fx in the application will lookup the current state of the install wizard process to restart the wizard at the last location. While this can help with a good user experience allowing the user to restart at the last step it can make automated testings more difficult as test cases 

Overall by being able to set variable values it allows allows testers to handle complex scenarios with ease, ensuring that the tests are both comprehensive and easy to understand.

## Collections

Collections play a crucial role in Power Apps, especially when it comes to querying the state of Dataverse. They allow you to store and manipulate data locally within your app, making it easier to manage and interact with data from various sources.

In the context of testing, collections are particularly useful for verifying that data has the expected values. Standard Power Fx functions like `CountRows()` and `Filter()` can be employed to perform these verifications effectively. For instance, `CountRows()` can be used to count the number of records in a collection, ensuring that the expected number of items is present. Similarly, `Filter()` can be used to query specific records based on certain criteria, allowing you to validate that the data meets the required conditions.

By leveraging collections and these Power Fx functions, you can create robust test cases that accurately reflect the state of your data. This not only enhances the reliability of your tests but also ensures that your application behaves as expected under various scenarios.

## Experimental Namespace

The Experimental Power Fx namespace is another vital aspect of this extension. It includes functions that are still under review and approval, providing testers with the opportunity to experiment with new features and offer valuable feedback for improvement. This experimental approach fosters innovation and helps in refining the testing process.

One example of this is the use of the `Experimental.SelectControl()` function. This function is particularly useful for selecting controls, such as Button3, that exist in galleries. This capability addresses a limitation in the current released version that did not allow `Select()` to be applied to repeating controls inside a gallery. The result enables more comprehensive testing scenarios and ensuring that all aspects of the application are thoroughly tested.

## Experimental.Pause()

The `Experimental.Pause()` function is a powerful tool in the testing process, allowing you to pause test execution and present the Playwright inspector. This feature is particularly useful for building and verifying tests, as it provides an opportunity to interact with the application in real-time.

When you invoke `Experimental.Pause()`, the test execution halts, and the Playwright inspector is displayed. This interactive environment enables you to examine the current state of the application, inspect elements, and perform actions manually. By doing so, you can verify that the test steps are executing as expected and make any necessary adjustments on the fly.

Using `Experimental.Pause()` is especially beneficial during the development of test cases. It allows you to step through the test script, observe the behavior of the application, and ensure that each step produces the desired outcome. This iterative process helps in identifying and resolving issues early, leading to more robust and reliable test scripts.

Moreover, the Playwright inspector provides a visual representation of the application, making it easier to understand the context of each test step. You can interact with the application elements, check their properties, and validate that the test assertions are correct. This hands-on approach enhances the accuracy of your tests and ensures that they cover all necessary scenarios.

In summary, Experimental.Pause() is an invaluable feature for building and verifying tests. By pausing test execution and presenting the Playwright inspector, it allows you to interact with the application, inspect elements, and ensure that your test scripts are accurate and reliable. This interactive process not only simplifies test development but also enhances the overall quality of your testing efforts.
