// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerApps.TestEngine.System;

namespace testengine.plugin.copilot
{
    /// <summary>
    /// Create an in memory file system to allow easy interaction with Test Engine state in a plugin
    /// </summary>
    public class InMemoryFileSystem : IFileSystem
    {
        /// <summary>
        /// List of files without path
        /// </summary>
        public Dictionary<string, string> Files { get; private set; } = new Dictionary<string, string>();

        // List of directories
        public List<string> Dirs { get; private set; } = new List<string>();

        public bool CanAccessDirectoryPath(string filePath)
        {
            throw new NotImplementedException();
        }

        public bool CanAccessFilePath(string filePath)
        {
            throw new NotImplementedException();
        }

        public void CreateDirectory(string directoryName)
        {
            var relativePath = RelativeDirectoryName(directoryName);
            if (!Dirs.Contains(relativePath))
            {
                Dirs.Add(relativePath);
            }
        }

        private string RelativeDirectoryName(string directoryName)
        {
            if (directoryName.StartsWith("file://")) {
                directoryName = directoryName.Replace("file://", "");
            }
            if (directoryName.StartsWith("file:\\\\"))
            {
                directoryName = directoryName.Replace("file:\\\\", "");
            }
            if (directoryName.IndexOf("TestEngineOutput") >= 0)
            {
                directoryName = directoryName.Substring(directoryName.IndexOf("TestEngineOutput"));
            }
            if (Path.HasExtension(directoryName))
            {
                directoryName = directoryName.Replace(Path.GetFileName(directoryName), "");
            }
            return directoryName;
        }

        public bool Exists(string directoryName)
        {
            return Dirs.Contains(RelativeDirectoryName(directoryName));
        }

        public bool FileExists(string fileName)
        {
            return Files.ContainsKey(RelativeFileName(fileName)) || Files.ContainsKey(Path.GetFileName(fileName));
        }

        public string GetDefaultRootTestEngine()
        {
            return "file://";
        }

        public string[] GetFiles(string directoryName)
        {
            var relativePath = RelativeDirectoryName(directoryName);

            return Files.Where(f => f.Key.StartsWith(relativePath)).Select(f => f.Key).ToArray();
        }

        private string RelativeFileName(string fileName)
        {
            if (fileName.StartsWith("file://"))
            {
                fileName = fileName.Replace("file://", "");
            }
            if (fileName.StartsWith("file:\\\\"))
            {
                fileName = fileName.Replace("file:\\\\", "");
            }
            if (fileName.IndexOf("TestEngineOutput") >= 0)
            {
                fileName = fileName.Substring(fileName.IndexOf("TestEngineOutput"));
            }
            return fileName;
        }

        public string[] GetFiles(string directoryName, string searchPattern)
        {
            var relativePath = RelativeDirectoryName(directoryName);

            return Files.Where(f => f.Key.StartsWith(relativePath)).Select(f => f.Key).ToArray();
        }

        public bool IsWritePermittedFilePath(string filePath)
        {
            throw new NotImplementedException();
        }

        public string ReadAllText(string filePath)
        {
            if (Files.ContainsKey(RelativeFileName(filePath)))
            {
                return Files[RelativeFileName(filePath)];
            }
            return Files[Path.GetFileName(filePath)];
        }

        public string RemoveInvalidFileNameChars(string fileName)
        {
            return fileName;
        }

        public void WriteFile(string filePath, byte[] data)
        {
            WriteTextToFile(filePath, Convert.ToBase64String(data));
        }

        public void WriteTextToFile(string filePath, string text, bool overwrite = false)
        {
            var relativePath = RelativeFileName(filePath);
            if (Files.ContainsKey(relativePath))
            {
                if (!overwrite)
                {
                    Files[relativePath] += text;
                }
                else
                {
                    Files[relativePath] = text;
                }   
            } 
            else
            {
                Files.Add(relativePath, text);
            }
            AddDirectoryIfNotExist(relativePath);
        }

        public void WriteTextToFile(string filePath, string[] text)
        {
            var relativePath = RelativeFileName(filePath);
            var lines = string.Join("\r\n", text);
            if (Files.ContainsKey(relativePath))
            {
                Files[relativePath] += lines;
            }
            else
            {
                Files.Add(relativePath, lines);
            }
            AddDirectoryIfNotExist(relativePath);
        }

        private void AddDirectoryIfNotExist(string relativePath)
        {
            var dirName = RelativeDirectoryName(relativePath);
            if (Dirs.Contains(dirName))
            {
                Dirs.Add(dirName);
            }
        }

        public void Delete(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
