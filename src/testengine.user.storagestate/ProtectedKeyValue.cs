// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testengine.user.storagestate
{
    public class ProtectedKeyValue
    {
        public string? KeyId { get; set; }
        public string? KeyName { get; set; }
        public string? ValueName { get; set; }
        public string? Data { get; set; }
    }
}
