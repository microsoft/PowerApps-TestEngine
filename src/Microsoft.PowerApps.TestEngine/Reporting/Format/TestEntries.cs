// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestEntries
    {
        [XmlElement(ElementName = "TestEntry")]
        public List<TestEntry> Entries { get; set; }
    }
}
