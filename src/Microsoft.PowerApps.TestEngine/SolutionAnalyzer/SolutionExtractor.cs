using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.PowerApps.TestEngine.SolutionAnalyzer
{
    public class SolutionExtractor
    {
        public string ExtractSolution(string solutionZipPath, string extractionPath)
        {
            if (!File.Exists(solutionZipPath))
                throw new FileNotFoundException($"Solution file not found: {solutionZipPath}");

            if (Directory.Exists(extractionPath))
                Directory.Delete(extractionPath, true);
                
            Directory.CreateDirectory(extractionPath);
            ZipFile.ExtractToDirectory(solutionZipPath, extractionPath);

            return extractionPath;
        }

        public string FindMsAppFile(string extractedSolutionPath)
        {
            var canvasAppsPath = Path.Combine(extractedSolutionPath, "CanvasApps");
            
            if (!Directory.Exists(canvasAppsPath))
                throw new DirectoryNotFoundException("CanvasApps folder not found in solution");

            var msappFiles = Directory.GetFiles(canvasAppsPath, "*.msapp", SearchOption.AllDirectories);
            
            if (msappFiles.Length == 0)
                throw new FileNotFoundException("No .msapp files found in solution");

            return msappFiles[0];
        }

        public string GetAppLogicalName(string extractedSolutionPath, string msappFileName)
        {
            var customizationsPath = Path.Combine(extractedSolutionPath, "customizations.xml");
            
            if (!File.Exists(customizationsPath))
                return Path.GetFileNameWithoutExtension(msappFileName);

            try
            {
                var doc = XDocument.Load(customizationsPath);
                var canvasApp = doc.Descendants("CanvasApp")
                    .FirstOrDefault(x => x.Element("Name")?.Value == Path.GetFileNameWithoutExtension(msappFileName));

                return canvasApp?.Element("UniqueName")?.Value ?? Path.GetFileNameWithoutExtension(msappFileName);
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(msappFileName);
            }
        }
    }
}
