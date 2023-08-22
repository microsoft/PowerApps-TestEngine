// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.RegularExpressions;

namespace Microsoft.PowerApps.TestEngine.System
{
    /// <summary>
    /// Wrapper for any System.IO methods needed
    /// </summary>
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

        public void WriteTextToFile(string filePath, string text)
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

        public void WriteTextToFile(string filePath, string[] text)
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

        public bool IsValidFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }
            if (filePath.Length < 3)
            {
                return false;
            }
            string invalidPathChars = new string(Path.GetInvalidPathChars());
            Regex invalidPathCharsRegex = new Regex($"[{Regex.Escape($"{invalidPathChars}:?*\"")}]");
            if (invalidPathCharsRegex.IsMatch(filePath.Substring(3, filePath.Length - 3)))
            {
                return false;
            }
            return true;
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public string RemoveInvalidFileNameChars(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
    }
}
