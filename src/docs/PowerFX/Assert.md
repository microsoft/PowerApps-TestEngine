# Assert

`Assert(BooleanExpression)`

`Assert(BooleanExpression, Message)`

The Assert function takes in a Power Fx expression that should evaluate to a boolean value. If the value returned is false, the test will fail.

## Example
`Assert(Label1.Text = "1");`

`Assert(Label1.Text = "1", "Checking that the Label1 text is set to 1");`