using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Xunit;

public class PowerFxTests
{
    public PowerFxTests()
    {
        // Clear any test state from simulation
        GetCurrentWeatherFunction.Weather = null;
    }

    [Fact]
    public void CanClone()
    {
        // Arrange
        var function = new GetCurrentWeatherFunction();
        var defaultWeather = GetCurrentWeatherFunction.ConvertToRecordValue(function.DefaultWeather("Test"));
        var engine = PowerFxEngine.Init(out ParserOptions options, "en-us");
        var clone = PowerFxEngine.CloneWithBlankValues(defaultWeather);



        // Act & Assert
        engine.UpdateVariable("weather", clone);
        Assert.True(engine.TryGetValue("weather", out FormulaValue blankWeather));

        engine.UpdateVariable("weather", defaultWeather);

        Assert.True(engine.TryGetValue("weather", out FormulaValue engineWeather));

        Assert.IsType<RecordValue>(engineWeather, exactMatch: false);

        var oldWeather = blankWeather as RecordValue;
        var newWeather = engineWeather as RecordValue;

        Assert.Null((oldWeather?.GetField("Location") as StringValue)?.Value);
        Assert.Equal("Test", (newWeather?.GetField("Location") as StringValue)?.Value);

    }

    [Theory]
    [InlineData("Set(a,1)", "a", "1")]
    [InlineData("Set(a,\"Test\")", "a", "\"Test\"")]
    [InlineData("SetProperty(Label1.Text,\"Test\")", "Label1", "{\"Text\":\"Test\"}")]
    [InlineData("Set(data, Table({Name:\"Test\"}))", "data", "{\"value\":[{\"Name\":\"Test\"}]}")]
    [InlineData("Collect(data, {Name:\"Test\"})", "data", "{\"value\":[{\"Name\":\"Test\"}]}")]
    [InlineData("WeatherService.GetCurrentWeather(\"Test\")", "", @"{""Condition"":""Sunny"",""Humidity"":50,""Location"":""Test"",""Temperature"":25,""WindSpeed"":10}")]
    [InlineData("Experimental.SimulateConnector({Name:\"WeatherService\",Then:{Humidity:1}});WeatherService.GetCurrentWeather(\"Test\")", "", "{\"Humidity\":1}")]
    [InlineData("WeatherService.GetCurrentWeather(\"Test\").Condition", "", "\"Sunny\"")]
    [InlineData("CountRows(accounts)", "", "1")]
    [InlineData("Experimental.SimulateDataverse({Entity:\"accounts\",Then:Table()});CountRows(accounts)", "", "0")]
    public void ExpectedJsonResults(string code, string variable, string expectedResult)
    {
        // Arange

        // Act
        var variableName = !string.IsNullOrEmpty(variable) ? ";" + variable : String.Empty;
        var result = PowerFxEngine.Execute(code + variableName);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("Assert(1=1)")]
    public void AssertResultsWithNoException(string code)
    {
        // Arange

        // Act
        var result = PowerFxEngine.Execute(code);

        // Assert
        Assert.Equal("true", result);
    }

    [Theory]
    [InlineData("Assert(1=2)", "")]
    [InlineData("Assert(1=2, \"Test\")", "Test")]
    [InlineData("SetProperty(Label1.Text,\"Test\");Assert(Label1.Text=1, \"Test\")", "Test")]
    public void AssertResultsWithException(string code, string expectedMessage)
    {
        // Arange

        // Act & Assert
        try
        {
            PowerFxEngine.Execute(code);
        } 
        catch (Exception ex)
        {
            Assert.Equal(expectedMessage, ex.Message);
        }
    }
}
