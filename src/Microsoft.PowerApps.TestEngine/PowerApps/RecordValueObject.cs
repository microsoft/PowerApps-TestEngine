// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.PowerApps
{
    public class RecordValueObject
    {
        [JsonProperty("Value")]
        private string Value { get; set; }

        public RecordValueObject(string val)
        {
            this.Value = val;
        }
    }
}
