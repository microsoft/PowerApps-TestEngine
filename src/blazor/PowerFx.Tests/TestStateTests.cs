using Xunit;

namespace PowerFx.Tests
{
    public class TestStateTests
    {
        [Theory]
        [InlineData("1", "1")]
        [InlineData("Lower(Language())", "\"en-us\"")]
        [InlineData(@"// Settings:
// Code:
Lower(Language())", "\"en-us\"")]
        [InlineData(@"// Settings:
// Code:
Switch(Lower(Language()), ""fr-fr"", ""Bonjour"", ""Hello"")", "\"Hello\"")]
        [InlineData(@"// Settings:
locale: fr-fr
// Code:
Switch(Lower(Language()); ""fr-fr""; ""Bonjour""; ""Hello"")", "\"Bonjour\"")]
        [InlineData(@"// File: test.pa.yaml
Properties:
  OnVisible: =Set(label1Text, ""Hello"")
// Code:
label1Text", "\"Hello\"")]
        [InlineData(@"// File: test.pa.yaml
Properties:
  OnVisible: =Set(label1Text, ""Hello"")
Controls:
  Label1:
    Text: = label1Text
// Code:
Label1.Text", "\"Hello\"")]

        [InlineData(@"// File: test.pa.yaml
Controls:
  Label1:
    FontSize: = 12
// Code:
Label1.FontSize", "12")]
        [InlineData(@"// File: test.pa.yaml
Controls:
  Label1:
    Width: = 12.5
// Code:
Label1.Width", "12.5")]
        [InlineData(@"// File: app.pa.yaml
OnStart: =Set(test,1)
// Code:
test", "1")]
        [InlineData(@"// File: test.pa.yaml
Controls:
  Label1:
    Text: = ""Start""
// Code:
SetProperty(Label1.Text, ""Hello"");
Label1.Text", "\"Hello\"")]
        [InlineData(@"// File: test.pa.yaml
Controls:
  Label1:
    Text: = ""Start""
  Label2:
    Text: = Label1.Text & "" Now""
// Code:
SetProperty(Label1.Text, ""Hello"");
Label2.Text", "\"Hello Now\"")]
        [InlineData(@"Set(a,1);a;", "1")]
        public void Simple(string value, string expected)
        {
            // Arrange
            var state = new TestState(value);

            // Act 
            var result = state.ExecuteCode();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("custom", "{text: Text}","Test1(data: custom): Text = data.text", "Test1({text: \"A\"})", "\"A\"")]
        [InlineData("custom", "{num: Number}", "Test1(data: custom): Number = data.num", "Test1({num: 1})", "1")]
        [InlineData("custom", "[{num: Number}]", "Test1(data: custom): Number = CountRows(data)", "Test1(Table({num: 0}))", "1")]
        [InlineData("Controls", "[{Name: Text, IsVisible: Boolean}]", @"Validate(x:Controls):Number =
Sum(
    ForAll(
        x,
        IfError(
            AssertNotError(Not(ThisRecord.IsVisible),""Not Visible""),
            {Value: 1},
            {Value: 0}
        )
    ),
    Value
)", "Validate(Table({Name: \"First1\", IsVisible: true}))", "1")]
        public void UserDefinedFunction(string typeName, string typeDeclaration, string function, string text, string expected)
        {
            // Arrange
            var value = string.Format(@"// Types:
{0}: {1}

// Function:
{2}

// Test:
{3}
", typeName, typeDeclaration, function, text);
            var state = new TestState(value);

            // Act 
            var result = state.ExecuteCode();

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
