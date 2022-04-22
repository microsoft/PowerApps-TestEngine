// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Serialization;

namespace Microsoft.PowerApps.TestEngine.Reporting.Format
{
    public class TestResultFiles
    {
        [XmlElement(ElementName = "ResultFile")]
        public List<ResultFile> ResultFile { get; set; }
    }
}
