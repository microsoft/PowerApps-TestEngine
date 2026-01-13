// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
