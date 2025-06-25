// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using System.Collections.Concurrent;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    /// <summary>
    /// Power FX RecordValue for Copilot Portal state
    /// </summary>
    public class CopilotStateRecordValue : RecordValue
    {
        private readonly CopilotPortalProvider _provider;

        public CopilotStateRecordValue(CopilotPortalProvider provider) 
            : base(RecordType.Empty().Add("Messages", FormulaType.String).Add("ConversationId", FormulaType.String))
        {
            _provider = provider;
        }

        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {
            switch (fieldName)
            {
                case "Messages":
                    // Return the latest messages as a concatenated string
                    var messages = _provider.Messages.ToArray();
                    var allMessages = string.Join("\n", messages);
                    result = FormulaValue.New(allMessages);
                    return true;

                case "ConversationId":
                    result = FormulaValue.New(_provider.ConversationId ?? string.Empty);
                    return true;

                default:
                    result = FormulaValue.NewBlank();
                    return false;
            }
        }
    }
}
