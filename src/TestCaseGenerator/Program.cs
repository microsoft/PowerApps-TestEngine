using System;
using System.IO;
using Microsoft.PowerApps.TestEngine.SolutionAnalyzer;
using Microsoft.PowerApps.TestEngine.TestCaseGenerator;

namespace TestCaseGeneratorTool
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            // Set default args for debugging when no args provided
            if (args.Length == 0)
            {
                Console.WriteLine("DEBUG MODE: Using default arguments");
                args = new[]
                {
                    @"C:\RR\TestEngine_RR\PowerApps-TestEngine\samples\DemoApp\DemoApp_1_0_0_1.zip",
                    @"C:\RR\TestEngine_RR\PowerApps-TestEngine\samples\DemoApp\demoapp-tests.fx.yaml",
                    "fd1a868e-5b14-ef06-b342-3568544a24d9",
                    "72f988bf-86f1-41af-91ab-2d7cd011db47"
                };
            }
#endif
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════╗");
            Console.WriteLine("║  Power Apps Test Case Generator v1.0          ║");
            Console.WriteLine("╚════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            if (args.Length < 4)
            {
                ShowUsage();
                return;
            }

            string solutionPath = args[0];
            string outputPath = args[1];
            string environmentId = args[2];
            string tenantId = args[3];

            try
            {
                Execute(solutionPath, outputPath, environmentId, tenantId);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  TestCaseGenerator.exe <solution-path> <output-path> <environment-id> <tenant-id>");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  solution-path    : Path to the Power Apps solution ZIP file");
            Console.WriteLine("  output-path      : Path where the test plan YAML will be generated");
            Console.WriteLine("  environment-id   : GUID of the target Power Apps environment");
            Console.WriteLine("  tenant-id        : GUID of the Azure AD tenant");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  TestCaseGenerator.exe ^");
            Console.WriteLine("    C:\\Solutions\\MyApp_1_0_0_1.zip ^");
            Console.WriteLine("    C:\\TestPlans\\myapp-tests.fx.yaml ^");
            Console.WriteLine("    12345678-1234-1234-1234-123456789012 ^");
            Console.WriteLine("    87654321-4321-4321-4321-210987654321");
        }

        static void Execute(string solutionPath, string outputPath, string environmentId, string tenantId)
        {
            Console.WriteLine($"📦 Solution    : {Path.GetFileName(solutionPath)}");
            Console.WriteLine($"📄 Output      : {Path.GetFileName(outputPath)}");
            Console.WriteLine($"🌐 Environment : {environmentId}");
            Console.WriteLine($"🏢 Tenant      : {tenantId}");
            Console.WriteLine();

            // Step 1: Extract solution ZIP
            Console.Write("[1/5] Extracting solution ZIP... ");
            var extractor = new SolutionExtractor();
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"solution_{Guid.NewGuid()}");
            var extractedPath = extractor.ExtractSolution(solutionPath, tempExtractPath);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓");
            Console.ResetColor();

            // Step 2: Find msapp file inside the solution
            Console.Write("[2/5] Locating .msapp file in solution... ");
            var msappPath = extractor.FindMsAppFile(extractedPath);
            var appLogicalName = extractor.GetAppLogicalName(extractedPath, Path.GetFileName(msappPath));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓");
            Console.ResetColor();
            Console.WriteLine($"      Found: {Path.GetFileName(msappPath)}");
            Console.WriteLine($"      App Logical Name: {appLogicalName}");

            // Step 3: Unpack and analyze msapp file
            Console.Write("[3/5] Unpacking and analyzing .msapp file... ");
            var analyzer = new MsAppAnalyzer();
            var appStructure = analyzer.AnalyzeMsApp(msappPath);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓");
            Console.ResetColor();
            
            Console.WriteLine($"      Screens found: {appStructure.Screens.Count}");
            int totalControls = 0;
            foreach (var screen in appStructure.Screens)
            {
                Console.WriteLine($"        • {screen.Name}: {screen.Controls.Count} control(s)");
                totalControls += screen.Controls.Count;
            }

            // Step 4: Generate test cases
            Console.Write("[4/5] Generating comprehensive test cases... ");
            var generator = new TestCaseGenerator();
            var testPlan = generator.GenerateTestPlan(appStructure, appLogicalName, environmentId, tenantId);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓");
            Console.ResetColor();

            // Step 5: Save test plan
            Console.Write("[5/5] Saving test plan file... ");
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllText(outputPath, testPlan);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓");
            Console.ResetColor();

            // Summary
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("✓ Test Plan Generated Successfully!");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine($"📊 Statistics:");
            Console.WriteLine($"   • Screens analyzed    : {appStructure.Screens.Count}");
            Console.WriteLine($"   • Controls discovered : {totalControls}");
            Console.WriteLine($"   • Test cases generated: {EstimateTestCases(appStructure)}");
            Console.WriteLine($"   • Output file         : {outputPath}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Review the generated test plan");
            Console.WriteLine("  2. Customize test cases as needed");
            Console.WriteLine("  3. Run: dotnet PowerAppsTestEngine.dll -i " + outputPath);

            // Cleanup
            try
            {
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);
            }
            catch { /* Ignore cleanup errors */ }
        }

        static int EstimateTestCases(AppStructure appStructure)
        {
            int count = 0;
            foreach (var screen in appStructure.Screens)
            {
                foreach (var control in screen.Controls)
                {
                    var type = control.Type.ToLower();
                    if (type.Contains("label")) count += 5;
                    else if (type.Contains("textinput")) count += 5;
                    else if (type.Contains("button")) count += 2;
                    else if (type.Contains("checkbox")) count += 2;
                    else if (type.Contains("combobox") || type.Contains("dropdown")) count += 1;
                    else if (type.Contains("datepicker")) count += 1;
                    else if (type.Contains("radio")) count += 3;
                    else if (type.Contains("slider")) count += 1;
                    else if (type.Contains("toggle")) count += 1;
                    else if (type.Contains("gallery")) count += 1;
                    else count += 1;
                }
            }
            return count;
        }
    }
}
