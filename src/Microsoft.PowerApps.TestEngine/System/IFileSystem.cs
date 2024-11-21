// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerApps.TestEngine.System
{
    /// <summary>
    /// Wrapper for any System.IO methods needed
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Creates directory
        /// </summary>
        /// <param name="directoryName">Directory name</param>
        public void CreateDirectory(string directoryName);

        /// <summary>
        /// Checks if a directory exists
        /// </summary>
        /// <param name="directoryName">Directory name</param>
        public bool Exists(string directoryName);

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        /// <param name="directoryName">Directory name</param>
        public bool FileExists(string fileName);

        /// <summary>
        /// Gets files in a directory
        /// </summary>
        /// <param name="directoryName">Directory name</param>
        /// <returns>Array of files in directory</returns>
        public string[] GetFiles(string directoryName);

        /// <summary>
        /// Gets files in a directory matching search pattern
        /// </summary>
        /// <param name="directoryName">Directory name</param>
        /// <param name="searchPattern">Directory name</param>
        /// <returns>Array of files in directory</returns>
        public string[] GetFiles(string directoryName, string searchPattern);

        /// <summary>
        /// Writes text to file
        /// </summary>
        /// <param name="filePath">File to write to</param>
        /// <param name="text">Text to put in file</param>
        public void WriteTextToFile(string filePath, string text);

        /// <summary>
        /// Writes text to file
        /// </summary>
        /// <param name="filePath">File to write to</param>
        /// <param name="text">Array of text to put in file</param>
        public void WriteTextToFile(string filePath, string[] text);

        /// <summary>
        /// Checks whether file path is valid
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>True if it is valid</returns>
        public bool IsValidFilePath(string filePath);

        /// <summary>
        /// Reads all text in a file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Text in file</returns>
        public string ReadAllText(string filePath);

        /// <summary>
        /// Removes characters that are not allowed in file names
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>File name with all valid characters</returns>
        public string RemoveInvalidFileNameChars(string fileName);

        /// <summary>
        /// Returns temporary path for local machine
        /// </summary>
        /// <returns>Location of the temporary path</returns>
        public string GetTempPath();

        /// <summary>
        /// Returns default root locaiton of all testengine artifacts
        /// </summary>
        /// <returns>Location of the root folder for test engine output and log files</returns>
        public string GetDefaultRootTestEngine();

        /// <summary>
        /// Checks whether file path is permitted for write operations
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>True if it is permitted</returns>
        public bool IsWritePermittedFilePath(string filePath);
    }
}
