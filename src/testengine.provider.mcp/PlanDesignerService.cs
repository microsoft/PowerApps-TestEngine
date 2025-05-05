// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class PlanDesignerService
    {
        private readonly IOrganizationService _organizationService;

        public PlanDesignerService(IOrganizationService organizationService)
        {
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        /// <summary>
        /// Retrieves a list of plans from Dataverse.
        /// </summary>
        /// <returns>A list of plans with basic details.</returns>
        public async Task<List<Plan>> GetPlansAsync()
        {
            var query = new QueryExpression("msdyn_plan")
            {
                ColumnSet = new ColumnSet("msdyn_planid", "msdyn_name", "msdyn_description", "modifiedon")
            };

            var plans = new List<Plan>();
            var results = _organizationService.RetrieveMultiple(query);

            foreach (var entity in results.Entities)
            {
                plans.Add(new Plan
                {
                    Id = entity.GetAttributeValue<Guid>("msdyn_planid"),
                    Name = entity.GetAttributeValue<string>("msdyn_name"),
                    Description = entity.GetAttributeValue<string>("msdyn_description"),
                    ModifiedOn = entity.GetAttributeValue<DateTime>("modifiedon")
                });
            }

            return await Task.FromResult(plans);
        }

        /// <summary>
        /// Retrieves details for a specific plan by its ID.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <returns>Details of the specified plan.</returns>
        public async Task<PlanDetails> GetPlanDetailsAsync(Guid planId)
        {
            var query = new QueryExpression("msdyn_plan")
            {
                ColumnSet = new ColumnSet("msdyn_planid", "msdyn_name", "msdyn_description", "msdyn_prompt", "msdyn_contentschemaversion", "msdyn_languagecode", "modifiedon"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("msdyn_planid", ConditionOperator.Equal, planId)
                    }
                }
            };

            var result = _organizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (result == null)
            {
                throw new Exception($"Plan with ID {planId} not found.");
            }

            return await Task.FromResult(new PlanDetails
            {
                Id = result.GetAttributeValue<Guid>("msdyn_planid"),
                Name = result.GetAttributeValue<string>("msdyn_name"),
                Description = result.GetAttributeValue<string>("msdyn_description"),
                Prompt = result.GetAttributeValue<string>("msdyn_prompt"),
                ContentSchemaVersion = result.GetAttributeValue<string>("msdyn_contentschemaversion"),
                LanguageCode = result.GetAttributeValue<int>("msdyn_languagecode"),
                ModifiedOn = result.GetAttributeValue<DateTime>("modifiedon")
            });
        }

        /// <summary>
        /// Retrieves artifacts for a specific plan by its ID.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <returns>A list of artifacts associated with the plan.</returns>
        public async Task<List<Artifact>> GetPlanArtifactsAsync(Guid planId)
        {
            var query = new QueryExpression("msdyn_planartifact")
            {
                ColumnSet = new ColumnSet("msdyn_planartifactid", "msdyn_name", "msdyn_type", "msdyn_artifactstatus", "msdyn_description"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("msdyn_parentplanid", ConditionOperator.Equal, planId)
                    }
                }
            };

            var artifacts = new List<Artifact>();
            var results = _organizationService.RetrieveMultiple(query);

            foreach (var entity in results.Entities)
            {
                artifacts.Add(new Artifact
                {
                    Id = entity.GetAttributeValue<Guid>("msdyn_planartifactid"),
                    Name = entity.GetAttributeValue<string>("msdyn_name"),
                    Type = entity.GetAttributeValue<string>("msdyn_type"),
                    Status = entity.GetAttributeValue<OptionSetValue>("msdyn_artifactstatus")?.Value,
                    Description = entity.GetAttributeValue<string>("msdyn_description")
                });
            }

            return await Task.FromResult(artifacts);
        }

        /// <summary>
        /// Retrieves solution assets for a specific plan by its ID.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <returns>A list of solution assets associated with the plan.</returns>
        public async Task<List<SolutionAsset>> GetSolutionAssetsAsync(Guid planId)
        {
            // Assuming solution assets are stored in a custom entity related to the plan
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("msdyn_planid", ConditionOperator.Equal, planId)
                    }
                }
            };

            var assets = new List<SolutionAsset>();
            var results = _organizationService.RetrieveMultiple(query);

            foreach (var entity in results.Entities)
            {
                assets.Add(new SolutionAsset
                {
                    Id = entity.GetAttributeValue<Guid>("solutionid"),
                    FriendlyName = entity.GetAttributeValue<string>("friendlyname"),
                    UniqueName = entity.GetAttributeValue<string>("uniquename")
                });
            }

            return await Task.FromResult(assets);
        }
    }

    // Supporting classes for data models
    public class Plan
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class PlanDetails : Plan
    {
        public string? Prompt { get; set; }
        public string? ContentSchemaVersion { get; set; }
        public int LanguageCode { get; set; }
    }

    public class Artifact
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public int? Status { get; set; }
        public string? Description { get; set; }
    }

    public class SolutionAsset
    {
        public Guid Id { get; set; }
        public string? FriendlyName { get; set; }
        public string? UniqueName { get; set; }
    }
}
