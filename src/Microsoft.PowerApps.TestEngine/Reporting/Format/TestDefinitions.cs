// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestDefinitions
    {
        [XmlElement(ElementName = "UnitTest")]
        public List<UnitTestDefinition> UnitTests { get; set; }
    }
}
