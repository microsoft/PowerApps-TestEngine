// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.IO;
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
            Directory.CreateDirectory(directoryName);
        }

        public bool Exists(string directoryName)
        {
            return Directory.Exists(directoryName);
        }

        public bool FileExists(string fileName)
        {
            return File.Exists(fileName);
        }

        public string[] GetFiles(string directoryName)
        {
            return Directory.GetFiles(directoryName);
        }

        public string[] GetFiles(string directoryName, string searchPattern)
        {
            return Directory.GetFiles(directoryName, searchPattern);
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
                throw new InvalidOperationException(string.Format("Write to path: {0} not permitted, ensure path is rooted in {1}.", filePath, GetDefaultRootTestEngine()));
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
                throw new InvalidOperationException(string.Format("Write to path: {0} not permitted, ensure path is rooted in {1}.", filePath, GetDefaultRootTestEngine()));
            }
        }

        public bool IsValidFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;
            filePath = filePath.Trim();

            try
            {
                // Get the full normalized path
                string fullPath = Path.GetFullPath(filePath);

                //just get this to check if its a valid file path, if its not then it throws 
                var g = new FileInfo(fullPath).IsReadOnly;
                string fileName = Path.GetFileName(filePath);
                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return false;
                if (string.IsNullOrWhiteSpace(fileName) || fileName.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public string RemoveInvalidFileNameChars(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public string GetTempPath()
        {
            return Path.GetTempPath();
        }

        public string GetDefaultRootTestEngine()
        {
            return Path.Combine(GetTempPath(), "TestEngine") + Path.DirectorySeparatorChar;
        }

        public bool IsWritePermittedFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;
            filePath = filePath.Trim();
            if (IsValidFilePath(filePath) && Path.IsPathRooted(filePath))
            {
                var fullPathUri = new Uri(Path.GetFullPath(filePath));
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
