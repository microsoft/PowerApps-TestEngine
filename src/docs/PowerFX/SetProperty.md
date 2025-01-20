# SetProperty

`SetProperty(control.propertyName, propertyValue)`

This is the same functionality as the Power Apps [SetProperty function](https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-setproperty).

When working with a nested gallery, use [Index()](https://learn.microsoft.com/en-us/power-platform/power-fx/reference/function-first-last) within the SetProperty function.

## Example

`SetProperty(TextInput.Text, "Say Something")`

`SetProperty(Dropdown1.Selected, {Value:"2"})`

`SetProperty(ComboBox1.SelectedItems, Table({Value:"1"},{Value:"2"}))`

`SetProperty(Index(Gallery1.AllItems, 1).TextInput1.Text, "Change the text input")`

`Select(Index(Index(Gallery1.AllItems, 1).Gallery2.AllItems, 1).TextInput1.Text, "Change the text input")`
