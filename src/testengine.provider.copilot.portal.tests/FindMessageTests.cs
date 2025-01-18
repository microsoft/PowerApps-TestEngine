﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.TestInfra;
using Microsoft.PowerFx.Types;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Tests.CopilotPortal.Tests
{
    public class FindMessageTests
    {
        [Theory]
        [InlineData(1, "{\"type\":\"message\"}", "{\"type\": \"message\", \"id\": \"68b79d56-6f82-445c-a595-5fd1120b5111\", \"timestamp\": \"2025-01-18T17:01:53.5993131+00:00\", \"channelid\": \"pva-studio\", \"from\": {\"id\": \"d2b4befb-8eca-e9f6-b04c-9edadb372e12/12e0ac1f-82d4-ef11-a730-000d3a35c98d\", \"name\": \"crd53_safeTravels\", \"role\": \"bot\"}, \"conversation\": {\"id\": \"e9faf6e5-bff2-4241-ae0f-f548edbf2683\"}, \"recipient\": {\"id\": \"e673cd5a-312f-4d1c-8e20-28da2221c9b0\", \"aadobjectid\": \"e673cd5a-312f-4d1c-8e20-28da2221c9b0\", \"role\": \"user\"}, \"textformat\": \"markdown\", \"membersadded\": [], \"membersremoved\": [], \"reactionsadded\": [], \"reactionsremoved\": [], \"locale\": \"en-US\", \"text\": \"Based on the available information, El Salvador has been updated to a Level 2 advisory, indicating that you should exercise increased caution due to crime, but there has been a significant reduction in gang-related activity and associated crime in the last two years . Turkey, on the other hand, requires caution in various public places due to security concerns . It is advisable to avoid high-risk areas with a Level 4 travel advisory due to extreme risks and limited assistance .\\n\\n: https://travel.state.gov/content/travel/en/traveladvisories/traveladvisories/el-salvador-travel-advisory.html \\\"El Salvador Travel Advisory\\\"\\n: https://travel.state.gov/content/travel/en/traveladvisories/traveladvisories/turkey-travel-advisory.html \\\"Turkey Travel Advisory\\\"\\n: https://travel.state.gov/content/travel/en/international-travel/before-you-go/travelers-with-special-considerations/high-risk-travelers.html \\\"Travel to High-Risk Areas\\\"\", \"speak\": \"<speak version=\\\"1.0\\\" xml:lang=\\\"en-US\\\" xmlns:mstts=\\\"http://www.w3.org/2001/mstts\\\" xmlns=\\\"http://www.w3.org/2001/10/synthesis\\\"><voice name=\\\"en-US-ChristopherNeural\\\" xmlns=\\\"\\\"><prosody rate=\\\"0%\\\" pitch=\\\"0%\\\">Based on the available information, El Salvador has been updated to a Level 2 advisory, indicating that you should exercise increased caution due to crime, but there has been a significant reduction in gang-related activity and associated crime in the last two years 1. Turkey, on the other hand, requires caution in various public places due to security concerns 2. It is advisable to avoid high-risk areas with a Level 4 travel advisory due to extreme risks and limited assistance 3.</prosody></voice></speak>\", \"inputhint\": \"acceptingInput\", \"attachments\": [], \"entities\": [{\"@type\": [\"AIGeneratedContent\"], \"@context\": [\"https://schema.org\"], \"@id\":\"https://travel.state.gov/content/travel/en/traveladvisories/traveladvisories/el-salvador-travel-advisory.html\", \"@type\":\"Claim\", \"@context\":\"https://schema.org\", \"@type\":\"Claim\", \"@context\":\"https://schema.org\", \"@type\":\"Claim\", \"@context\":\"https://schema.org\"}]}")]
        [InlineData(1, "{\"type\":\"message\"}", "{\"type\": \"message\", \"id\": null, \"timestamp\": null, \"channelid\": null, \"from\": null, \"conversation\": null, \"recipient\": null, \"textformat\": null, \"membersadded\": null, \"membersremoved\": null, \"reactionsadded\": null, \"reactionsremoved\": null, \"locale\": null, \"text\": null, \"speak\": null, \"inputhint\": null, \"attachments\": null, \"entities\": null}")]
        [InlineData(0, "{\"type\":\"message\"}", "{\"type\": null, \"id\": null, \"timestamp\": null, \"channelid\": null, \"from\": {\"id\": null, \"name\": null, \"role\": null}, \"conversation\": {\"id\": null}, \"recipient\": {\"id\": null, \"aadobjectid\": null, \"role\": null}, \"textformat\": null, \"membersadded\": [], \"membersremoved\": [], \"reactionsadded\": [], \"reactionsremoved\": [], \"locale\": null, \"text\": null, \"speak\": null, \"inputhint\": null, \"attachments\": [], \"entities\": []}")]
        [InlineData(1, "{}", "{\"type\": null, \"id\": null, \"timestamp\": null, \"channelid\": null, \"from\": {\"id\": null, \"name\": null, \"role\": null}, \"conversation\": {\"id\": null}, \"recipient\": {\"id\": null, \"aadobjectid\": null, \"role\": null}, \"textformat\": null, \"membersadded\": [], \"membersremoved\": [], \"reactionsadded\": [], \"reactionsremoved\": [], \"locale\": null, \"text\": null, \"speak\": null, \"inputhint\": null, \"attachments\": [], \"entities\": []}")]
        [InlineData(1, "null", "{\"type\": null, \"id\": null, \"timestamp\": null, \"channelid\": null, \"from\": {\"id\": null, \"name\": null, \"role\": null}, \"conversation\": {\"id\": null}, \"recipient\": {\"id\": null, \"aadobjectid\": null, \"role\": null}, \"textformat\": null, \"membersadded\": [], \"membersremoved\": [], \"reactionsadded\": [], \"reactionsremoved\": [], \"locale\": null, \"text\": null, \"speak\": null, \"inputhint\": null, \"attachments\": [], \"entities\": []}")]
        [InlineData(1, "{\"type\":\"event\"}", "{\"type\":\"event\",\"id\":\"9a6cdbec-afb7-44be-81ad-886bd578393a\",\"timestamp\":\"2025-01-18T00:58:33.1434129\\u002B00:00\",\"channelId\":\"pva-studio\",\"from\":{\"id\":\"d2b4befb-8eca-e9f6-b04c-9edadb372e12/12e0ac1f-82d4-ef11-a730-000d3a35c98d\",\"name\":\"crd53_safeTravels\",\"role\":\"bot\"},\"conversation\":{\"id\":\"d19694c3-4ef9-47d4-a60a-8d4f47e0987e\"},\"recipient\":{\"id\":\"24a56070-e29b-4131-8a74-8f69fe9674ef\",\"aadObjectId\":\"24a56070-e29b-4131-8a74-8f69fe9674ef\",\"role\":\"user\"},\"membersAdded\":[],\"membersRemoved\":[],\"reactionsAdded\":[],\"reactionsRemoved\":[],\"locale\":\"en-US\",\"attachments\":[],\"entities\":[],\"replyToId\":\"04988bcb-ffbb-47f8-85f9-b1fbdf17c66f\",\"valueType\":\"DialogTracingInfo\",\"value\":{\"actions\":[{\"actionId\":\"end-topic\",\"topicId\":\"crd53_safeTravels.topic.Conversationalboosting\",\"triggerId\":\"main\",\"dialogComponentId\":\"45e77488-4c0d-4392-bd7c-b2848597fd0e\",\"actionType\":\"EndDialog\",\"conditionItemExit\":[\"has-answer\"],\"variableState\":{\"dialogState\":{},\"globalState\":{}},\"exception\":\"\",\"resultTrace\":{}}]},\"name\":\"DialogTracing\",\"listenFor\":[],\"textHighlights\":[]}")]
        public void TestFindMessage(int count, string query, string message)
        {
            // Arrange
            var provider = new CopilotPortalProvider();
            provider.Messages.Add(message);

            var testInfraFunctions = new Mock<ITestInfraFunctions>().Object;
            var testState = new Mock<ITestState>().Object;
            var logger = new Mock<ILogger>().Object;

            var function = new FindMessageFunction(testInfraFunctions, testState, logger, provider);

            // Act
            var result = function.Execute(FindMessageFunction.ConvertToFormulaValue(JsonConvert.DeserializeObject<TestQuery>(query)) as RecordValue);

            // Assert
            Assert.Equal(count, result.Rows.Count());
        }
    }

    public class TestQuery {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }

}
