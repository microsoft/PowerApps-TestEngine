// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class CopilotStateRecordValue : RecordValue
    {
        IMessageProvider _provider;

        static RecordType _messageType = RecordType.Empty()
            .Add("Message", StringType.String);

        static TableType _messageTable = TableType.Empty()
            .Add("Message", _messageType);

        static RecordType _copilotType = RecordType.Empty()
            .Add("Messages", _messageTable);

        public CopilotStateRecordValue(IMessageProvider provider) : base(_copilotType)
        {
            _provider = provider;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            switch (fieldName)
            {
                case "Messages":
                    result = TableValue.NewTable(
                        _messageType,
                        _provider.Messages.Select(
                            json => RecordValue.NewRecordFromFields(
                                    _messageType,
                                    new NamedValue("Message", StringValue.New(json))
                                    )
                            ).ToArray());
                    return true;
                default:
                    result = null;
                    return false;
            }
        }
    }
}
