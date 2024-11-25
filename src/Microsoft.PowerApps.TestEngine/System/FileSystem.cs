// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Microsoft.PowerApps.TestEngine.System
{
    /// <summary>
    /// Wrapper for any System.IO methods needed
    /// </summary>
    [Export(typeof(IFileSystem))]
    public class FileSystem : IFileSystem
    {
        public void CreateDirectory(string directoryName)
        {
            if (IsNonUNCDirectoryPath(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or write to path: '{0}' not permitted.", directoryName));
            }
        }

        public bool Exists(string directoryName)
        {
            if (IsNonUNCDirectoryPath(directoryName))
            {
                return Directory.Exists(directoryName);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or write to path: '{0}' not permitted.", directoryName));
            }
        }

        public bool FileExists(string fileName)
        {
            if (IsValidFilePath(fileName))
            {
                return File.Exists(fileName);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid file path '{0}'.", fileName));
            }
        }

        public string[] GetFiles(string directoryName)
        {
            if (IsNonUNCDirectoryPath(directoryName))
            {
                return Directory.GetFiles(directoryName);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or read from path: '{0}' not permitted.", directoryName));
            }
        }

        public string[] GetFiles(string directoryName, string searchPattern)
        {
            if (IsNonUNCDirectoryPath(directoryName))
            {
                return Directory.GetFiles(directoryName, searchPattern);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or read from path: '{0}' not permitted.", directoryName));
            }
        }

        public void WriteTextToFile(string filePath, string text)
        {
            if (IsWritePermittedFilePath(filePath))
            {
                if (File.Exists(filePath))
                {
                    File.AppendAllText(filePath, text);
                }
                else
                {
                    File.WriteAllText(filePath, text);
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Write to path: '{0}' not permitted, ensure path is rooted in base TestEngine path.", filePath));
            }
        }

        public void WriteTextToFile(string filePath, string[] text)
        {
            if (IsWritePermittedFilePath(filePath))
            {
                if (File.Exists(filePath))
                {
                    File.AppendAllLines(filePath, text);
                }
                else
                {
                    File.WriteAllLines(filePath, text);
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Write to path: '{0}' not permitted, ensure path is rooted in base TestEngine path.", filePath));
            }
        }

        public bool IsValidFilePath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // Ensure the file path doesn't end with a space or period (Windows restriction)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (filePath.EndsWith(" ") || filePath.EndsWith("."))
                        return false;
                }
                var fullPath = Path.GetFullPath(filePath);

                //check if its a network path, if so fail
                var fullPathUri = new Uri(fullPath.StartsWith(@"\\?\") ? fullPath.Replace(@"\\?\", "") : fullPath);
                if (fullPathUri.IsUnc)
                {
                    return false;
                }
                // Check for reserved device names (Windows only)
                if (ReservedFolderNamesExistInFilePath(fullPath))
                {
                    return false;
                }

                //just get this to check if its a valid file path, if its not then it throws 
                var g = new FileInfo(fullPath).IsReadOnly;
                var fileName = Path.GetFileName(filePath);
                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return false;
                if (string.IsNullOrWhiteSpace(fileName) || fileName.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ReservedFolderNamesExistInFilePath(string fileFullPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "CLOCK$", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

                var root = Path.GetPathRoot(fileFullPath);
                var restOfPath = fileFullPath.Substring(root.Length);

                string[] pathSegments = restOfPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                // Iterate over folder names
                for (int i = 0; i < pathSegments.Length - 1; i++)
                {
                    if (reservedNames.Contains(pathSegments[i], StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Last segment filename, strip extension and validate
                string lastSegment = pathSegments.Last();
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(lastSegment.Trim());

                // Check if the file name (without extension) is a reserved name
                if (reservedNames.Contains(fileNameWithoutExtension.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsNonUNCDirectoryPath(string directoryPath)
        {
            try
            {
                //check is only to verify its not a UNC path
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    return false;
                }
                if (directoryPath.EndsWith(" ") || directoryPath.EndsWith("."))
                {
                    return false;
                }
                var fullPath = Path.GetFullPath(directoryPath);

                //check if its a network path if so fail
                var fullPathUri = new Uri(fullPath.StartsWith(@"\\?\") ? fullPath.Replace(@"\\?\", "") : fullPath);
                if (fullPathUri.IsUnc)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string ReadAllText(string filePath)
        {
            if (IsValidFilePath(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid file path '{0}'.", filePath));
            }
        }

        public string RemoveInvalidFileNameChars(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public void WriteFile(string filePath, byte[] data)
        {
            if (IsWritePermittedFilePath(filePath))
            {
                File.WriteAllBytes(filePath, data);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Write to path: '{0}' not permitted, ensure path is rooted in base TestEngine path.", filePath));
            }
        }

        public string GetTempPath()
        {
            return Path.GetTempPath();
        }

        public string GetDefaultRootTestEngine()
        {
            return Path.Combine(GetTempPath(), "Microsoft", "TestEngine") + Path.DirectorySeparatorChar;
        }

        public bool IsWritePermittedFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;
            if (IsValidFilePath(filePath) && Path.IsPathRooted(filePath))
            {
                var fullPath = Path.GetFullPath(filePath);
                var fullPathUri = new Uri(fullPath.StartsWith(@"\\?\") ? fullPath.Replace(@"\\?\", "") : fullPath);
                var baseUri = new Uri(GetDefaultRootTestEngine());
                if (baseUri.IsBaseOf(fullPathUri))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
