// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace PowerAppsTestEngineWrapper
{
    public class InputOptions
    {
        public string? OutputFile { get; set; }
        public string? RunName { get; set; }
        public string? EnvironmentId { get; set; }
        public string? TenantId { get; set; }
        public string? TestPlanFile { get; set; }
        public string? OutputDirectory { get; set; }
        public string? LogLevel { get; set; }
        public string? QueryParams { get; set; }
        public string? Domain { get; set; }
        public string? Modules { get; set; }
        public string? UserAuth { get; set; }
        public string? Provider { get; set; }
        public string? UserAuthType { get; set; }
        public string? Wait { get; set; }
        public string? Record { get; set; }
        public string? UseStaticContext { get; set; }
    }
}
