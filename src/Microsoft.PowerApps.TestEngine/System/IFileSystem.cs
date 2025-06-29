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
        /// Gets directories in path
        /// </summary>
        /// <param name="path">Path name</param>
        /// <returns>Array of directories within the path</returns>
        public string[] GetDirectories(string path);

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
        /// <param name="overwrite">Determine if the contents of the file should be replaced with the text. Default value if <c>False</c></param>
        public void WriteTextToFile(string filePath, string text, bool overwrite = false);

        /// <summary>
        /// Writes text to file
        /// </summary>
        /// <param name="filePath">File to write to</param>
        /// <param name="text">Array of text to put in file</param>
        public void WriteTextToFile(string filePath, string[] text);

        /// <summary>
        /// Checks whether file path is accessible
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>True if it is accessible</returns>
        public bool CanAccessFilePath(string filePath);

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
        /// Writes a binary file to the file system imlementation
        /// </summary>
        /// <param name="filePath">The name of the file to create</param>
        /// <param name="data">The data to write</param>
        /// <returns></returns>
        public void WriteFile(string filePath, byte[] data);

        /// <summary>
        /// Returns default root location of all testengine artifacts
        /// </summary>
        /// <returns>Location of the root folder for test engine output and log files</returns>
        public string GetDefaultRootTestEngine();

        /// <summary>
        /// Checks whether file path is permitted for write operations
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>True if it is permitted</returns>
        public bool IsWritePermittedFilePath(string filePath);

        /// <summary>
        /// Checks whether directory path is accessible
        /// </summary>
        /// <param name="filePath">Path to check</param>
        /// <returns>True if it is valid</returns>
        public bool CanAccessDirectoryPath(string filePath);

        /// <summary>
        /// Delete a file from the system assuming file is in the permitted locations to delete from
        /// </summary>
        /// <param name="fileName">The file to delete</param>
        void Delete(string fileName);

        /// <summary>
        /// Delete a directory from the system assuming directory is in the permitted locations to delete from
        /// </summary>
        /// <param name="directoryName">The file to delete</param>
        void DeleteDirectory(string directoryName);
    }
}
