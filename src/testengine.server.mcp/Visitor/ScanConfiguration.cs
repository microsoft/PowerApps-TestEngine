// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerApps.TestEngine.MCP.Visitor
{
    /// <summary>
    /// Represents the schema for a scan configuration.
    /// The scan configuration contains rules for processing different node types.
    /// </summary>
    public class ScanReference
    {
        /// <summary>
        /// Gets or sets the name of the scan.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the scan.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the version of the scan configuration.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the list of rules to apply when a directory is encountered.
        /// </summary>
        public List<ScanRule>? OnDirectory { get; set; }

        /// <summary>
        /// Gets or sets the list of rules to apply when a file is encountered.
        /// </summary>
        public List<ScanRule>? OnFile { get; set; }

        /// <summary>
        /// Gets or sets the list of rules to apply when an object is encountered.
        /// </summary>
        public List<ScanRule>? OnObject { get; set; }

        /// <summary>
        /// Gets or sets the list of rules to apply when a property is encountered.
        /// </summary>
        public List<ScanRule>? OnProperty { get; set; }

        /// <summary>
        /// Gets or sets the list of rules to apply when a function is encountered.
        /// </summary>
        public List<ScanRule>? OnFunction { get; set; }

        /// <summary>
        /// Gets or sets the list of rules to apply at start of scan is encountered.
        /// </summary>
        public List<ScanRule>? OnStart { get; set; }

        /// <summary>
        /// Gets or sets the list of rules to apply at end of scan is encountered.
        /// </summary>
        public List<ScanRule>? OnEnd { get; set; }
    }

    /// <summary>
    /// Represents a single rule in the scan configuration.
    /// A rule consists of a condition (When) and an action (Then).
    /// </summary>
    public class ScanRule
    {
        /// <summary>
        /// Gets or sets the PowerFx expression that determines if the rule should be applied.
        /// </summary>
        public string? When { get; set; }

        /// <summary>
        /// Gets or sets the PowerFx expression that defines the action to take when the condition is met.
        /// </summary>
        public string? Then { get; set; }
    }

    /// <summary>
    /// Represents a fact discovered during workspace scanning.
    /// </summary>
    public class Fact
    {
        /// <summary>
        /// Gets or sets the path where the fact was discovered.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the name of the fact.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the fact.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the value of the fact.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets any additional context associated with the fact.
        /// </summary>
        public string? Context { get; set; }
    }
}
