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
    }
}
