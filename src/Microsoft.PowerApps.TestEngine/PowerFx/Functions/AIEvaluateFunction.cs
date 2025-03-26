// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Dynamic;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// Execute a Dataverse AI Custom Prompt by name or id
    /// </summary>
    public class AIEvaluateFunction : ReflectionFunction
    {
        private readonly ILogger _logger;
        private readonly IOrganizationService _client;
        private readonly DataverseAIPredictHelper _helper;

        private static readonly RecordType _result = RecordType.Empty()
            .Add(new NamedFormulaType("Id", StringType.String))
            .Add(new NamedFormulaType("FinishReason", StringType.String))
            .Add(new NamedFormulaType("Text", StringType.String));

        private static readonly RecordType _parameters = RecordType.Empty();

        public AIEvaluateFunction(ILogger logger, IOrganizationService client, DataverseAIPredictHelper helper) : base(DPath.Root.Append(new DName("Preview")), "AIEvaluate", _result, FormulaType.String, _parameters)
        {
            _logger = logger;
            _client = client;
            _helper = helper;
        }

        /// <summary>
        /// Convert the dynamic parameters of Power Fx record into the format required by Dataverse Predict
        /// </summary>
        /// <param name="parameters">The Power Fx record to convert</param>
        /// <param name="top">If <c>True</c> indicate that this record is a top level record</param>
        /// <returns></returns>
        public ExpandoObject ConvertRecordValueToDictionary(RecordValue parameters, bool top = false)
        {
            ExpandoObject result = new ExpandoObject();
            var dictionary = result as IDictionary<string, object>;

            if ( top )
            {
                // Tell Dataverse the encoding type of tye paramaters
                dictionary.Add("@odata.type", "#Microsoft.Dynamics.CRM.expando");
            }

            foreach (var field in parameters.Fields)
            {
                if (field.Value is RecordValue recordValue)
                {
                    dictionary[field.Name] = ConvertRecordValueToDictionary(recordValue);
                }
                else if (field.Value is TableValue tableValue)
                {
                    var list = new List<dynamic>();
                    foreach (var row in tableValue.Rows)
                    {
                        list.Add(ConvertRecordValueToDictionary(row.Value));
                    }
                    dictionary[field.Name] = list;
                }
                else
                {
                    dictionary[field.Name] = field.Value.ToObject();
                }
            }

            return result;
        }

        /// <summary>
        /// Execute the custom prompt
        /// </summary>
        /// <param name="name">The name or id of the custom model to execute</param>
        /// <param name="parameters">The parameters expected by the custom model</param>
        /// <returns>The finish reason and text</returns>
        /// <exception cref="Exception">If unable to find the model</exception>
        public RecordValue Execute(StringValue name, RecordValue parameters)
        {
            return ExecuteAsync(name, parameters).Result;
        }

        public async Task<RecordValue> ExecuteAsync(StringValue name, RecordValue parameters)
        {
            Guid entityId;
            bool isGuid = Guid.TryParse(name.Value, out entityId);
            GuidValue idValue = GuidValue.New(Guid.Empty);
            Dictionary<string, object> response = null;

            var parameterData = ConvertRecordValueToDictionary(parameters, true);

            if (isGuid)
            {
                // If name is a GUID, make a request with the converted parameters
                response = await _helper.ExecuteRequestAsync(entityId, parameterData);
            }
            else
            {
                // We need to try convert the name of the model into an id to call the Rest API
                var query = new QueryExpression("msdyn_aimodel")
                {
                    ColumnSet = new ColumnSet(true)
                };
                query.Criteria.AddCondition("msdyn_name", ConditionOperator.Equal, name.Value);
                var entities = _client.RetrieveMultiple(query);

                if (entities.Entities.Count > 0)
                {
                    // We found a match
                    var entity = entities.Entities[0];
                    entityId = entity.Id;

                    response = await _helper.ExecuteRequestAsync(entityId, parameterData);
                }
                else
                {
                    throw new Exception($"No entity found with name {name.Value}");
                }
            }

            var finishReasonValue = response["FinishReason"].ToString();
            var textValue = response["Text"].ToString();

            var id = new NamedValue("Id", idValue);
            var finishReason = new NamedValue("FinishReason", FormulaValue.New(finishReasonValue));
            var text = new NamedValue("Text", FormulaValue.New(textValue));

            return RecordValue.NewRecordFromFields(_result, new[] { id, finishReason, text } );
        }
    }
}
