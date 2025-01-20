using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

using Microsoft.PowerFx;

/// <summary>
/// This will execute Simulate dataverse request to Power Platform
/// </summary>
public class SimulateDataverseFunction : ReflectionFunction
{
    private static readonly RecordType dataverseRequest = RecordType.Empty();
    public RecalcEngine Engine { get; set; } = new RecalcEngine();

    public SimulateDataverseFunction()
        : base(DPath.Root.Append(new DName("Experimental")), "SimulateDataverse", FormulaType.Blank, dataverseRequest)
    {
    }

    public BlankValue Execute(RecordValue intercept)
    {
        ExecuteAsync(intercept).Wait();

        return FormulaValue.NewBlank();
    }

    private async Task ExecuteAsync(RecordValue intercept)
    {
        StringValue? entityName = StringValue.New("");
        List<NamedValue> fields = new List<NamedValue>();

        await foreach (var field in intercept.GetFieldsAsync(CancellationToken.None))
        {
            fields.Add(field);
        }

        var entityNameField = fields.FirstOrDefault(f => f.Name.ToLower() == "entity");
        if (entityNameField != null)
        {
            entityName = intercept.GetField(entityNameField.Name) as StringValue;
        }

        FormulaValue? thenFieldValue = null;

        var thenField = fields.FirstOrDefault(f => f.Name.ToLower() == "then");
        if (thenField != null)
        {
            thenFieldValue = intercept.GetField(thenField.Name);
        }

        switch (entityName?.Value.ToLower())
        {
            case "accounts":
                if (thenFieldValue is TableValue accountsTable)
                {
                    if (accountsTable.Rows.Count() == 0)
                    {
                        Engine.TryGetValue("accounts", out FormulaValue existing);
                        if (existing is TableValue existingAccounts)
                        {
                            foreach ( DValue<RecordValue> row in existingAccounts.Rows.ToList())
                            {
                                await existingAccounts.RemoveAsync(new List<FormulaValue>() { row.Value }, true, CancellationToken.None);
                            }
                            Engine.UpdateVariable("accounts", existingAccounts);
                        }
                    }
                }
                break;   
        }
    }
}