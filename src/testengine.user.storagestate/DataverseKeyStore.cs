// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace testengine.user.storagestate
{
    public class DataverseKeyStore : IXmlRepository
    {
        private readonly ILogger _logger;
        private readonly IOrganizationService _service;
        private string _friendlyName;

        public DataverseKeyStore(ILogger logger, IOrganizationService organizationService, string friendlyName)
        {
            _logger = logger;
            _service = organizationService;
            _friendlyName = friendlyName;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            // Retrieve keys from Dataverse
            var query = new QueryExpression("te_key")
            {
                ColumnSet = new ColumnSet("te_xml"),
                Criteria = new FilterExpression
                {
                    Conditions =
                {
                    new ConditionExpression("te_name", ConditionOperator.Equal, _friendlyName)
                }
                }
            };

            var keys = _service.RetrieveMultiple(query)
            .Entities
            .Select(e => XElement.Parse(e.GetAttributeValue<string>("te_xml")))
            .ToList();

            _logger.LogDebug($"Found {keys.Count()} keys");

            return keys.AsReadOnly();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            var keyEntity = new Entity("te_key")
            {
                ["te_name"] = _friendlyName,
                ["te_xml"] = element.ToString(SaveOptions.DisableFormatting)
            };

            _service.Create(keyEntity);
        }
    }
}
