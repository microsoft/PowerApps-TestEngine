# Samples Introduction

Document introducing the information about the controls and test engine features demonstrated for each sample. Most sample contains one zipped msapp and one YAML test plan. YAML test plan are all define in [YAML Format](https://github.com/microsoft/PowerApps-TestEngine/blob/main/docs/Yaml/README.md). In order to test Sample please follow [readme](https://github.com/microsoft/PowerApps-TestEngine).

## Type of Samples


| Sample Name | Features Demonstrated |
| -- | -- |
| Basic Gallery | Assert `Label` Text to be the first item("Lorem ipsum 1") in the gallery. Select on the `NextArrow` in first row on gallery. Assert `Label` Text to be second item("Lorem ipsum 2") to validate the label in the 2nd row of the gallery to verifies that you can interact with controls within a basic gallery. |
| Button Clicker |  Wait for the `label`to be certain number("0"). Select on the `Button`. Assert `label` to be number+1("1") to verifies that counter increments when the button is clicked.
| Calculator| A component for Calculater with two `label`s for number input, one `lable` for calculated result and four `Button` for Add, Subtract, Multiply and Divide. Assert two input label to certain number("100", "100"). Select on one of four `Button`(Add). Assert result `Label` to right value("200") to Verifies that the calculator app works.
| Connector | Use SetPoperty to set `TextInput` to a String("Atlanta"). Select on `Button`. Assert `Label` to a string ("You are seeing the mock response") to verifies that you can mock network requests.
| Container | Select on the `Button`. Assert `label` to be number+1("1") to verifies that you can interact with control in the container.
 |DifferentVariableTypes| Use Wait, SetProperty, and Assert function to test `TextInput`, `Rating`, `Toggle`, `DatePicker` control to make sure DateType like String, Number, Boolean, and Date works.
 |ManyScreens| Three Screens on the canvas app. First 'Home Screem' have two `Button` navigate to other two screen. Other two Screen 'Label Screen' and 'Gallery Screen' each have one `Button` navigate to the 'Home Screem'. Select on the 'Label Screen ' `Button`. Assert `label1` to be string on 'Label Screen'("Hello world!") to verifies that you can interact with controls on other screens.
 |Nested Gallery| Two Gallery and two label each with column and row. Column Gallery inside row Gallery.  `Select` 1nd row in the row gallery. `Assert` row `Label` to validate that the selected row is updated. `Select` 2nd column in the column gallery. `Assert` column `Label` to validate that the selected column is updated. 
 |PCF Component| Import PCF Component in the canvsas app. Use SetPoperty to set `IncrementControl1` to a number(10). Assert `IncrementControl1` to a number (10) to verifies that you can interact with increment control of the PCF Component.
 |Screen Navigation|Two Screens and labels on the canvas app. 'Screen1' have `Button2` navigate to second screen. 'Screen2' have `Button1` navigate to first screen. Select on the `Button2`. Assert `label2` to be the string("Screen2") to verifies that you can interact with controls for screen navigation.


