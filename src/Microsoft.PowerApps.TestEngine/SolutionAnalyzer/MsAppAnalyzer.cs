using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.PowerApps.TestEngine.SolutionAnalyzer
{
    public class MsAppAnalyzer
    {
        public AppStructure AnalyzeMsApp(string msappFilePath)
        {
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"msapp_{Guid.NewGuid()}");
            
            try
            {
                Console.WriteLine($"DEBUG: Extracting msapp to: {tempExtractPath}");
                Directory.CreateDirectory(tempExtractPath);
                ZipFile.ExtractToDirectory(msappFilePath, tempExtractPath);

                Console.WriteLine("DEBUG: Extracted structure:");
                ListDirectory(tempExtractPath, "  ");

                var appStructure = new AppStructure
                {
                    AppName = Path.GetFileNameWithoutExtension(msappFilePath),
                    Screens = new List<ScreenInfo>()
                };

                // First try to parse Src folder (unpacked format with YAML files)
                var srcPath = Path.Combine(tempExtractPath, "Src");
                if (Directory.Exists(srcPath))
                {
                    Console.WriteLine("DEBUG: Found Src folder - parsing YAML files");
                    ParseSrcFolder(srcPath, appStructure);
                }
                
                // If no controls found, try CanvasManifest + Controls folder
                if (appStructure.Screens.Sum(s => s.Controls.Count) == 0)
                {
                    Console.WriteLine("DEBUG: No controls from Src folder, trying Controls folder");
                    ParseFromCanvasManifest(tempExtractPath, appStructure);
                    ParseControlsFolder(tempExtractPath, appStructure);
                }

                Console.WriteLine($"DEBUG: Analysis complete - found {appStructure.Screens.Count} screens with total {appStructure.Screens.Sum(s => s.Controls.Count)} controls");
                return appStructure;
            }
            finally
            {
                if (Directory.Exists(tempExtractPath))
                {
                    try { Directory.Delete(tempExtractPath, true); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }

        private void ListDirectory(string path, string indent)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    Console.WriteLine($"{indent}DIR: {Path.GetFileName(dir)}");
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    Console.WriteLine($"{indent}FILE: {Path.GetFileName(file)}");
                }
            }
            catch { }
        }

        private void ParseFromCanvasManifest(string extractPath, AppStructure appStructure)
        {
            try
            {
                var canvasManifestPath = Path.Combine(extractPath, "CanvasManifest.json");
                if (!File.Exists(canvasManifestPath))
                {
                    Console.WriteLine("DEBUG: CanvasManifest.json not found");
                    return;
                }

                Console.WriteLine("DEBUG: Parsing CanvasManifest.json");
                var json = File.ReadAllText(canvasManifestPath);
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("Screens", out var screens))
                {
                    Console.WriteLine($"DEBUG: Found {screens.GetArrayLength()} screens in manifest");
                    foreach (var screen in screens.EnumerateArray())
                    {
                        if (screen.TryGetProperty("Name", out var screenName))
                        {
                            var name = screenName.GetString();
                            Console.WriteLine($"DEBUG: Processing screen: {name}");
                            
                            var screenInfo = new ScreenInfo
                            {
                                Name = name,
                                Controls = new List<ControlInfo>()
                            };
                            appStructure.Screens.Add(screenInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error parsing CanvasManifest: {ex.Message}");
            }
        }

        private void ParseControlsFolder(string extractPath, AppStructure appStructure)
        {
            var controlsPath = Path.Combine(extractPath, "Controls");
            
            if (!Directory.Exists(controlsPath))
            {
                Console.WriteLine("DEBUG: Controls folder not found");
                return;
            }

            Console.WriteLine($"DEBUG: Parsing Controls folder");
            
            // Get all JSON files in Controls folder
            var jsonFiles = Directory.GetFiles(controlsPath, "*.json", SearchOption.AllDirectories);
            Console.WriteLine($"DEBUG: Found {jsonFiles.Length} control definition files");

            foreach (var jsonFile in jsonFiles)
            {
                var screenName = Path.GetFileNameWithoutExtension(jsonFile).Replace(".json", "");
                Console.WriteLine($"DEBUG: Parsing controls for: {screenName}");
                
                // Find the corresponding screen
                var screen = appStructure.Screens.FirstOrDefault(s => 
                    s.Name.Equals(screenName, StringComparison.OrdinalIgnoreCase));
                
                if (screen == null)
                {
                    // Create screen if not found
                    screen = new ScreenInfo
                    {
                        Name = screenName,
                        Controls = new List<ControlInfo>()
                    };
                    appStructure.Screens.Add(screen);
                }

                // Parse the JSON to extract controls
                ParseControlJson(jsonFile, screen.Controls);
                
                Console.WriteLine($"DEBUG:   Found {screen.Controls.Count} controls in {screenName}");
            }
        }

        private void ParseControlJson(string jsonFilePath, List<ControlInfo> controls)
        {
            try
            {
                var json = File.ReadAllText(jsonFilePath);
                var doc = JsonDocument.Parse(json);
                
                // Parse the root element
                ParseJsonControl(doc.RootElement, controls);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error parsing control JSON {jsonFilePath}: {ex.Message}");
            }
        }

        private void ParseJsonControl(JsonElement element, List<ControlInfo> controls)
        {
            string controlName = null;
            string controlType = "Unknown";

            // Extract control name
            if (element.TryGetProperty("Name", out var nameEl))
                controlName = nameEl.GetString();

            // Extract control type
            if (element.TryGetProperty("Template", out var templateEl))
                controlType = templateEl.GetString();
            else if (element.TryGetProperty("Type", out var typeEl))
                controlType = typeEl.GetString();
            else if (element.TryGetProperty("ControlType", out var ctrlTypeEl))
                controlType = ctrlTypeEl.GetString();

            // Add control if valid
            if (!string.IsNullOrEmpty(controlName) && !controlType.Equals("screen", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"DEBUG:     Control: {controlName} ({controlType})");
                controls.Add(new ControlInfo
                {
                    Name = controlName,
                    Type = controlType,
                    Properties = new Dictionary<string, string>()
                });
            }

            // Recursively parse child controls
            if (element.TryGetProperty("Children", out var children))
            {
                foreach (var child in children.EnumerateArray())
                {
                    ParseJsonControl(child, controls);
                }
            }
        }

        private void ParseSrcFolder(string srcPath, AppStructure appStructure)
        {
            Console.WriteLine("DEBUG: Listing files in Src folder:");
            var allFiles = Directory.GetFiles(srcPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in allFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
            }

            // Look for .fx.yaml, .pa.yaml, or .yaml files
            var yamlFiles = allFiles.Where(f => 
                f.EndsWith(".fx.yaml", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".pa.yaml", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            ).ToList();

            Console.WriteLine($"DEBUG: Found {yamlFiles.Count} YAML files to parse");

            foreach (var yamlFile in yamlFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(yamlFile);
                
                // Skip App files
                if (fileName.Equals("App", StringComparison.OrdinalIgnoreCase) ||
                    fileName.StartsWith("App.", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"DEBUG: Skipping App file: {fileName}");
                    continue;
                }

                Console.WriteLine($"DEBUG: Parsing YAML file: {fileName}");
                var screenInfo = ParseYamlFile(yamlFile);
                if (screenInfo != null && screenInfo.Controls.Count > 0)
                {
                    appStructure.Screens.Add(screenInfo);
                    Console.WriteLine($"DEBUG: Added screen '{screenInfo.Name}' with {screenInfo.Controls.Count} controls");
                }
            }
        }

        private ScreenInfo ParseYamlFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                fileName = fileName.Replace(".fx", "").Replace(".pa", "");
                
                Console.WriteLine($"DEBUG: ========== Parsing {fileName} ==========");
                
                var screenInfo = new ScreenInfo
                {
                    Name = fileName,
                    Controls = new List<ControlInfo>()
                };

                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                
                // Parse the new YAML format where controls are under Children
                bool inChildrenSection = false;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    
                    // Check if we're entering Children section
                    if (line.Trim() == "Children:")
                    {
                        inChildrenSection = true;
                        Console.WriteLine($"DEBUG: Found Children section at line {i}");
                        continue;
                    }
                    
                    // If we're in Children section, look for control definitions
                    if (inChildrenSection)
                    {
                        // Match pattern: "  - ControlName:"
                        var match = Regex.Match(line, @"^\s+- (\w+):\s*$");
                        if (match.Success)
                        {
                            var controlName = match.Groups[1].Value;
                            
                            // Look ahead for Control type in next lines
                            string controlType = "Unknown";
                            for (int j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                            {
                                var nextLine = lines[j];
                                // Match: "          Control: FluentV8/Label@1.8.6"
                                var controlMatch = Regex.Match(nextLine, @"Control:\s+([\w/]+)");
                                if (controlMatch.Success)
                                {
                                    var fullType = controlMatch.Groups[1].Value;
                                    // Extract the control type after the last /
                                    // "FluentV8/Label@1.8.6" -> "Label"
                                    var parts = fullType.Split('/');
                                    if (parts.Length > 1)
                                    {
                                        controlType = parts[parts.Length - 1].Split('@')[0];
                                    }
                                    else
                                    {
                                        controlType = fullType.Split('@')[0];
                                    }
                                    break;
                                }
                            }
                            
                            Console.WriteLine($"DEBUG:   [FOUND] Control: {controlName} | Type: {controlType}");
                            
                            screenInfo.Controls.Add(new ControlInfo
                            {
                                Name = controlName,
                                Type = controlType,
                                Properties = new Dictionary<string, string>()
                            });
                        }
                        
                        // Exit Children section if indentation decreases significantly
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("  "))
                        {
                            inChildrenSection = false;
                            Console.WriteLine($"DEBUG: Exiting Children section at line {i}");
                        }
                    }
                }

                Console.WriteLine($"DEBUG: ========== Summary for {fileName} ==========");
                Console.WriteLine($"DEBUG: Total controls found: {screenInfo.Controls.Count}");
                foreach (var ctrl in screenInfo.Controls)
                {
                    Console.WriteLine($"DEBUG:   - {ctrl.Name} ({ctrl.Type})");
                }
                Console.WriteLine($"DEBUG: ==========================================");
                
                return screenInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: ERROR parsing YAML {filePath}:");
                Console.WriteLine($"DEBUG: {ex.Message}");
                return null;
            }
        }
    }

    public class AppStructure
    {
        public string AppName { get; set; }
        public List<ScreenInfo> Screens { get; set; }
    }

    public class ScreenInfo
    {
        public string Name { get; set; }
        public List<ControlInfo> Controls { get; set; }
    }

    public class ControlInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}
