// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

public class SetVariableVisitor : TexlVisitor
{
    private readonly RecalcEngine _recalcEngine;

    public SetVariableVisitor(RecalcEngine recalcEngine)
    {
        _recalcEngine = recalcEngine;

    }

    private void HandleSetCall(CallNode callNode)
    {
        if (callNode.Args.Count >= 2)
        {
            var variableNameNode = callNode.Args.ChildNodes[0];
            var valueNode = callNode.Args.ChildNodes[1];

            if (variableNameNode is FirstNameNode firstName)
            {
                var variableName = firstName.Ident.Name;
                if (!_recalcEngine.TryGetValue(variableName, out FormulaValue existing))
                {
                    var defaultValue = GetDefaultValue(valueNode);
                    _recalcEngine.UpdateVariable(variableName, defaultValue);
                }
            }
        }
    }

    private FormulaValue GetDefaultValue(TexlNode variableType)
    {
        if (variableType is DecLitNode)
        {
            return FormulaValue.New(0);
        }

        if (variableType is StrLitNode)
        {
            return FormulaValue.New(String.Empty);
        }

        if (variableType is BoolLitNode)
        {
            return BooleanValue.New(false);
        }

        if (variableType is RecordNode recordNode)
        {
            return RecordValue.Empty();
        }

        if (variableType is CallNode functionCall)
        {
            var result = _recalcEngine.Check(functionCall.ToString());
            return GetDefaultFormulaValue(result.ReturnType);
        }

        // Default to string blank if the type is unknown
        return StringValue.NewBlank();
    }

    private FormulaValue GetDefaultFormulaValue(FormulaType variableType)
    {
        if (variableType is NumberType)
        {
            return FormulaValue.New(0);
        }

        if (variableType is DecimalType)
        {
            return FormulaValue.New(0.0);
        }

        if (variableType is StringType)
        {
            return FormulaValue.New(String.Empty);
        }

        if (variableType is BooleanType)
        {
            return BooleanValue.New(false);
        }

        if (variableType is GuidType)
        {
            return FormulaValue.New(Guid.Empty);
        }

        if (variableType is DateType)
        {
            return BooleanValue.New(DateTime.MinValue);
        }

        if (variableType is RecordType recordType)
        {
            return RecordValue.NewRecordFromFields(recordType, new List<NamedValue>());
        }

        if (variableType is TableType tableType)
        {
            return TableValue.NewTable(tableType.ToRecord());
        }

        // Default to string blank if the type is unknown
        return StringValue.NewBlank();
    }

    public override void Visit(TypeLiteralNode node)
    {

    }

    public override void Visit(ErrorNode node)
    {

    }

    public override void Visit(BlankNode node)
    {

    }

    public override void Visit(BoolLitNode node)
    {

    }

    public override void Visit(StrLitNode node)
    {

    }

    public override void Visit(NumLitNode node)
    {

    }

    public override void Visit(DecLitNode node)
    {

    }

    public override void Visit(FirstNameNode node)
    {

    }

    public override void Visit(ParentNode node)
    {

    }

    public override void Visit(SelfNode node)
    {

    }

    public override void PostVisit(StrInterpNode node)
    {

    }

    public override void PostVisit(DottedNameNode node)
    {

    }

    public override void PostVisit(UnaryOpNode node)
    {

    }

    public override void PostVisit(BinaryOpNode node)
    {

    }

    public override void PostVisit(VariadicOpNode node)
    {

    }

    public override void PostVisit(CallNode node)
    {
        HandleSetCall(node);
    }

    public override void PostVisit(ListNode node)
    {

    }

    public override void PostVisit(RecordNode node)
    {

    }

    public override void PostVisit(TableNode node)
    {

    }

    public override void PostVisit(AsNode node)
    {

    }
}
