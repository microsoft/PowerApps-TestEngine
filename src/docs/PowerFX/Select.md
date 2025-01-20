# Select

`Select(control)`

`Select(control, row or column)`

`Select(control, row or column, child control)`

`Select(Index(gallerycontrol.AllItems, row or column).child control)`

This is the same functionality as the Power Apps [Select function](https://docs.microsoft.com/en-us/power-apps/maker/canvas-apps/functions/function-select).

When working with a nested gallery, use [Index()](https://learn.microsoft.com/en-us/power-platform/power-fx/reference/function-first-last) within the select function.

## Example

`Select(Button1)`

`Select(Gallery1,1)`

`Select(Gallery1,1,Button1)`

`Select(Index(Gallery1.AllItems, 2).Icon2)`

`Select(Index(Index(Gallery1.AllItems, 1).Gallery2.AllItems, 4).Icon3);`
