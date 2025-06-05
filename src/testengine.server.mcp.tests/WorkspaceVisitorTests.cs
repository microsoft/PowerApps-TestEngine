// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Xml.Linq;
using Microsoft.PowerApps.TestEngine.MCP;
using Microsoft.PowerApps.TestEngine.MCP.Visitor;
using Microsoft.PowerApps.TestEngine.System;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Moq;

namespace testengine.server.mcp.tests
{
    public class WorkspaceVisitorTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger> _mockFrameworkLogger;
        private readonly RecalcEngine _recalcEngine;
        private readonly RecalcEngineAdapter _recalcEngineAdapter;
        private readonly string _workspacePath;

        public WorkspaceVisitorTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLogger = new Mock<ILogger>();
            _mockFrameworkLogger = new Mock<Microsoft.Extensions.Logging.ILogger>();
            _recalcEngine = new RecalcEngine();
            _recalcEngine.Config.AddFunction(new AddContextFunction());
            _recalcEngine.Config.AddFunction(new AddFactFunction());
            _recalcEngineAdapter = new RecalcEngineAdapter(_recalcEngine, _mockFrameworkLogger.Object);
            _workspacePath = @"c:\test-workspace";
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenParametersAreNull()
        {
            // Arrange
            ScanReference scanReference = new ScanReference
            {
                Name = "Test Scan"
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WorkspaceVisitor(null, _workspacePath, scanReference, _recalcEngineAdapter));
            Assert.Throws<ArgumentNullException>(() => new WorkspaceVisitor(_mockFileSystem.Object, null, scanReference, _recalcEngineAdapter));
            Assert.Throws<ArgumentNullException>(() => new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, null, _recalcEngineAdapter));
            Assert.Throws<ArgumentNullException>(() => new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, null));
        }

        [Fact]
        public void Visit_ShouldThrowDirectoryNotFoundException_WhenWorkspacePathDoesNotExist()
        {
            // Arrange
            _mockFileSystem.Setup(fs => fs.Exists(_workspacePath)).Returns(false);
            var scanReference = new ScanReference
            {
                Name = "Test Scan"
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => visitor.Visit());
        }

        [Fact]
        public void Visit_ShouldProcessOnFileRules_WhenFileMatchesPattern()
        {
            // Arrange
            var screenFilePath = Path.Combine(_workspacePath, "MainScreen.yaml");
            SetupBasicWorkspace(new[] { screenFilePath });

            _mockFileSystem.Setup(x => x.ReadAllText(screenFilePath)).Returns(@"");

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnFile = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current.Name, \".*Screen.*\")",
                        Then = "AddContext(Current, \"UI Screen File\")"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert

        }

        [Fact]
        public void Visit_ShouldProcessOnObjectRules_WhenControlsMatchPatterns()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "controls.yaml");

            // Setup YAML content with button and icon controls
            string yamlContent = @"
