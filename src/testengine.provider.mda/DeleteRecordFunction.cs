using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;
using System.Threading.Tasks;

namespace testengine.provider.mda
{
    /// <summary>
    /// This will wait for the current record to be deleted.
    /// </summary>
    public class DeleteRecordFunction : ReflectionFunction
    {
        private readonly ITestInfraFunctions _testInfraFunctions;
        private readonly ITestState? _testState;
        private readonly ILogger _logger;

        public DeleteRecordFunction(ITestInfraFunctions testInfraFunctions, ITestState? testState, ILogger logger)
            : base(DPath.Root.Append(new DName("Preview")), "DeleteRecord", BooleanType.Boolean)
        {
            _testInfraFunctions = testInfraFunctions;
            _testState = testState;
            _logger = logger;
        }

        /// <summary>
        /// Attempt to delete the current record
        /// </summary>
        /// <returns><c>True</c> if record successfully deleted.</returns>
        public BooleanValue Execute()
        {
            _logger.LogInformation("Starting Delete Record");
            return ExecuteAsync().Result;
        }

        public async Task<BooleanValue> ExecuteAsync()
        {

            await _testInfraFunctions.RunJavascriptAsync<bool>(
                @"window.deleteCompleted = null;
                  var entityName = Xrm.Page.data.entity.getEntityName && Xrm.Page.data.entity.getEntityName();
                  var entityId = Xrm.Page.data.entity.getId && Xrm.Page.data.entity.getId();
                  if (entityName && entityId) {
                      Xrm.WebApi.deleteRecord(entityName, entityId.replace(/[{}]/g, ''))
                          .then(function() { window.deleteCompleted = true; })
                          .catch(function() { window.deleteCompleted = false; });
                  } else {
                      window.deleteCompleted = false;
                  }"
            );


            var getValue = () => _testInfraFunctions.RunJavascriptAsync<object>("window.deleteCompleted").Result;

            var result = PollingHelper.Poll<object>(
                null,
                x => x == null,
                getValue,
                _testState != null ? 3000 : _testState.GetTimeout(),
                _logger,
                "Unable to complete delete"
            );

            if (result is bool value)
            {
                return BooleanValue.New(value);
            }

            return BooleanValue.New(false);
        }
    }
}
