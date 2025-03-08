using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

using Microsoft.PowerFx;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;

/// <summary>
/// This will execute Simulate connector request to Power Platform
/// </summary>
public class SimulateConnectorFunction : ReflectionFunction
{

    private static readonly RecordType dataverseRequest = RecordType.Empty();

    public SimulateConnectorFunction()
        : base(DPath.Root.Append(new DName("Experimental")), "SimulateConnector", FormulaType.Blank, dataverseRequest)
    {
    }

    public BlankValue Execute(RecordValue intercept)
    {
        ExecuteAsync(intercept).Wait();

        return FormulaValue.NewBlank();
    }

    private async Task ExecuteAsync(RecordValue intercept)
    {
        StringValue? connectorName = StringValue.New("");
        List<NamedValue> fields = new List<NamedValue>();

        await foreach (var field in intercept.GetFieldsAsync(CancellationToken.None))
        {
            fields.Add(field);
        }

        var connectorNameField = fields.FirstOrDefault(f => f.Name.ToLower() == "name");
        if (connectorNameField != null)
        {
            connectorName = intercept.GetField(connectorNameField.Name) as StringValue;
        }

        FormulaValue? thenFieldValue = null;

        var thenField = fields.FirstOrDefault(f => f.Name.ToLower() == "then");
        if (thenField != null)
        {
            thenFieldValue = intercept.GetField(thenField.Name);
        }

        switch (connectorName?.Value.ToLower())
        {
            case "weatherservice":
                if (thenFieldValue is RecordValue weatherRecord)
                {
                    GetCurrentWeatherFunction.Weather = weatherRecord;
                }
                break;   
        }
    }
}