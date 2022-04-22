// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestCounters
    {
        [XmlAttribute(AttributeName = "total")]
        public int Total { get; set; }
        [XmlAttribute(AttributeName = "executed")]
        public int Executed { get; set; }
        [XmlAttribute(AttributeName = "passed")]
        public int Passed { get; set; }
        [XmlAttribute(AttributeName = "failed")]
        public int Failed { get; set; }
        [XmlAttribute(AttributeName = "error")]
        public int Error { get; set; }
        [XmlAttribute(AttributeName = "timeout")]
        public int Timeout { get; set; }
        [XmlAttribute(AttributeName = "aborted")]
        public int Aborted { get; set; }
        [XmlAttribute(AttributeName = "inconclusive")]
        public int Inconclusive { get; set; }
        [XmlAttribute(AttributeName = "passedButRunAborted")]
        public int PassedButRunAborted { get; set; }
        [XmlAttribute(AttributeName = "notRunnable")]
        public int NotRunnable { get; set; }
        [XmlAttribute(AttributeName = "notExecuted")]
        public int NotExecuted { get; set; }
        [XmlAttribute(AttributeName = "disconnected")]
        public int Disconnected { get; set; }
        [XmlAttribute(AttributeName = "warning")]
        public int Warning { get; set; }
        [XmlAttribute(AttributeName = "completed")]
        public int Completed { get; set; }
        [XmlAttribute(AttributeName = "inProgress")]
        public int InProgress { get; set; }
        [XmlAttribute(AttributeName = "pending")]
        public int Pending { get; set; }
    }
}
