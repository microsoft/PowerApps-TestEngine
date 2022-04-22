// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestErrorInfo
    {
        [XmlElement(ElementName = "Message")]
        public string Message { get; set; }
        [XmlElement(ElementName = "StackTrace")]
        public string StackTrace { get; set; }
    }
}
