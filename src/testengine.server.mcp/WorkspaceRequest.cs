// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

public class WorkspaceRequest
{
    public string Location { get; set; } = string.Empty;

    public string[] Scans { get; set; } =  new string[] { };

    public string PowerFx { get; set; } =  string.Empty;
}
