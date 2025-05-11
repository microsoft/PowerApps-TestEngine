// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace testengine.provider.mcp
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
