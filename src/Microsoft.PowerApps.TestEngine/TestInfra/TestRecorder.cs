// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.PowerApps.TestEngine.TestInfra
{
    ///<summary>
    /// The TestRecorder class is designed to generate and record test steps for the current test session.
    /// This includes network interaction for Dataverse and Connectors, as well as user interaction via Mouse.
    ///</summary>
    ///<remarks>Future support for Keyboard recording could be considered</remarks>
    public class TestRecorder
    {
        private readonly ILogger _logger;
        private readonly IBrowserContext _browserContext;
        private readonly ITestState _testState;
        private readonly ITestInfraFunctions _infra;
        private readonly IPowerFxEngine _engine;
        private readonly IFileSystem _fileSystem;
        private string _audioPath = string.Empty;

        public ConcurrentBag<string> SetupSteps = new ConcurrentBag<string>();
        public ConcurrentBag<string> TestSteps = new ConcurrentBag<string>();

        ///<summary>
        /// Initializes a new instance of the TestRecorder class.
        ///</summary>
        ///<param name="logger">The logger instance for logging information.</param>
        ///<param name="browserContext">The browser context for Playwright interactions.</param>
        ///<param name="testState">The current test state.</param>
        ///<param name="infra">The infrastructure functions providing access to the current page.</param>
        ///<param name="powerFxEngine">The Power Fx engine representing the current test state of controls, properties, variables, and collections.</param>
        ///<param name="fileSystem">The file system interface for interacting with the file system.</param>
        public TestRecorder(ILogger logger, IBrowserContext browserContext, ITestState testState, ITestInfraFunctions infra, IPowerFxEngine powerFxEngine, IFileSystem fileSystem)
        {
            _logger = logger;
            _browserContext = browserContext;
            _testState = testState;
            _infra = infra;
            _engine = powerFxEngine;
            _fileSystem = fileSystem;
        }

        ///<summary>
        /// Sets up the TestRecorder by subscribing to browser HTTP Requests
        ///</summary>
        public void SetupHttpMonitoring()
        {
            _browserContext.Response += OnResponse;
        }

        public async Task SetupAudioRecording(string audioPath)
        {
            var feedbackHost = new Uri(_testState.GetDomain()).Host;

            _audioPath = audioPath;

            var recordingJavaScript = @"
document.addEventListener('keydown', (event) => {{
if (event.ctrlKey && event.key === 'r') {{
    event.preventDefault();
        // Create a dialog box
        const dialog = document.createElement('div');
        dialog.innerHTML = `
            <style>
                #recordDialog {{ z-index: 100; position: relative }}
                #recordDialog p {{ display: none }}
            </style>
            <div id='recordDialog'>
                <button id='startRecording'>Start</button>
                <button id='stopRecording' disabled >Stop</button>
                <audio id='audioPlayback' controls ></audio>
                <p id= 'feedback' ></p>
                <button id='closeDialog'>Close</button>
            </div>
        `;
        document.body.appendChild(dialog);

        // Get buttons, audio element, and feedback element
        const startButton = document.getElementById('startRecording');
        const stopButton = document.getElementById('stopRecording');
        const audioPlayback = document.getElementById('audioPlayback');
        const feedback = document.getElementById('feedback');
        const closeButton = document.getElementById('closeDialog');

        let mediaRecorder;
        let audioChunks = [];

        // Function to start recording
        startButton.addEventListener('click', async () => {{
            // Request access to the microphone
            const stream = await navigator.mediaDevices.getUserMedia({{ audio: true }});
            mediaRecorder = new MediaRecorder(stream);

            // Start recording
            mediaRecorder.start();
            startButton.disabled = true;
            stopButton.disabled = false;
            feedback.textContent = 'Recording...';

            // Collect audio data
            mediaRecorder.addEventListener('dataavailable', event => {{
                audioChunks.push(event.data);
            }});

            document.TestEngineAudioSessionId = 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {{
                    const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
                    return v.toString(16);
            }});

            fetch('https://{0}/testengine/audio/start', {{
                method: 'POST',
                body: JSON.stringify({{ 'startDateTime': new Date().toISOString(), 'audioSessionId': document.TestEngineAudioSessionId  }}),
                headers: {{
                    'Content-Type': 'application/json'
                }}
            }});

            // When recording stops, create an audio file
            mediaRecorder.addEventListener('stop', () => {{
                const audioBlob = new Blob(audioChunks, {{ type: 'audio/wav' }});
                const audioUrl = URL.createObjectURL(audioBlob);
                audioPlayback.src = audioUrl;

                // Post the recorded audio to an API
                fetch('https://{0}/testengine/audio/upload', {{
                    method: 'POST',
                    body: audioBlob,
                    headers: {{
                        'Content-Type': 'audio/wav',
                        'endDateTime': new Date().toISOString(),
                        'audioSessionId': document.TestEngineAudioSessionId
                    }}
                }}).then(response => {{
                    if (response.ok) {{
                        feedback.textContent = 'Audio uploaded successfully!';
                    }} else {{
                        feedback.textContent = 'Failed to upload audio.';
                    }}
                }}).catch(error => {{
                    feedback.textContent = 'Error uploading audio: ' + error;
                }});
            }});
        }});

        // Function to stop recording
        stopButton.addEventListener('click', () => {{
            mediaRecorder.stop();
            startButton.disabled = false;
            stopButton.disabled = true;
            feedback.textContent = 'Recording stopped. Uploading audio...';
        }});

        // Function to close the dialog
        closeButton.addEventListener('click', () => {{
            document.body.removeChild(dialog);
        }});
    }}
}});
";

            await _infra.Page.EvaluateAsync(string.Format(recordingJavaScript, feedbackHost));

            // Add recording if page is reloaded
            _infra.Page.Load += async (object sender, IPage e) =>
            {
                await _infra.Page.EvaluateAsync(string.Format(recordingJavaScript, feedbackHost));
            };
        }

        ///<summary>
        /// Sets up the TestRecorder by subscribing to page mouse events.
        ///</summary>
        public void SetupMouseMonitoring()
        {
            var page = _infra.Page;

            var feedbackUrl = new Uri(new Uri($"https://{new Uri(_testState.GetDomain()).Host}"), new Uri("testengine", UriKind.Relative));

            AddClickListener(_infra.Page, feedbackUrl).Wait();

            // Add handler to listen if page reloaded to add the mouse monitoring
            _infra.Page.Load += (object sender, IPage e) =>
            {
                AddClickListener(_infra.Page, feedbackUrl).Wait();
            };

            //TODO: Subscribe to keyboard events from the page. This will need to consider focus changes and how get value for SetProperty() based on control type
        }

        ///<summary>
        /// Sets up the TestRecorder API
        ///</summary>
        public void RegisterTestEngineApi()
        {
            var feedbackUrl = new Uri(new Uri($"https://{new Uri(_testState.GetDomain()).Host}"), new Uri("testengine", UriKind.Relative));

            // Intercept ALL calls for testengine feedback for recording
            _browserContext.RouteAsync($"{feedbackUrl.ToString()}/**", (IRoute route) => HandleTestEngineData(route));
        }

        /// <summary>
        /// Handle callback from HTTP request sent from browser to test enging
        /// </summary>
        /// <param name="route">The request that has been intercepted</param>
        /// <returns>New response to the browser</returns>
        private async Task HandleTestEngineData(IRoute route)
        {
            if (route.Request.Url.Contains("/audio/start") && route.Request.Method == "POST")
            {
                // Read the posted file
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                };
                var audioContext = JsonConvert.DeserializeObject<Dictionary<string, object>>(route.Request.PostData, settings);

                var started = String.Empty;
                var audioId = String.Empty;

                if (audioContext.ContainsKey("startDateTime") && audioContext["startDateTime"] is DateTime startDateTime)
                {
                    started = startDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                if (audioContext.ContainsKey("audioSessionId"))
                {
                    audioId = audioContext["audioSessionId"].ToString();
                }

                TestSteps.Add($"// Audio started - {started} - {audioId}");
            }

            if (route.Request.Url.Contains("/audio/upload"))
            {
                var headers = route.Request.Headers;
                var ended = String.Empty;
                var audioId = String.Empty;
                if (headers.ContainsKey("endDateTime".ToLower()))
                {
                    ended = headers["endDateTime".ToLower()];
                }

                if (headers.ContainsKey("audioSessionId".ToLower()))
                {
                    audioId = headers["audioSessionId".ToLower()];
                }

                TestSteps.Add($"// Audio end - {ended} - {audioId}");

                // Read the posted file

                var audioFile = route.Request.PostDataBuffer;


                if (!_fileSystem.Exists(_audioPath))
                {
                    _fileSystem.CreateDirectory(_audioPath);
                }

                _fileSystem.WriteFile(Path.Combine(_audioPath, $"recording_{DateTime.Now.ToString("yyyyHHmmss")}.wav"), audioFile);
            }

            if (route.Request.Url.Contains("/click"))
            {
                // TODO: handle click for known controls
                // TODO: handle click for known like combobox (Or use Keyboard shortcuts to handle differences?
                // TODO: handle click for known controls inside gallery or components
                // TODO: handle click for controls inside PCF using css selector using Experimental.PlaywrightAction()?
                var segments = new Uri(route.Request.Url).AbsolutePath.Split('/');
                if (segments.Length >= 4
                    && segments[1].Equals("testengine", StringComparison.OrdinalIgnoreCase)
                    && segments[2].Equals("click", StringComparison.OrdinalIgnoreCase))
                {
                    var controlName = segments[3];
                    _logger.LogDebug($"Click {controlName}");

                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(route.Request.PostData);

                    var text = data.ContainsKey("text") && !String.IsNullOrEmpty(data["text"].ToString()) ? data["text"].ToString() : "";
                    var alt = false;
                    var control = false;

                    if (data.ContainsKey("alt") && bool.TryParse(data["alt"].ToString(), out bool altValue))
                    {
                        alt = altValue;
                    }

                    if (data.ContainsKey("control") && bool.TryParse(data["control"].ToString(), out bool controlValue))
                    {
                        control = controlValue;
                    }

                    // TODO: Refactor read Power Fx Template provided for recording session and evaluate templates from the Recording Test Suite
                    // This will need to consider alt, control values

                    // TODO: Consider control names and if need to apply Power Fx [] delimiter
                    if (alt)
                    {
                        // TODO: Handle single quote in the text
                        TestSteps.Add($"Experimental.PlaywrightAction(\"[data-test-id='{controlName}']:has-text('{text}')\", \"wait\");");
                    }
                    else if (control)
                    {
                        TestSteps.Add($"Experimental.WaitUntil({controlName}.Text=\"{text}\");");
                    }
                    else
                    {
                        // Assume that the select item is compatible with Select() Power Fx function
                        TestSteps.Add($"Select({controlName});");
                    }
                }
            }

            // Always send back Status 200 and do not send information to target URL as the request is for recording only
            await route.FulfillAsync(new RouteFulfillOptions { Status = 200 });
        }

        /// <summary>
        /// Listen for clicks on the active page document
        /// </summary>
        /// <param name="page">The page to listen for click events</param>
        /// <param name="feedbackUrl">The url to send click summarized event data so testenginge can generate test steps</param>
        /// <returns>Completed task</returns>
        private async Task AddClickListener(IPage page, Uri feedbackUrl)
        {
            // TODO: Handle controls that do not have data-control-name
            string listenerJavaScript = String.Format(@"(function() {{
        document.addEventListener('click', function(event) {{
            const element = event.target.closest('[data-control-name]');
            if (element) {{
                const controlName = element.getAttribute('data-control-name');
                const clickData = {{
                    controlName: controlName,
                    x: event.clientX,
                    y: event.clientY,
                    text: element.textContent.trim(),
                    alt: (event.altKey),
                    control: (event.ctrlKey)
                }};
                fetch('{0}/click/' + controlName, {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json'
                    }},
                    body: JSON.stringify(clickData)
                }});
            }}
        }});
    }})();", feedbackUrl);
            await page.EvaluateAsync(listenerJavaScript);
        }

        /// <summary>
        /// Handle response to HTTP page that is sent starting work in a new thread not to block execution
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResponse(object sender, IResponse e)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await HandleResponse(e);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (sender is List<Task>)
            {
                var tasks = sender as List<Task>;
                tasks.Add(tcs.Task);
            }
        }

        /// <summary>
        /// Check for responses that need to have handling for Recording test step generation
        /// </summary>
        /// <param name="response">The response to check for matching request/reponse to generate a TestStep</param>
        /// <returns>Completed task</returns>
        private async Task HandleResponse(IResponse response)
        {
            // Check of the request related to a Dataverse connection
            if (response.Request.Url.Contains("/api/data/v"))
            {
                switch (response.Request.Method)
                {
                    case "GET":
                        var entity = GetODataEntity(response.Request.Url);
                        var data = await ConvertODataToFormulaValue(response);
                        // TODO: Check for $filter and convert OData $filter to Filter record and Power Fx expression
                        SetupSteps.Add(GenerateDataverseQuery(entity, data));
                        break;
                    case "POST":
                        // TODO Handle create
                        break;
                }
            }

            // Check for Power Platform connector invocation
            if (response.Request.Url.Contains("/invoke") && response.Request.Headers.ContainsKey("x-ms-request-url"))
            {
                switch (response.Request.Method)
                {
                    case "POST":
                        var action = GetActionName(response.Request.Headers["x-ms-request-url"]);
                        var when = GetWhenConnectorValue(response.Request.Headers["x-ms-request-url"]);
                        var then = await ConvertJsonResultToFormulaValue(response);
                        SetupSteps.Add(GenerateConnector(action, when, then));
                        break;
                }
            }
        }

        /// <summary>
        /// Convert data extracted from HTTP request into Expertimental.SimulateConnection() call
        /// </summary>
        /// <param name="name">The connector that the simulation relates to</param>
        /// <param name="when">Paremeters determining when the simulation should apply</param>
        /// <param name="then">The table or record to return when a match is found</param>
        /// <returns>Generated Power Fx function</returns>
        private string GenerateConnector(string name, FormulaValue when, FormulaValue then)
        {
            StringBuilder connectorBuilder = new StringBuilder();

            connectorBuilder.Append($"Experimental.SimulateConnector({{Name: \"{name}\"");

            if (when is RecordValue whenRecord)
            {
                connectorBuilder.Append(", When: ");
                connectorBuilder.Append("{");
                foreach (var field in whenRecord.Fields)
                {
                    connectorBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                }
                connectorBuilder.Length -= 2; // Remove the trailing comma and space
                connectorBuilder.Append("}, ");
            }
            else
            {
                connectorBuilder.Append(", ");
            }

            if (then is BlankValue blankThenTable)
            {
                connectorBuilder.Append("Then: Blank()");
            }

            if (then is TableValue thenTable)
            {
                connectorBuilder.Append("Then: ");
                connectorBuilder.Append("Table(");

                var rowAdded = false;

                foreach (var record in thenTable.Rows)
                {
                    var recordValue = record.Value as RecordValue;

                    if (recordValue != null)
                    {
                        rowAdded = true;
                        connectorBuilder.Append("{");
                        foreach (var field in recordValue.Fields)
                        {
                            connectorBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                        }
                        connectorBuilder.Length -= 2; // Remove the trailing comma and space
                        connectorBuilder.Append("}, ");
                    }
                }

                if (rowAdded)
                {
                    connectorBuilder.Length -= 2; // Remove the trailing comma and space
                }

                connectorBuilder.Append(")"); // Close the table
            }

            if (then is RecordValue thenRecord)
            {
                connectorBuilder.Append("Then: ");
                if (thenRecord.Fields.Count() == 0)
                {
                    connectorBuilder.Append("Blank()");
                }
                else
                {
                    connectorBuilder.Append("{");
                    foreach (var field in thenRecord.Fields)
                    {
                        var formattedFieldValue = FormatValue(field.Value);
                        connectorBuilder.Append($"{field.Name}: {formattedFieldValue}, ");
                    }
                    connectorBuilder.Length -= 2; // Remove the trailing comma and space
                    connectorBuilder.Append("}");
                }
            }

            connectorBuilder.Append("});"); // Close record argument and the SimulateConnector function

            return connectorBuilder.ToString();
        }

        /// <summary>
        /// Extract the Connector action from the url
        /// </summary>
        /// <param name="url">The relative connector url reference</param>
        /// <returns>The action name</returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetActionName(string url)
        {
            var requestUrl = new Uri(new Uri("https://example.com"), new Uri(url, UriKind.Relative));

            var segments = requestUrl.AbsolutePath.Split('/');

            // Assuming the entity name is the last segment in the URL and using format /api/data/v9.X/entityname
            // The first segment will be empty as has leading /
            if (segments.Length >= 3 && segments[1].Equals("apim", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Handle case where requesting connector name vs list using /apim/name/**
                return segments[2];
            }

            throw new ArgumentException("Invalid request url");
        }


        /// <summary>
        /// Convert the requested action url into Power Fx When record
        /// </summary>
        /// <param name="url">Teh url to be converted</param>
        /// <returns>The When record that represents the request</returns>
        private FormulaValue GetWhenConnectorValue(string url)
        {
            var requestUrl = new Uri(new Uri("https://example.com"), new Uri(url, UriKind.Relative));

            var segments = requestUrl.AbsolutePath.Split('/');

            List<NamedValue> fields = new List<NamedValue>();


            var action = String.Empty;

            // Assuming the entity name is the last segment in the URL and using format /api/data/v9.X/entityname
            // The first segment will be empty as has leading /
            if (segments.Length > 4)
            {
                // TODO: Handle case where requesting connector name vs list using /apim/name/**
                var parts = new List<string>(segments);
                parts.RemoveAt(0); // Remove empty item
                parts.RemoveAt(0); // Remove apim
                parts.RemoveAt(0); // Remove connector name
                parts.RemoveAt(0); // Remove connector id

                // Assume the reminaing item is the action
                fields.Add(new NamedValue("Action", FormulaValue.New(string.Join("/", parts))));
            }

            if (!string.IsNullOrEmpty(requestUrl.Query))
            {
                var items = HttpUtility.ParseQueryString(HttpUtility.UrlDecode(requestUrl.Query));
                foreach (var key in items.AllKeys)
                {
                    switch (key.ToLower())
                    {
                        case "$filter":
                            string powerFxExpression = ConvertODataToPowerFx(items[key]);
                            fields.Add(new NamedValue("Filter", FormulaValue.New(powerFxExpression)));
                            break;
                    }
                }
            }

            if (fields.Count() == 0)
            {
                return RecordValue.NewBlank();
            }

            return RecordValue.NewRecordFromFields(fields);
        }

        /// <summary>
        /// Convert a OData $filter to a Power Fx string expression
        /// </summary>
        /// <param name="odataFilter">The filter to be converted</param>
        /// <returns>Power Fx expression that represents the $filter</returns>
        string ConvertODataToPowerFx(string odataFilter)
        {
            // Parse the OData filter without a known EDM model
            var filterClause = ParseFilter(odataFilter);

            // Convert the filter clause to Power Fx expression
            return ConvertFilterClauseToPowerFx(filterClause.Expression);
        }

        /// <summary>
        /// Parse the odata filter clause and return the Abstract Syntax Tree (AST) representation of the expression
        /// </summary>
        /// <param name="odataFilter">The text $filter clause</param>
        /// <returns>The AST representation of the filter clause</returns>
        private FilterClause ParseFilter(string odataFilter)
        {
            EdmModel edmModel = new EdmModel();
            // Define the entity type and set up the EDM model
            EdmEntityType entityType = new EdmEntityType("Namespace", "EntityName", null, false, true);
            edmModel.AddElement(entityType);

            // Parse the filter
            return ODataUriParser.ParseFilter(odataFilter, edmModel, entityType);
        }

        /// <summary>
        /// Convert the AST representation of a OData filter clause to the equivent Power Fx expression
        /// </summary>
        /// <param name="expression">An element of the OData AST tree convert</param>
        /// <returns>Power Fx representation of the AST fragement</returns>
        /// <exception cref="NotSupportedException"></exception>
        string ConvertFilterClauseToPowerFx(SingleValueNode expression)
        {
            if (expression is BinaryOperatorNode binaryOperatorNode)
            {
                string left = ConvertFilterClauseToPowerFx(binaryOperatorNode.Left);
                string right = ConvertFilterClauseToPowerFx(binaryOperatorNode.Right);
                string operatorString = binaryOperatorNode.OperatorKind switch
                {
                    BinaryOperatorKind.Equal => "=",
                    BinaryOperatorKind.GreaterThan => ">",
                    BinaryOperatorKind.GreaterThanOrEqual => ">=",
                    BinaryOperatorKind.LessThan => "<",
                    BinaryOperatorKind.LessThanOrEqual => "<=",
                    BinaryOperatorKind.NotEqual => "!=",
                    BinaryOperatorKind.Multiply => "*",
                    BinaryOperatorKind.Divide => "/",
                    BinaryOperatorKind.Modulo => "MOD(",
                    BinaryOperatorKind.And => "AND(",
                    BinaryOperatorKind.Or => "OR(",
                    _ => throw new NotSupportedException($"Operator {binaryOperatorNode.OperatorKind} is not supported")
                };
                if (operatorString.Contains("("))
                {
                    // It is a function
                    return $"{operatorString}{left},{right})";
                }
                else
                {
                    return $"{left} {operatorString} {right}";
                }

            }
            else if (expression is UnaryOperatorNode unaryOperatorNode)
            {
                string operand = ConvertFilterClauseToPowerFx(unaryOperatorNode.Operand);
                return $"NOT({operand})";
            }
            else if (expression is SingleValuePropertyAccessNode propertyAccessNode)
            {
                return propertyAccessNode.Property.Name;
            }
            else if (expression is SingleValueOpenPropertyAccessNode openPropertyAccessNode)
            {
                return openPropertyAccessNode.Name;
            }
            else if (expression is ConstantNode constantNode)
            {
                if (constantNode.Value is string stringValue)
                {
                    // Need to add two quotes as it will be included in a string
                    return $"\"\"{stringValue}\"\"";
                }
                return constantNode.Value.ToString();
            }
            if (expression is ConvertNode convertNode)
            {
                return ConvertFilterClauseToPowerFx(convertNode.Source);
            }

            throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported");
        }

        /// <summary>
        /// Generate a Power Fx Experimental.SimulateDataverse() from extracted HTTP request data
        /// </summary>
        /// <param name="entity">The entity that the request relates to</param>
        /// <param name="data">The optional data to convert</param>
        /// <returns></returns>
        private string GenerateDataverseQuery(string entity, FormulaValue data)
        {
            StringBuilder queryBuilder = new StringBuilder();

            queryBuilder.Append($"Experimental.SimulateDataverse({{Action: \"Query\", Entity: \"{entity}\", Then: ");

            if (data is TableValue tableValue)
            {
                queryBuilder.Append($"Table(");

                var rowAdded = false;

                foreach (var record in tableValue.Rows)
                {
                    var recordValue = record.Value as RecordValue;

                    if (recordValue != null)
                    {
                        rowAdded = true;
                        queryBuilder.Append("{");
                        foreach (var field in recordValue.Fields)
                        {
                            queryBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                        }
                        queryBuilder.Length -= 2; // Remove the trailing comma and space
                        queryBuilder.Append("}, ");
                    }
                }

                if (rowAdded)
                {
                    queryBuilder.Length -= 2; // Remove the trailing comma and space
                }

                queryBuilder.Append(")"); // Close the table
            }
            else if (data is RecordValue recordValue)
            {
                queryBuilder.Append("{");
                foreach (var field in recordValue.Fields)
                {
                    queryBuilder.Append($"{field.Name}: {FormatValue(field.Value)}, ");
                }
                queryBuilder.Length -= 2; // Remove the trailing comma and space
                queryBuilder.Append("}");
            }
            else
            {
                queryBuilder.Append(FormatValue(data));
            }

            queryBuilder.Append("});"); // Close record argument and the SimulateDataverse dataverse function

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Convert Power Fx formula value to the string representation
        /// </summary>
        /// <param name="value">The vaue to convert</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string FormatValue(FormulaValue value)
        {
            //TODO: Handle special case of DateTime As unix time to DateTime
            return value switch
            {
                BlankValue blankValue => "Blank()",
                StringValue stringValue => $"\"{stringValue.Value}\"",
                NumberValue numberValue => numberValue.Value.ToString(),
                BooleanValue booleanValue => booleanValue.Value.ToString().ToLower(),
                // Assume all dates should be in UTC
                DateValue dateValue => "\"" + dateValue.GetConvertedValue(TimeZoneInfo.Utc).ToString("o") + "\"" , // ISO 8601 format
                DateTimeValue dateTimeValue => "\"" + dateTimeValue.GetConvertedValue(TimeZoneInfo.Utc).ToString("o") + "\"", // ISO 8601 format
                RecordValue recordValue => FormatRecordValue(recordValue),
                TableValue tableValue => FormatTableValue(tableValue),
                _ => throw new ArgumentException("Unsupported FormulaValue type")
            };
        }

        /// <summary>
        /// Convert a Power Fx object to String Representation of the Record
        /// </summary>
        /// <param name="recordValue">The record to be converted</param>
        /// <returns>Power Fx representation</returns>
        private string FormatRecordValue(RecordValue recordValue)
        {
            var fields = recordValue.Fields.Select(field => $"{field.Name}: {FormatValue(field.Value)}");
            return $"{{{string.Join(", ", fields)}}}";
        }

        /// <summary>
        /// Convert the Power Fx table into string representation
        /// </summary>
        /// <param name="tableValue">The table to be converted</param>
        /// <returns>The string representation of all rows of the table</returns>
        private string FormatTableValue(TableValue tableValue)
        {
            var rows = tableValue.Rows.Select(row => FormatValue(row.Value));
            return $"Table({string.Join(", ", rows)})";
        }

        /// <summary>
        /// Convert OData response to Power Fx Value
        /// </summary>
        /// <param name="response">The HTTP reponse to read Json response from</param>
        /// <returns></returns>
        private async Task<FormulaValue> ConvertODataToFormulaValue(IResponse response)
        {
            // Read the JSON content from the response
            var jsonString = await response.JsonAsync();
            var json = jsonString.ToString();
            var jsonObject = JObject.Parse(json);

            if (jsonObject.ContainsKey("value"))
            {
                return await ConvertJsonToFormulaValue(jsonObject["value"]);
            }
            return await ConvertJsonToFormulaValue(jsonObject);
        }

        /// <summary>
        /// Convert the Json body of the reponse to Power Fx formula
        /// </summary>
        /// <param name="response">The HTTP reponse to read Json response from</param>
        /// <returns>The mapped formula value</returns>
        private async Task<FormulaValue> ConvertJsonResultToFormulaValue(IResponse response)
        {
            // Read the JSON content from the response
            var jsonString = await response.JsonAsync();
            JToken jsonObject = IsJsonElementArray(jsonString) ? JArray.Parse(jsonString.ToString()) : JObject.Parse(jsonString.ToString());

            return await ConvertJsonToFormulaValue(jsonObject.Root);
        }

        public bool IsJsonElementArray(JsonElement? element)
        {
            return element?.ValueKind == JsonValueKind.Array;
        }

        /// <summary>
        /// Convert Json object to Power Fx formula value
        /// </summary>
        /// <param name="jsonObject">JObject, JArray or JValue token to convert</param>
        /// <returns>The mapped Power Fx formula</returns>
        private async Task<FormulaValue> ConvertJsonToFormulaValue(JToken jsonObject)
        {
            // Check if the value parameter is an array
            if (jsonObject is JArray jsonArray)
            {
                // Create a list of RecordValue to hold the attributes of each object
                var records = new List<RecordValue>();

                // Use empty type as each record might have different values
                RecordType recordType = RecordType.Empty();

                foreach (var item in jsonArray)
                {
                    var fields = new List<NamedValue>();

                    foreach (var property in item.Children<JProperty>())
                    {
                        var fieldValue = await ConvertJsonToFormulaValue(property.Value);
                        fields.Add(new NamedValue(property.Name, fieldValue));
                        recordType = recordType.Add(new NamedFormulaType(property.Name, fieldValue.Type));
                    }

                    records.Add(RecordValue.NewRecordFromFields(fields));
                }

                // Convert the list of RecordValue to a TableValue with the generated recordType
                return TableValue.NewTable(recordType, records);
            }
            // Check if the value parameter is an object
            else if (jsonObject is JObject jsonObjectValue)
            {
                var fields = new List<NamedValue>();
                RecordType recordType = RecordType.Empty();

                foreach (var property in jsonObjectValue.Children<JProperty>())
                {
                    var name = property.Name;
                    FormulaValue value = null;

                    if (property.Value is JObject || property.Value is JArray)
                    {
                        value = await ConvertJsonToFormulaValue(property.Value);

                    }
                    else if (property.Value is JValue)
                    {
                        var propertyValue = ((JValue)property.Value).Value;
                        if (propertyValue is string stringValue)
                        {
                            value = FormulaValue.New(stringValue);
                        }
                        else if (propertyValue is int intValue)
                        {
                            value = FormulaValue.New(intValue);
                        }
                        else if (propertyValue is double doubleValue)
                        {
                            value = FormulaValue.New(doubleValue);
                        }
                        else if (propertyValue is bool boolValue)
                        {
                            value = FormulaValue.New(boolValue);
                        }
                        else if (propertyValue is DateTime dateTimeValue)
                        {
                            value = FormulaValue.New(dateTimeValue);
                        }
                        else if (propertyValue == null)
                        {
                            value = FormulaValue.NewBlank();
                        }
                    }
                    else
                    {
                        _logger.LogDebug("The property parameter is not not supported");
                    }

                    if (value == null && property.Value != null)
                    {
                        // TODO: Improve unknown value mapping
                        value = FormulaValue.New(property.Value.ToString());
                    }

                    if (value == null)
                    {
                        // Lets just map to blank
                        value = BlankValue.NewBlank();
                    }

                    fields.Add(new NamedValue(name, value));

                    recordType = recordType.Add(new NamedFormulaType(property.Name, value.Type));
                }

                // Convert the object to a RecordValue with the generated recordType
                return RecordValue.NewRecordFromFields(recordType, fields);
            }
            // Check if the value parameter is a scalar value
            else if (jsonObject != null)
            {
                // Convert the scalar value to a FormulaValue
                return FormulaValue.New(jsonObject.ToString());
            }


            _logger.LogDebug("The value parameter is not a valid JSON type");
            return FormulaValue.NewBlank();
        }

        /// <summary>
        /// Extract the oadata entity from the url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetODataEntity(string url)
        {
            var requestUrl = new Uri(url);
            var segments = requestUrl.AbsolutePath.Split('/');

            // Assuming the entity name is the last segment in the URL and using format /api/data/v9.X/entityname
            // The first segment will be empty as has leading /
            if (segments.Length >= 5 && segments[1].Equals("api", StringComparison.OrdinalIgnoreCase) &&
                segments[2].Equals("data", StringComparison.OrdinalIgnoreCase) && segments[3].StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Handle case where requesting entity vs list using /api/data/v9.X/entityname(id) syntax
                return segments[4];
            }

            throw new ArgumentException("Invalid OData URL format");
        }

        ///<summary>
        /// Generates test steps and data, and saves them to the specified path.
        ///</summary>
        ///<param name="path">The path where the test steps and data will be saved.</param>
        public async void Generate(string path)
        {
            if (!_fileSystem.Exists(path))
            {
                _fileSystem.CreateDirectory(path);
            }

            string filePath = $"{path}/recorded.te.yaml";

            var line = 0;

            var exists = new List<string>();

            StringBuilder setup = new StringBuilder();
            while (!SetupSteps.IsEmpty)
            {
                if (SetupSteps.TryTake(out string item))
                {
                    line++;
                    var spaces = String.Empty;
                    var add = !exists.Contains(item);
                    if (add)
                    {
                        exists.Add(item);
                        if (line > 1)
                        {
                            spaces = new string(' ', 8);
                        }
                        setup.Append($"{spaces}{item}\r\n");
                    }
                }
            }

            // Transfer elements to a ConcurrentQueue
            ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            while (!TestSteps.IsEmpty)
            {
                if (TestSteps.TryTake(out string item))
                {
                    queue.Enqueue(item);
                }
            }

            StringBuilder steps = new StringBuilder();

            line = 0;
        
            // Enumberate in First In First Out (FIFO)
            foreach (var step in queue)
            {
                line++;
                var spaces = String.Empty;
                if (line > 1)
                {
                    spaces = new string(' ', 8);
                }
                steps.Append($"{spaces}{step}\r\n");
            }

            var template = @"# yaml-embedded-languages: powerfx
testSuite:
  testSuiteName: Recorded test suite
  testSuiteDescription: Summary of what the test suite
  persona: User1
  appLogicalName: NotNeeded
  onTestSuiteBegin: |
    =
    {0}
  
  testCases:
    - testCaseName: Recorded test cases
      testCaseDescription: Set of test steps recorded from browser
      testSteps: |
        =
        {1}

testSettings:
  headless: false
  locale: ""en-US""
  recordVideo: true
  extensionModules:
    enable: true
  browserConfigurations:
    - browser: Chromium

environmentVariables:
  users:
    - personaName: User1
      emailKey: user1Email
      passwordKey: NotNeeded
";

            var results = string.Format(template, setup.ToString(), steps.ToString());

            //TODO: Write the recorded test steps to the file
            _fileSystem.WriteTextToFile(filePath, results);
        }
    }
}