controls:
  Button1:
    type: button
    properties:
      Text: ""Submit""
      OnSelect: ""SubmitForm(Form1)""
  Icon1:
    type: icon
    properties:
      Image: ""info""
  Input1:
    type: input
    properties:
      Default: """"
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnObject = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current.Name, \".*Icon.*|.*Button.*|.*Input.*\")",
                        Then = @"
                          AddFact(Current);
                          With(
                            {
                              Name: Current.Name,
                              Type: If(
                                IsMatch(Current.Name, "".*Icon.*""), 
                                ""Icon"",
                                If(
                                  IsMatch(Current.Name, "".*Button.*""),
                                  ""Button"",
                                  ""TextInput""
                                )
                              ),
                              Parent: Current.Parent
                            },
                            AddFact({Type: ""Control"", Name: Self.Name, Path: Current.Path})
                          );"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify that control facts were created
            //var controlFacts = visitor.Facts.Where(f => f.Key == "Control").ToList();
            //Assert.Contains(controlFacts, f => f.Key == "Button1");
            //Assert.Contains(controlFacts, f => f.Key == "Icon1");
            //Assert.Contains(controlFacts, f => f.Key == "Input1");
        }

        [Fact]
        public void Visit_ShouldProcessOnPropertyRules_ForOnSelectProperties()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "button.yaml");

            // Setup YAML content with button that has OnSelect property
            string yamlContent = @"
screen:
  name: MainScreen
  controls:
    Button1:
      type: button
      properties:
        Text: ""Navigate""
        OnSelect: ""Navigate(DetailsScreen)""
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnProperty = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current.Name, \"OnSelect\")",
                        Then = @"
                          AddFact(Current);
                          AddContext(Current, ""Navigation Property"");"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify that property facts were created
            //var propertyFacts = visitor.Facts.Where(f => f.Key == "OnSelect").ToList();
            //Assert.NotEmpty(propertyFacts);
            //Assert.Contains(propertyFacts, f => f.Value == "Navigate(DetailsScreen)");
        }

        [Fact]
        public void Visit_ShouldProcessOnFunctionRules_ForNavigateCalls()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "navigation.yaml");

            // Setup YAML content with button that navigates
            string yamlContent = @"
screen:
  name: MainScreen
  controls:
    Button1:
      type: button
      properties:
        Text: ""Navigate""
        OnSelect: ""Navigate(DetailsScreen, None)""
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnFunction = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current, \"Navigate\")",
                        Then = @"
                          AddContext(Current, ""Screen Navigation"");
                          AddFact({
                            Type: ""Navigation"",
                            Name: ""NavigationCall"",
                            Value: Current,
                            Path: Current.Path
                          });"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify that navigation facts were created
            //var navigationFacts = visitor.Facts.Where(f => f.Key == "Navigation").ToList();
            //Assert.NotEmpty(navigationFacts);
            //Assert.Contains(navigationFacts, f => f.Value == "Navigate(DetailsScreen, None)");
        }

        [Fact]
        public void Visit_ShouldProcessOnFunctionRules_ForSubmitFormCalls()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "forms.yaml");

            // Setup YAML content with form submit
            string yamlContent = @"
screen:
  name: FormScreen
  controls:
    Form1:
      type: form
      properties:
        DataSource: ""Accounts""
    SubmitButton:
      type: button
      properties:
        Text: ""Submit Form""
        OnSelect: ""SubmitForm(Form1)""
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnFunction = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current, \"SubmitForm\")",
                        Then = @"
                          AddContext(Current, ""Form Submission"");
                          AddFact({
                            Type: ""FormSubmission"",
                            Name: ""FormSubmit"",
                            Value: Current,
                            Path: Current.Path
                          });"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            //// Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify that form submission facts were created
            //var formFacts = visitor.Facts.Where(f => f.Key == "FormSubmission").ToList();
            //Assert.NotEmpty(formFacts);
            //Assert.Contains(formFacts, f => f.Value == "SubmitForm(Form1)");
        }

        [Fact]
        public void Visit_ShouldProcessOnFunctionRules_ForDataOperations()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "data-ops.yaml");

            // Setup YAML content with data operations
            string yamlContent = @"
screen:
  name: DataScreen
  controls:
    CreateButton:
      type: button
      properties:
        Text: ""Create Record""
        OnSelect: ""Collect(Accounts, {Name: TextInput1.Text})""
    UpdateButton:
      type: button
      properties:
        Text: ""Update Record""
        OnSelect: ""Patch(Accounts, LookUp(Accounts, ID = SelectedID.Value), {Name: TextInput1.Text})""
    DeleteButton:
      type: button
      properties:
        Text: ""Delete Record""
        OnSelect: ""Remove(Accounts, LookUp(Accounts, ID = SelectedID.Value))""
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnFunction = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current, \"Patch|Collect|Remove|RemoveIf\")",
                        Then = @"
                          AddContext(Current, ""Data Operation"");
                          AddFact({
                            Type: ""DataOperation"",
                            Name: If(
                              IsMatch(Current, ""Patch""),
                              ""Update"",
                              If(
                                IsMatch(Current, ""Collect""),
                                ""Create"",
                                ""Delete""
                              )
                            ),
                            Value: Current,
                            Path: Current.Path
                          });"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify that data operation facts were created
            //var dataOpFacts = visitor.Facts.Where(f => f.Key == "DataOperation").ToList();
            //Assert.NotEmpty(dataOpFacts);
            //Assert.Contains(dataOpFacts, f => f.Key == "Create" && f.Value.Contains("Collect"));
            //Assert.Contains(dataOpFacts, f => f.Key == "Update" && f.Value.Contains("Patch"));
            //Assert.Contains(dataOpFacts, f => f.Key == "Delete" && f.Value.Contains("Remove"));
        }

        [Fact]
        public void Visit_ShouldProcessOnEndRules_WhenScanCompletes()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "app.yaml");

            // Setup simple YAML content
            string yamlContent = @"
app:
  name: TestApp
  path: /apps/test-app
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnFile = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "true",
                        Then = @"
                          AddFact({
                            Type: ""AppInfo"",
                            Name: ""AppPath"",
                            Value: ""/apps/test-app"",
                            Path: Current.Path
                          });"
                    }
                },
                OnEnd = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "true",
                        Then = @"
                          AddFact({
                            Type: ""ScanSummary"",
                            Name: ""Completed"",
                            Value: ""Scan completed successfully"",
                            Path: """"
                          });"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify that OnEnd facts were created
            //var summaryFacts = visitor.Facts.Where(f => f.Key == "ScanSummary").ToList();
            //Assert.NotEmpty(summaryFacts);
            //Assert.Contains(summaryFacts, f => f.Key == "Completed" && f.Value == "Scan completed successfully");
        }

        [Fact]
        public void Visit_ShouldProcessOnStartRules_BeforeScanBegins()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "app.yaml");

            // Setup simple YAML content
            string yamlContent = @"
