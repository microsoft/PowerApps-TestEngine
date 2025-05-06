// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Text;
using System.Text.Json;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

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
        public List<Plan> GetPlans()
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

            return plans;
        }

        /// <summary>
        /// Retrieves details for a specific plan by its ID.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <returns>Details of the specified plan.</returns>
        public PlanDetails GetPlanDetails(Guid planId)
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

            return new PlanDetails
            {
                Id = result.GetAttributeValue<Guid>("msdyn_planid"),
                Name = result.GetAttributeValue<string>("msdyn_name"),
                Description = result.GetAttributeValue<string>("msdyn_description"),
                Prompt = result.GetAttributeValue<string>("msdyn_prompt"),
                ContentSchemaVersion = result.GetAttributeValue<string>("msdyn_contentschemaversion"),
                LanguageCode = result.GetAttributeValue<int>("msdyn_languagecode"),
                ModifiedOn = result.GetAttributeValue<DateTime>("modifiedon"),
                Content = DownloadJsonFileContent("msdyn_plan", planId, "msdyn_content"),
                Artifacts = GetPlanArtifacts(planId)
            };
        }

        public object DownloadJsonFileContent(string entity, Guid id, string column)
        {
            // Retrieve the msdyn_plan record
            Entity plan = _organizationService.Retrieve(entity, id, new ColumnSet(column));

            // Check if the msdyn_content column is present
            if (plan.Contains(column))
            {
                // Get the file column value
                var fileId = plan.GetAttributeValue<Guid>(column);

                // Retrieve the file content using the fileId
                return RetrieveFileContent(new EntityReference(entity, id), column);
            }

            return new Dictionary<string, object>();
        }

        private object RetrieveFileContent(EntityReference fileId, string column)
        {
            InitializeFileBlocksDownloadRequest initializeFileBlocksDownloadRequest = new()
            {
                Target = fileId,
                FileAttributeName = column
            };

            var initializeFileBlocksDownloadResponse =
                (InitializeFileBlocksDownloadResponse)_organizationService.Execute(initializeFileBlocksDownloadRequest);

            string fileContinuationToken = initializeFileBlocksDownloadResponse.FileContinuationToken;
            long fileSizeInBytes = initializeFileBlocksDownloadResponse.FileSizeInBytes;

            List<byte> fileBytes = new((int)fileSizeInBytes);

            long offset = 0;
            long blockSizeDownload = 4 * 1024 * 1024; // 4 MB

            // File size may be smaller than defined block size
            if (fileSizeInBytes < blockSizeDownload)
            {
                blockSizeDownload = fileSizeInBytes;
            }

            while (fileSizeInBytes > 0)
            {
                // Prepare the request
                DownloadBlockRequest downLoadBlockRequest = new()
                {
                    BlockLength = blockSizeDownload,
                    FileContinuationToken = fileContinuationToken,
                    Offset = offset
                };

                // Send the request
                var downloadBlockResponse =
                           (DownloadBlockResponse)_organizationService.Execute(downLoadBlockRequest);

                // Add the block returned to the list
                fileBytes.AddRange(downloadBlockResponse.Data);

                // Subtract the amount downloaded,
                // which may make fileSizeInBytes < 0 and indicate
                // no further blocks to download
                fileSizeInBytes -= (int)blockSizeDownload;
                // Increment the offset to start at the beginning of the next block.
                offset += blockSizeDownload;
            }

            var data = fileBytes.ToArray();
            var content = Encoding.UTF8.GetString(data);

            // Check if the content starts with '{' (indicating JSON)
            if (content.TrimStart().StartsWith("{"))
            {
                try
                {
                    // Convert JSON to object
                    return DeserializeToDictionary(content);
                }
                catch (JsonException ex)
                {
                    throw new Exception($"Failed to parse JSON content: {ex.Message}", ex);
                }
            }

            return new Dictionary<string, object>
            {
                { "Data", content }
            };
        }

        private object DeserializeToDictionary(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserialize the JSON into a generic object
            var deserializedObject = JsonSerializer.Deserialize<object>(json, options);

            // Recursively convert the object into a Dictionary<string, object>
            return ConvertToDictionary(deserializedObject);
        }

        private object ConvertToDictionary(object? obj)
        {
            if (obj is JsonElement jsonElement)
            {
                return ConvertJsonElementToDictionary(jsonElement);
            }

            if (obj is Dictionary<string, object> dictionary)
            {
                return dictionary.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)ConvertToDictionary(kvp.Value)
                );
            }

            if (obj is List<object> list)
            {
                return new Dictionary<string, object>
                {
                    { "Array", list.Select(ConvertToDictionary).ToList() }
                };
            }

            return new Dictionary<string, object> { { "Value", obj ?? string.Empty } };
        }

        private object ConvertJsonElementToDictionary(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return element.EnumerateObject()
                        .ToDictionary(
                            property => property.Name,
                            property => ConvertJsonElementToDictionary(property.Value)
                        );

                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(ConvertJsonElementToDictionary).ToList();

                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    return element.GetDecimal();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();

                case JsonValueKind.Null:
                    return string.Empty;

                default:
                    return element.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Retrieves artifacts for a specific plan by its ID.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <returns>A list of artifacts associated with the plan.</returns>
        public List<Artifact> GetPlanArtifacts(Guid planId)
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
                var id = entity.GetAttributeValue<Guid>("msdyn_planartifactid");
                artifacts.Add(new Artifact
                {
                    Id = id,
                    Name = entity.GetAttributeValue<string>("msdyn_name"),
                    Type = entity.GetAttributeValue<string>("msdyn_type"),
                    Status = entity.GetAttributeValue<OptionSetValue>("msdyn_artifactstatus")?.Value,
                    Description = entity.GetAttributeValue<string>("msdyn_description"),
                    Metadata = DownloadJsonFileContent("msdyn_planartifact", id, "msdyn_artifactmetadata"),
                    Proposal = DownloadJsonFileContent("msdyn_planartifact", id, "msdyn_proposal")
                });
            }

            return artifacts;
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

        public object Content { get; set; } = new Dictionary<string, object>();

        public List<Artifact> Artifacts { get; set; } = new List<Artifact>();
    }

    public class Artifact
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public int? Status { get; set; }
        public string? Description { get; set; }

        public object Metadata { get; set; } = new Dictionary<string, object>();

        public object Proposal { get; set; } = new Dictionary<string, object>();
    }

    public class SolutionAsset
    {
        public Guid Id { get; set; }
        public string? FriendlyName { get; set; }
        public string? UniqueName { get; set; }
    }
}
