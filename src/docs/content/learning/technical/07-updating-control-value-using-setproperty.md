---
title: 07 - Updating Control Value using SetProperty
---

## Introduction to SetProperty

The `SetProperty` function in Power Fx is used to update the value of a control in your Power Apps application. This function allows you to change the properties of controls, such as text, color, visibility, and more.

### Syntax

```powerfx
SetProperty(control.property, value)
```

- control.property: The property of the control you want to update.
- value: The new value you want to assign to the property.

## Example: Updating Label Text

Let's consider a basic example that combines [Asserting Results](./06-asserting-results.md) where we want to update the text of a label control (Label1) to "End of the test" and make sure changes are applied:

{% powerfx %}
// Setup simulated Label Control with Text Value to check below
SetProperty(Label1.Text, "Start of test");
Assert(Label1.Text = "Start of test", "Unexpected start value");

SetProperty(Label1.Text, "End of the test");
Assert(Label1.Text = "End of the test", "Unexpected end value");
{% endpowerfx %}

Want to explore more concepts examples checkout the <a href="/powerfuldev-testing/learning/playground?title=assert-multiple-values" class="btn btn--primary">Learning Playground</a> to explore related testing concepts

## Using SetProperty in a Test Plan

You can use the SetProperty function in your test plans to modify control values and verify the behavior of your application. Let's look at an example where we modify the value so that the test fails.

## Example from Button Clicker Sample

In the Button Clicker sample, we can add a step to update the text of Label1 and then assert that the text has been updated correctly.

Steps to Use SetProperty in Your Test Plan

1. Navigate to the \samples\buttonclicker\ directory.

2. Open the testPlan.fx.yaml file in a text editor.
Add a SetProperty Statement:

3. Add a SetProperty statement to update the value of Label1.Text. For example:

    ```powerfx
    SetProperty(Label1.Text, "End of the test")
    ```

4. Add an Assert Statement:

Add an assert statement to check if the value has been updated correctly. For example:

    ```powerfx
    Assert(Label1.Text = "End of the test", "Label1 should display 'End of the test'")
    ```

5. Modify the Value to Make the Test Fail:

To demonstrate a failing test, modify the assert statement to check for an incorrect value. For example:

    ```powerfx
    Assert(Label1.Text = "Incorrect Value", "Label1 should display 'Incorrect Value'")
    ```

6. Run the test

    ```pwsh
    cd samples\buttonclicker
    pwsh -File RunTests.ps1
    ```

## Summary

In this section, you learned how to use the `SetProperty` function in Power Fx to update control values in your Power Apps application. By including SetProperty statements in your test plans, you can modify control properties and verify the behavior of your application. This process involves adding SetProperty and assert statements to your test plan, running the test script, and reviewing the results to confirm that the control values are updated as expected.

<a href="/powerfuldev-testing/learning/08-simulating-connector" class="btn btn--primary">Simulating connector</a>
