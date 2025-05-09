// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Microsoft.PowerApps.TestEngine.Providers
{
    public class StubOrganizationService : IOrganizationService
    {
        private readonly Dictionary<string, List<Entity>> _dataStore;

        public StubOrganizationService()
        {
            _dataStore = new Dictionary<string, List<Entity>>
            {
                {
                    "msdyn_plan", new List<Entity>
                    {
                        new Entity("msdyn_plan")
                        {
                            ["msdyn_planid"] = Guid.Parse("8bc6d90c-5a29-f011-8c4d-000d3a5a111e"),
                            ["msdyn_name"] = "Business Flight Requests",
                            ["msdyn_description"] = "This solution allows employlees to request, track, and manage business flights, with approvals from managers and reviewers ensuring compliance with company policies.",
                            ["msdyn_prompt"] = "Create a solution that allows flight requests for business purposes. The flight requests should be approved by managers and allow reviewers. Provide an adjustable method of booking based on destination and related booking code that has defined spending limits.",
                            ["msdyn_contentschemaversion"] = "1.1",
                            ["msdyn_languagecode"] = 1033,
                            ["modifiedon"] = DateTime.Parse("2025-05-05T02:38:53Z"),
                            ["modifiedby"] = new EntityReference("systemuser", Guid.Parse("64c53c59-d414-ef11-9f8a-00224803aff6"))
                            {
                                Name = "Graham Barnes"
                            }
                        }
                    }
                },
                {
                    "msdyn_planartifact", new List<Entity>
                    {
                        new Entity("msdyn_planartifact")
                        {
                            ["msdyn_planartifactid"] = Guid.Parse("93c6d90c-5a29-f011-8c4d-000d3a5a111e"),
                            ["msdyn_name"] = "Flight Request App",
                            ["msdyn_type"] = "PowerAppsCanvasApp",
                            ["msdyn_artifactstatus"] = new OptionSetValue(419550001),
                            ["msdyn_description"] = "An app for employees to submit, adjust, and view flight requests for business purposes.",
                            ["msdyn_parentplanid"] = new EntityReference("msdyn_plan", Guid.Parse("8bc6d90c-5a29-f011-8c4d-000d3a5a111e"))
                        },
                        new Entity("msdyn_planartifact")
                        {
                            ["msdyn_planartifactid"] = Guid.Parse("f681e712-5a29-f011-8c4d-000d3a5a111e"),
                            ["msdyn_name"] = "Manager Approval App",
                            ["msdyn_type"] = "PowerAppsModelApp",
                            ["msdyn_artifactstatus"] = new OptionSetValue(419550000),
                            ["msdyn_description"] = "An app for managers to approve or reject flight requests and view request details.",
                            ["msdyn_parentplanid"] = new EntityReference("msdyn_plan", Guid.Parse("8bc6d90c-5a29-f011-8c4d-000d3a5a111e"))
                        },
                        new Entity("msdyn_planartifact")
                        {
                            ["msdyn_planartifactid"] = Guid.Parse("fa81e712-5a29-f011-8c4d-000d3a5a111e"),
                            ["msdyn_name"] = "Reviewer Compliance App",
                            ["msdyn_type"] = "PowerAppsModelApp",
                            ["msdyn_artifactstatus"] = new OptionSetValue(419550000),
                            ["msdyn_description"] = "An app for reviewers to review flight requests for compliance and provide feedback.",
                            ["msdyn_parentplanid"] = new EntityReference("msdyn_plan", Guid.Parse("8bc6d90c-5a29-f011-8c4d-000d3a5a111e"))
                        },
                        new Entity("msdyn_planartifact")
                        {
                            ["msdyn_planartifactid"] = Guid.Parse("ff81e712-5a29-f011-8c4d-000d3a5a111e"),
                            ["msdyn_name"] = "Flight Request Notification Flow",
                            ["msdyn_type"] = "PowerAutomateFlow",
                            ["msdyn_artifactstatus"] = new OptionSetValue(419550000),
                            ["msdyn_description"] = "A flow to notify employees when their flight request is approved or rejected.",
                            ["msdyn_parentplanid"] = new EntityReference("msdyn_plan", Guid.Parse("8bc6d90c-5a29-f011-8c4d-000d3a5a111e"))
                        }
                    }
                }
            };
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            if (_dataStore.ContainsKey(entityName))
            {
                var entity = _dataStore[entityName].Find(e => e.Id == id);
                if (entity != null)
                {
                    return entity;
                }
            }

            throw new Exception($"Entity {entityName} with ID {id} not found.");
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            if (query is QueryExpression queryExpression)
            {
                if (_dataStore.ContainsKey(queryExpression.EntityName))
                {
                    var results = new EntityCollection();
                    foreach (var entity in _dataStore[queryExpression.EntityName])
                    {
                        if (queryExpression.Criteria.Conditions.Count == 0 || MatchesCriteria(entity, queryExpression.Criteria))
                        {
                            results.Entities.Add(entity);
                        }
                    }
                    return results;
                }
            }

            return new EntityCollection();
        }

        public Guid Create(Entity entity)
        {
            if (!_dataStore.ContainsKey(entity.LogicalName))
            {
                _dataStore[entity.LogicalName] = new List<Entity>();
            }

            entity.Id = Guid.NewGuid();
            _dataStore[entity.LogicalName].Add(entity);
            return entity.Id;
        }

        public void Update(Entity entity)
        {
            if (_dataStore.ContainsKey(entity.LogicalName))
            {
                var existingEntity = _dataStore[entity.LogicalName].Find(e => e.Id == entity.Id);
                if (existingEntity != null)
                {
                    _dataStore[entity.LogicalName].Remove(existingEntity);
                    _dataStore[entity.LogicalName].Add(entity);
                }
            }
        }

        public void Delete(string entityName, Guid id)
        {
            if (_dataStore.ContainsKey(entityName))
            {
                var entity = _dataStore[entityName].Find(e => e.Id == id);
                if (entity != null)
                {
                    _dataStore[entityName].Remove(entity);
                }
            }
        }

        private bool MatchesCriteria(Entity entity, FilterExpression filter)
        {
            foreach (var condition in filter.Conditions)
            {
                if (!entity.Attributes.ContainsKey(condition.AttributeName) ||
                    !entity.Attributes[condition.AttributeName].Equals(condition.Values[0]))
                {
                    return false;
                }
            }

            return true;
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            throw new NotImplementedException();
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            throw new NotImplementedException();
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            throw new NotImplementedException();
        }

        Xrm.Sdk.Entity IOrganizationService.Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            throw new NotImplementedException();
        }
    }
}
