// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using Microsoft.Xrm.Sdk;

namespace Microsoft.PowerApps.TestEngine.PowerFx.Functions
{
    /// <summary>
    /// Execute a 
    /// </summary>
    public class AIPredictFunction : ReflectionFunction
    {
        private readonly ILogger _logger;
        private readonly IOrganizationService _client;
        private static readonly RecordType _result = RecordType.Empty()
            .Add(new NamedFormulaType("Id", StringType.String))
            .Add(new NamedFormulaType("Result", StringType.String));

        public AIPredictFunction(ILogger logger, IOrganizationService client) : base(DPath.Root.Append(new DName("Preview")), "AIPredict", _result, FormulaType.String, FormulaType.String)
        {
            _logger = logger;
            _client = client;
        }

        public RecordValue Execute(StringValue name, StringValue json)
        {
            var id = new NamedValue("Id", FormulaValue.NewBlank());
            var result = new NamedValue("Id", FormulaValue.NewBlank());

            return RecordValue.NewRecordFromFields(_result, new[] { id, result } );
        }
    }
}
