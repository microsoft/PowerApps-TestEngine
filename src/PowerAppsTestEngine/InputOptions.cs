// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace PowerAppsTestEngine
{
    public class InputOptions
    {
        public string? EnvironmentId { get; set; }
        public string? TenantId { get; set; }
        public string? TestPlanFile { get; set; }
        public string? OutputDirectory { get; set; }
        public string? LogLevel { get; set; }
        public string? QueryParams { get; set; }
        public string? Domain { get; set; }
    }
}
