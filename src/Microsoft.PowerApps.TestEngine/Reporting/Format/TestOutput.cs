// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestOutput
    {
        [XmlElement(ElementName = "StdOut")]
        public string StdOut { get; set; }

        [XmlElement(ElementName = "ErrorInfo")]
        public TestErrorInfo ErrorInfo { get; set; }
    }
}