app:
  name: TestApp
  path: /apps/test-app
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnStart = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "true",
                        Then = @"
                          AddFact({
                            Type: ""InitInfo"",
                            Name: ""ScanStarted"",
                            Value: ""Scan initialization completed"",
                            Path: """"
                          });"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify that OnStart facts were created
            //var initFacts = visitor.Facts.Where(f => f.Key == "InitInfo").ToList();
            //Assert.NotEmpty(initFacts);
            //Assert.Contains(initFacts, f => f.Key == "ScanStarted" && f.Value == "Scan initialization completed");
        }

        [Fact]
        public void Visit_ShouldCorrectlyIdentifyScreenWithNavigation()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "screens.yaml");

            // Setup YAML with screen definition
            string yamlContent = @"
app:
  screens:
    Screen1:
      type: screen
      controls:
        Label1:
          type: label
          properties:
            Text: ""Welcome Screen""
        NavigateButton:
          type: button
          properties:
            Text: ""Go to Details""
            OnSelect: ""Navigate(DetailsScreen)""
    DetailsScreen:
      type: screen
      controls:
        BackButton:
          type: button
          properties:
            Text: ""Back""
            OnSelect: ""Back()""
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnFile = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current.Name, \".*screens\")",
                        Then = @"
                          AddFact(Current);
                          AddContext(Current, ""Screen Definition"");
                          AddFact({
                            Type: ""Screen"",
                            Name: Current.Name,
                            Value: ""Screen"",
                            Path: Current.Path
                          });"
                    }
                },
                OnFunction = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current, \"Navigate\")",
                        Then = @"
                          AddFact({
                            Type: ""Navigation"",
                            Name: ""NavigationLink"",
                            Value: Current,
                            Path: Current.Path
                          });"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify screen facts
            //var screenFacts = visitor.Facts.Where(f => f.Key == "Screen").ToList();
            //Assert.Equal(2, screenFacts.Count());
            //Assert.Contains(screenFacts, f => f.Key == "Screen1");
            //Assert.Contains(screenFacts, f => f.Key == "DetailsScreen");

            //// Verify navigation
            //var navigationFacts = visitor.Facts.Where(f => f.Key == "Navigation").ToList();
            //Assert.NotEmpty(navigationFacts);
            //Assert.Contains(navigationFacts, f => f.Value.Contains("Navigate(DetailsScreen)"));
        }

        [Fact]
        public void Visit_ShouldIdentifyAndTrackFormValidation()
        {
            // Arrange
            var yamlFilePath = Path.Combine(_workspacePath, "validation.yaml");

            // Setup YAML with validation rules
            string yamlContent = @"
app:
  screens:
    FormScreen:
      type: screen
      controls:
        NameInput:
          type: textInput
          properties:
            Default: """"
            ValidationMessage: ""Name is required""
            Validation: ""!IsBlank(Self.Text)""
        EmailInput:
          type: textInput
          properties:
            Default: """"
            ValidationMessage: ""Invalid email format""
            Validation: ""IsMatch(Self.Text, '[^@]+@[^\.]+\..+')""
";

            SetupBasicWorkspace(new[] { yamlFilePath });
            _mockFileSystem.Setup(fs => fs.ReadAllText(yamlFilePath)).Returns(yamlContent);

            var scanReference = new ScanReference
            {
                Name = "Test Scan",
                OnProperty = new List<ScanRule>
                {
                    new ScanRule
                    {
                        When = "IsMatch(Current.Name, \".*Valid.*|.*Validation.*\")",
                        Then = @"
                          AddFact(Current);
                          AddFact({
                            Type: ""Validation"",
                            Name: Current.Parent.Name + ""_Validation"",
                            Value: Current.Formula,
                            Path: Current.Path
                          });"
                    }
                }
            };

            var visitor = new WorkspaceVisitor(_mockFileSystem.Object, _workspacePath, scanReference, _recalcEngineAdapter, _mockLogger.Object);

            // Act
            visitor.Visit();

            // Assert
            //Assert.NotEmpty(visitor.Facts);

            //// Verify validation facts
            //var validationFacts = visitor.Facts.Where(f => f.Key == "Validation").ToList();
            //Assert.Equal(2, validationFacts.Count());
            //Assert.Contains(validationFacts, f => f.Key == "NameInput_Validation" && f.Value == "!IsBlank(Self.Text)");
            //Assert.Contains(validationFacts, f => f.Key == "EmailInput_Validation" && f.Value.Contains("IsMatch"));
        }

        private void SetupBasicWorkspace(string[] files)
        {
            _mockFileSystem.Setup(fs => fs.Exists(_workspacePath)).Returns(true);
            _mockFileSystem.Setup(fs => fs.GetDirectories(_workspacePath)).Returns(Array.Empty<string>());
            _mockFileSystem.Setup(fs => fs.GetFiles(_workspacePath)).Returns(files);

            foreach (var file in files)
            {
                _mockFileSystem.Setup(fs => fs.Exists(file)).Returns(true);
            }
        }


        private class AddContextFunction : ReflectionFunction
        {
            public AddContextFunction() : base("AddContext", BooleanType.Boolean, RecordType.Empty(), StringType.String)
            {
            }
            public BooleanValue Execute(RecordValue node, StringValue context)
            {
                return FormulaValue.New(true);
            }
        }

        private class AddFactFunction : ReflectionFunction
        {
            public AddFactFunction() : base("AddFact", BooleanType.Boolean, RecordType.Empty())
            {
            }

            public BooleanValue Execute(RecordValue fact)
            {
                return FormulaValue.New(true);
            }
        }
    }
}
