// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace testengine.provider.copilot.portal
{
    public class CopilotMessage
    {
        public string? Type { get; set; }
        public string? Id { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? ChannelId { get; set; }
        public From? From { get; set; }
        public Conversation? Conversation { get; set; }
        public Recipient? Recipient { get; set; }
        public string? TextFormat { get; set; }
        public Member[]? MembersAdded { get; set; }
        public Member[]? MembersRemoved { get; set; }
        public Reaction[]? ReactionsAdded { get; set; }
        public Reaction[]? ReactionsRemoved { get; set; }
        public string? Locale { get; set; }
        public string? Text { get; set; }
        public string? Speak { get; set; }
        public string? InputHint { get; set; }
        public Attachment[]? Attachments { get; set; }
        public Entity[]? Entities { get; set; }
        public ChannelData? ChannelData { get; set; }
        public string? ReplyToId { get; set; }
        public ListenFor[]? ListenFor { get; set; }
        public TextHighlight[]? TextHighlights { get; set; }
        public string? ValueType { get; set; }
        public Value? Value { get; set; }
        public string? Name { get; set; }
    }

    public class From
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
    }

    public class Conversation
    {
        public string? Id { get; set; }
    }

    public class Recipient
    {
        public string? Id { get; set; }
        public string? AadObjectId { get; set; }
        public string? Role { get; set; }
    }

    public class Member
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
    }

    public class Reaction
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
    }

    public class Attachment
    {
        public string? ContentType { get; set; }
        public string? Content { get; set; }
        public string? ContentUrl { get; set; }
        public string? Name { get; set; }
        public string? ThumbnailUrl { get; set; }
    }

    public class Entity
    {
        [JsonPropertyName("@type")]
        public string[]? TypeArray { get; set; }

        [JsonPropertyName("@context")]
        public string[]? ContextArray { get; set; }

        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }


        [JsonPropertyName("context")]
        public string? Context { get; set; }
    }

    public class ChannelData
    {
        // Define properties for ChannelData based on the JSON structure
    }

    public class ListenFor
    {
        public string? Text { get; set; }
        public string? ListenType { get; set; }
    }

    public class TextHighlight
    {
        public string? Text { get; set; }
        public string? HighlightType { get; set; }
        public int? StartIndex { get; set; }
        public int? EndIndex { get; set; }
    }

    public class Value
    {
        public Action[]? Actions { get; set; }
    }

    public class Action
    {
        public string? ActionId { get; set; }
        public string? TopicId { get; set; }
        public string? TriggerId { get; set; }
        public string? DialogComponentId { get; set; }
        public string? ActionType { get; set; }
        public string[]? ConditionItemExit { get; set; }
        public VariableState? VariableState { get; set; }
        public string? Exception { get; set; }
        public ResultTrace? ResultTrace { get; set; }
    }

    public class VariableState
    {
        public Dictionary<string, object>? DialogState { get; set; }
        public Dictionary<string, object>? GlobalState { get; set; }
    }

    public class ResultTrace
    {
        // Define properties for ResultTrace based on the JSON structure
    }
}
