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

        public string[] GetFiles(string directoryName)
        {
            return Directory.GetFiles(directoryName);
        }

        public void WriteTextToFile(string filePath, string text)
        {
            File.WriteAllText(filePath, text);
        }

        public void WriteTextToFile(string filePath, string[] text)
        {
            File.WriteAllLines(filePath, text);
        }

        public bool IsValidFilePath(string filePath)
        {
            try
            {
                Path.GetFullPath(filePath);
            }
            catch
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
    }
}
