// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace testengine.server.mcp.tests
{
    public class PlanDesignerServiceTest
    {
        private readonly Mock<IOrganizationService> _mockOrganizationService;
        private readonly Mock<SourceCodeService> _mockSourceCodeService;
        private readonly PlanDesignerService _planDesignerService;

        public PlanDesignerServiceTest()
        {
            _mockOrganizationService = new Mock<IOrganizationService>();
            _mockSourceCodeService = new Mock<SourceCodeService>();
            _planDesignerService = new PlanDesignerService(_mockOrganizationService.Object, _mockSourceCodeService.Object);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenOrganizationServiceIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PlanDesignerService(null, null));
        }

        [Fact]
        public void GetPlans_ShouldReturnEmptyList_WhenNoPlansExist()
        {
            // Arrange
            var emptyEntityCollection = new EntityCollection();
            _mockOrganizationService
                .Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
                .Returns(emptyEntityCollection);

            // Act
            var plans = _planDesignerService.GetPlans();

            // Assert
            Assert.NotNull(plans);
            Assert.Empty(plans);
        }

        [Fact]
        public void GetPlans_ShouldReturnListOfPlans_WhenPlansExist()
        {
            // Arrange
            var entityCollection = new EntityCollection(new List<Entity>
            {
                new Entity("msdyn_plan")
                {
                    ["msdyn_planid"] = Guid.NewGuid(),
                    ["msdyn_name"] = "Test Plan 1",
                    ["msdyn_description"] = "Description 1",
                    ["modifiedon"] = DateTime.UtcNow,
                    ["solutionid"] = Guid.NewGuid()
                },
                new Entity("msdyn_plan")
                {
                    ["msdyn_planid"] = Guid.NewGuid(),
                    ["msdyn_name"] = "Test Plan 2",
                    ["msdyn_description"] = "Description 2",
                    ["modifiedon"] = DateTime.UtcNow,
                    ["solutionid"] = Guid.NewGuid()
                }
            });

            _mockOrganizationService
                .Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
                .Returns(entityCollection);

            // Act
            var plans = _planDesignerService.GetPlans();

            // Assert
            Assert.NotNull(plans);
            Assert.Equal(2, plans.Count);
            Assert.Equal("Test Plan 1", plans[0].Name);
            Assert.Equal("Test Plan 2", plans[1].Name);
        }

        [Fact]
        public void GetPlanDetails_ShouldThrowException_WhenPlanDoesNotExist()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var emptyEntityCollection = new EntityCollection();
            _mockOrganizationService
                .Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
                .Returns(emptyEntityCollection);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _planDesignerService.GetPlanDetails(planId));
            Assert.Equal($"Plan with ID {planId} not found.", exception.Message);
        }

        [Fact]
        public void GetPlanDetails_ShouldReturnPlanDetails_WhenPlanExists()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var solutionId = Guid.NewGuid();
            var entity = new Entity("msdyn_plan")
            {
                ["msdyn_planid"] = planId,
                ["msdyn_name"] = "Test Plan",
                ["msdyn_description"] = "Test Description",
                ["msdyn_prompt"] = "Test Prompt",
                ["msdyn_contentschemaversion"] = "1.0",
                ["msdyn_languagecode"] = 1033,
                ["modifiedon"] = DateTime.UtcNow,
                ["solutionid"] = solutionId
            };

            var entityCollection = new EntityCollection(new List<Entity> { entity });
            _mockOrganizationService
                .Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
                .Returns(entityCollection);

            _mockOrganizationService
                .Setup(service => service.Retrieve("msdyn_plan", planId, It.Is<ColumnSet>(c => c.Columns[0] == "msdyn_content")))
                .Returns(new Entity());

            _mockOrganizationService
                .Setup(service => service.Retrieve("msdyn_planartifact", Guid.Empty, It.IsAny<ColumnSet>()))
                .Returns(new Entity());

            _mockSourceCodeService.Setup(m => m.LoadSolutionFromSourceControl(new WorkspaceRequest() { Location = "valid/path" })).Returns(null);

            // Act
            var planDetails = _planDesignerService.GetPlanDetails(planId, "valid/path");

            // Assert
            Assert.NotNull(planDetails);
            Assert.Equal(planId, planDetails.Id);
            Assert.Equal("Test Plan", planDetails.Name);
            Assert.Equal("Test Description", planDetails.Description);
            Assert.Equal("Test Prompt", planDetails.Prompt);
            Assert.Equal(1033, planDetails.LanguageCode);
        }

        [Fact]
        public void GetPlanArtifacts_ShouldReturnEmptyList_WhenNoArtifactsExist()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var emptyEntityCollection = new EntityCollection();
            _mockOrganizationService
                .Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
                .Returns(emptyEntityCollection);

            // Act
            var artifacts = _planDesignerService.GetPlanArtifacts(planId);

            // Assert
            Assert.NotNull(artifacts);
            Assert.Empty(artifacts);
        }

        [Fact]
        public void GetPlanArtifacts_ShouldReturnListOfArtifacts_WhenArtifactsExist()
        {
            // Arrange
            var planId = Guid.NewGuid();
            var artifactId = Guid.NewGuid();
            var entity = new Entity("msdyn_planartifact")
            {
                ["msdyn_planartifactid"] = artifactId,
                ["msdyn_name"] = "Test Artifact",
                ["msdyn_type"] = "Type1",
                ["msdyn_artifactstatus"] = new OptionSetValue(1),
                ["msdyn_description"] = "Artifact Description"
            };

            var entityCollection = new EntityCollection(new List<Entity> { entity });
            _mockOrganizationService
                .Setup(service => service.RetrieveMultiple(It.IsAny<QueryExpression>()))
                .Returns(entityCollection);


            _mockOrganizationService
                .Setup(service => service.Retrieve("msdyn_planartifact", artifactId, It.Is<ColumnSet>(c => c.Columns[0] == "msdyn_artifactmetadata")))
                .Returns(new Entity());

            _mockOrganizationService
                .Setup(service => service.Retrieve("msdyn_planartifact", artifactId, It.Is<ColumnSet>(c => c.Columns[0] == "msdyn_proposal")))
                .Returns(new Entity());

            // Act
            var artifacts = _planDesignerService.GetPlanArtifacts(planId);

            // Assert
            Assert.NotNull(artifacts);
            Assert.Single(artifacts);
            Assert.Equal(artifactId, artifacts[0].Id);
            Assert.Equal("Test Artifact", artifacts[0].Name);
            Assert.Equal("Type1", artifacts[0].Type);
            Assert.Equal(1, artifacts[0].Status);
            Assert.Equal("Artifact Description", artifacts[0].Description);
        }
    }
}
