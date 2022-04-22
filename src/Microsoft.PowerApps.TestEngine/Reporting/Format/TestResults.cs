// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestResults
    {
        [XmlElement(ElementName = "UnitTestResult")]
        public List<UnitTestResult> UnitTestResults { get; set; }
    }
}
