using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Microsoft.PowerApps.TestEngine.SolutionAnalyzer
{
    public class MsAppUnpacker
    {
        public string UnpackMsApp(string msappPath, string outputPath)
        {
            Console.WriteLine($"DEBUG: Unpacking msapp: {msappPath}");
            
            // Create temporary directory for unpacking
            var unpackDir = Path.Combine(outputPath, $"unpacked_{Guid.NewGuid()}");
            Directory.CreateDirectory(unpackDir);
            
            try
            {
                // Extract msapp as ZIP first
                var tempExtract = Path.Combine(Path.GetTempPath(), $"msapp_temp_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempExtract);
                ZipFile.ExtractToDirectory(msappPath, tempExtract);
                
                // Check if already unpacked (has Src folder)
                var srcFolder = Path.Combine(tempExtract, "Src");
                if (Directory.Exists(srcFolder))
                {
                    Console.WriteLine("DEBUG: msapp is already in unpacked format");
                    return tempExtract;
                }
                
                // If packed, we need to use PASopa to unpack
                // For now, we'll work with the extracted structure
                Console.WriteLine("DEBUG: Working with extracted msapp structure");
                return tempExtract;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error unpacking msapp: {ex.Message}");
                throw;
            }
        }
    }
}
