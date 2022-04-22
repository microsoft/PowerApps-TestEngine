// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestEntry
    {
        [XmlAttribute(AttributeName = "testId")]
        public string TestId { get; set; }
        [XmlAttribute(AttributeName = "executionId")]
        public string ExecutionId { get; set; }
        [XmlAttribute(AttributeName = "testListId")]
        public string TestListId { get; set; }
    }
}
