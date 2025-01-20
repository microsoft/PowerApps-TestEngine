// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using testengine.provider.copilot.portal;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class FindMessageFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState _testState;
        private readonly ILogger _logger;
        private readonly CopilotPortalProvider _provider;
        private static readonly RecordType _messageType = GenerateCopilotRecordType();

        public FindMessageFunction(ITestInfraFunctions testInfraFunctions, ITestState testState, ILogger logger, CopilotPortalProvider provider)
            : base(DPath.Root.Append(new DName("Experimental")), "FindMessage", _messageType, RecordType.Empty().Add("Type", FormulaType.String).Add("Text", FormulaType.String))
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
            _provider = provider;
        }

        public TableValue Execute(RecordValue criteria)
        {
            var fields = criteria == null ? new List<NamedValue>() : criteria.Fields;
            var waitField = fields.Any(f => f.Name == "Wait") ? criteria.GetField("Wait") : BlankValue.NewBlank();
            object waitValue = false;
            List<string> messages = new List<string>();

            if (!(waitField is BlankValue))
            {
                waitField.TryGetPrimitiveValue(out waitValue);
            }

            if (waitValue is bool shoudWait && shoudWait)
            {
                var startTime = DateTime.Now;
                var timeout = _testState.GetTimeout();
                while (DateTime.Now.Subtract(startTime).TotalSeconds < timeout)
                {
                    messages = FindMessages(fields, criteria);
                    Thread.Sleep(500);
                }
            } 
            else
            {
                messages = FindMessages(fields, criteria);
            }
            
            _logger.LogDebug($"Search Result: {messages.Count()} record(s)");

            var tableRows = messages.Select(json => ConvertToFormulaValue(ConvertJsonToCopilotMessage(json)) as RecordValue).ToArray<RecordValue>();
            return TableValue.NewTable(_messageType, tableRows);
        }

        private List<string> FindMessages(IEnumerable<NamedValue> fields, RecordValue criteria)
        {
            var type = fields.Any(f => f.Name == "Type") ? criteria.GetField("Type") : BlankValue.NewBlank();
            var text = fields.Any(f => f.Name == "Text") ? criteria.GetField("Text") : BlankValue.NewBlank();
            var adaptive = fields.Any(f => f.Name == "AdaptiveCard") ? criteria.GetField("AdaptiveCard") : BlankValue.NewBlank();
            var jsonPathQuery = "";
            object typeValue = String.Empty;
            object textValue = String.Empty;
            object adaptiveValue = false;

            if (!(type is BlankValue))
            {
                type.TryGetPrimitiveValue(out typeValue);
                if (!string.IsNullOrEmpty(jsonPathQuery))
                {
                    jsonPathQuery += " && ";
                }
                jsonPathQuery += $"$..[?(@.type == '{Sanitize(typeValue.ToString())}')]";
            }
            if (!(text is BlankValue))
            {
                type.TryGetPrimitiveValue(out typeValue);
                if (!string.IsNullOrEmpty(jsonPathQuery))
                {
                    jsonPathQuery += " && ";
                }
                text.TryGetPrimitiveValue(out textValue);
                jsonPathQuery += $"$..[@.text =~ /.*{Sanitize(textValue.ToString())}.*/i)]";
            }
            if (!(adaptive is BlankValue))
            {
                adaptive.TryGetPrimitiveValue(out adaptiveValue);
                if (!string.IsNullOrEmpty(jsonPathQuery))
                {
                    jsonPathQuery += " && ";
                }
                jsonPathQuery = $"$.attachments[?(@.contentType == 'application/vnd.microsoft.card.adaptive')]";
            }

            return string.IsNullOrEmpty(jsonPathQuery) ? _provider.Messages : _provider.Messages
                .Where(json => JToken.Parse(json).SelectTokens(jsonPathQuery).Any())
                .ToList();
        }

        public static CopilotMessage? ConvertJsonToCopilotMessage(string json)
        {
            return JsonSerializer.Deserialize<CopilotMessage>(json);
        }

        // Function to convert CLR object to PowerFx FormulaValue
        public static FormulaValue ConvertToFormulaValue(object obj)
        {
            if (obj == null)
            {
                return FormulaValue.NewBlank();
            }

            var type = obj.GetType();

            // Handle scalar types
            if (obj is string strObject)
            {
                return FormulaValue.New(strObject);
            }

            if (obj is int intObject)
            {
                return FormulaValue.New(intObject);
            }

            if (obj is decimal decObject)
            {
                return FormulaValue.New(decObject);
            }

            if (obj is DateTime dateTimeObject)
            {
                return FormulaValue.New(dateTimeObject);
            }

            // Handle collections (arrays, lists, etc.)
            if (obj is IEnumerable objList)
            {
                var items = new List<RecordValue>();
                foreach ( var item in objList )
                {
                    items.Add(ConvertToFormulaValue(item) as RecordValue);
                }
                return TableValue.NewTable(items.First().Type, items);
            }

            // Handle records (complex types)
            var fields = new List<NamedValue>();
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = property.GetValue(obj);
                fields.Add(new NamedValue(property.Name, ConvertToFormulaValue(value)));
            }

            return RecordValue.NewRecordFromFields(fields.ToArray());
        }

        public static string Sanitize(string value)
        {
            // Escape special characters for regex
            return Regex.Escape(value);
        }

        /// <summary>
        /// Power Fx Record and Table representation of Copilot activity and event data
        /// </summary>
        /// <returns></returns>
        private static RecordType GenerateCopilotRecordType()
        {
            return RecordType.Empty()
                .Add("Type", FormulaType.String)
                .Add("Id", FormulaType.String)
                .Add("Timestamp", FormulaType.DateTime)
                .Add("ChannelId", FormulaType.String)
                .Add("From", RecordType.Empty()
                    .Add("Id", FormulaType.String)
                    .Add("Name", FormulaType.String)
                    .Add("Role", FormulaType.String))
                .Add("Conversation", RecordType.Empty()
                    .Add("Id", FormulaType.String))
                .Add("Recipient", RecordType.Empty()
                    .Add("Id", FormulaType.String)
                    .Add("AadObjectId", FormulaType.String)
                    .Add("Role", FormulaType.String))
                .Add("TextFormat", FormulaType.String)
                .Add("MembersAdded", TableType.Empty()
                    .Add("Id", FormulaType.String)
                    .Add("Name", FormulaType.String)
                    .Add("Role", FormulaType.String))
                .Add("MembersRemoved", TableType.Empty()
                    .Add("Id", FormulaType.String)
                    .Add("Name", FormulaType.String)
                    .Add("Role", FormulaType.String))
                .Add("ReactionsAdded", TableType.Empty()
                    .Add("Id", FormulaType.String)
                    .Add("Name", FormulaType.String)
                    .Add("Role", FormulaType.String))
                .Add("ReactionsRemoved", TableType.Empty()
                    .Add("Id", FormulaType.String)
                    .Add("Name", FormulaType.String)
                    .Add("Role", FormulaType.String))
                .Add("Locale", FormulaType.String)
                .Add("Text", FormulaType.String)
                .Add("Speak", FormulaType.String)
                .Add("InputHint", FormulaType.String)
                .Add("Attachments", TableType.Empty()
                    .Add("ContentType", FormulaType.String)
                    .Add("Content", FormulaType.String)
                    .Add("ContentUrl", FormulaType.String)
                    .Add("Name", FormulaType.String)
                    .Add("ThumbnailUrl", FormulaType.String))
                .Add("Entities", TableType.Empty()
                    .Add("Type", FormulaType.String)
                    .Add("Citation", TableType.Empty()
                        .Add("Appearance", RecordType.Empty()
                            .Add("Text", FormulaType.String)
                            .Add("Abstract", FormulaType.String)
                            .Add("Type", FormulaType.String)
                            .Add("Name", FormulaType.String)
                            .Add("Url", FormulaType.String))
                        .Add("Position", FormulaType.Number)
                        .Add("Type", FormulaType.String)
                        .Add("Id", FormulaType.String))
                    .Add("Context", FormulaType.String)
                    .Add("Id", FormulaType.String)
                    .Add("AdditionalType", FormulaType.String)
                    .Add("Context", FormulaType.String))
                .Add("ChannelData", RecordType.Empty()
                    .Add("PvaGptFeedback", RecordType.Empty()
                        .Add("Endpoints", TableType.Empty()
                            .Add("EndpointUrl", FormulaType.String))
                        .Add("QueryRewrittingOpenAIResponse", FormulaType.String)
                        .Add("SummarizationOpenAIResponse", RecordType.Empty()
                            .Add("Result", RecordType.Empty()
                                .Add("Summary", FormulaType.String)
                                .Add("TextSummary", FormulaType.String)
                                .Add("SpeechSummary", FormulaType.String)
                                .Add("TextCitations", TableType.Empty()
                                    .Add("Id", FormulaType.String)
                                    .Add("Text", FormulaType.String)
                                    .Add("Title", FormulaType.String)
                                    .Add("Type", FormulaType.Number)
                                    .Add("Position", FormulaType.Number)
                                    .Add("EntityType", FormulaType.String)
                                    .Add("EntityContext", FormulaType.String)
                                    .Add("Url", FormulaType.String)
                                    .Add("SensitivityLabelInfo", FormulaType.String)
                                    .Add("SearchSourceId", FormulaType.String))
                                .Add("SpeechCitations", TableType.Empty()
                                    .Add("CitationText", FormulaType.String))
                                .Add("ContainsConfidentialData", FormulaType.Boolean)
                                .Add("HighestSensitivityLabelInfo", FormulaType.String)
                                .Add("MessageId", FormulaType.String)
                                .Add("SearchInput", FormulaType.String))
                            .Add("RawSummary", FormulaType.String)
                            .Add("CompletionTokens", FormulaType.Number)
                            .Add("PromptTokens", FormulaType.Number)
                            .Add("Prompt", FormulaType.String)
                            .Add("CompletionResponse", FormulaType.String)
                            .Add("ErrorCode", FormulaType.String)
                            .Add("CapiResourceUsage", FormulaType.String))
                        .Add("SearchResults", TableType.Empty()
                            .Add("ResultText", FormulaType.String))
                        .Add("VerifiedSearchResults", TableType.Empty()
                            .Add("VerifiedResultText", FormulaType.String))
                        .Add("SearchErrors", TableType.Empty()
                            .Add("ErrorText", FormulaType.String))
                        .Add("SearchLogs", TableType.Empty()
                            .Add("LogText", FormulaType.String))
                        .Add("SearchTerms", TableType.Empty()
                            .Add("TermText", FormulaType.String))
                        .Add("Message", FormulaType.String)
                        .Add("ScreenedMessage", FormulaType.String)
                        .Add("RewrittenMessage", FormulaType.String)
                        .Add("RewrittenMessageKeywords", TableType.Empty()
                            .Add("Keyword", FormulaType.String)
                            .Add("RewrittenText", FormulaType.String))
                        .Add("FilteredOpenAISummary", FormulaType.String)
                        .Add("ScreenedOpenAISummary", FormulaType.String)
                        .Add("ActivityId", FormulaType.String)
                        .Add("ConversationId", FormulaType.String)
                        .Add("PerformedContentProvenanceCheck", FormulaType.Boolean)
                        .Add("PerformedContentModerationCheck", FormulaType.Boolean)
                        .Add("CdsBotId", FormulaType.String)
                        .Add("TenantId", FormulaType.String)
                        .Add("EnvironmentId", FormulaType.String)
                        .Add("GptAnswerState", FormulaType.String)
                        .Add("TriggeredGptFallback", FormulaType.Boolean)
                        .Add("CompletionState", FormulaType.String))
                    .Add("StreamType", FormulaType.String)
                    .Add("StreamId", FormulaType.String))
                .Add("ReplyToId", FormulaType.String)
                .Add("ListenFor", TableType.Empty()
                    .Add("Text", FormulaType.String)
                    .Add("ListenType", FormulaType.String))
                .Add("TextHighlights", TableType.Empty()
                    .Add("Text", FormulaType.String)
                    .Add("HighlightType", FormulaType.String)
                    .Add("StartIndex", FormulaType.Number)
                    .Add("EndIndex", FormulaType.Number))
                .Add("ValueType", FormulaType.String)
                .Add("Value", RecordType.Empty()
                    .Add("Actions", TableType.Empty()
                        .Add("ActionId", FormulaType.String)
                        .Add("TopicId", FormulaType.String)
                        .Add("TriggerId", FormulaType.String)
                        .Add("DialogComponentId", FormulaType.String)
                        .Add("ActionType", FormulaType.String)
                        .Add("ConditionItemExit", TableType.Empty()
                            .Add("Condition", FormulaType.String))
                        .Add("VariableState", RecordType.Empty()
                            .Add("DialogState", TableType.Empty()
                                .Add("Key", FormulaType.String)
                                .Add("Value", FormulaType.String))
                            .Add("GlobalState", TableType.Empty()
                                .Add("Key", FormulaType.String)
                                .Add("Value", FormulaType.String)))
                        .Add("Exception", FormulaType.String)
                        .Add("ResultTrace", RecordType.Empty())))
                .Add("Name", FormulaType.String);
        }
    }
}
