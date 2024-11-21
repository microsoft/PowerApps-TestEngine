// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System;
using Microsoft.PowerApps.TestEngine.System;
using Xunit;
using System.Linq;
using Moq;

namespace Microsoft.PowerApps.TestEngine.Tests.System
{
    public class FileSystemTests
    {
        [Theory]
        [InlineData("file.txt", true)]
        [InlineData("C:/folder/file.txt", true)]
        [InlineData("C:\\folder\\file", true)]
        [InlineData("C:\\folder", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("C:/fold>er/fg", false)]
        [InlineData("C:/folder/f>g", false)]
        [InlineData("C:/folder/f:g", false)]
        [InlineData("C:/folder/fg/", false)]
        [InlineData("../folder/fg", true)]
        [InlineData("../folder/f:g", false)]
        public void IsValidFilePathTest(string? filePath, bool expectedResult)
        {
            var fileSystem = new FileSystem();
            var result = fileSystem.IsValidFilePath(filePath);
            Assert.Equal(expectedResult, result);
        }

        // Some inline data is commented because invalid characters can vary by file system.
        // The commented data below should pass on your windows machine.
        [Theory]
        [InlineData("file.txt", "file.txt")]
        [InlineData("", "")]
        // [InlineData("C:/folder/file.txt", "Cfolderfile.txt")]
        // [InlineData("C:\\folder\\file", "Cfolderfile")]
        // [InlineData("tem|<p", "temp")]
        public void RemoveInvalidFileNameCharsTest(string inputFileName, string expectedFileName)
        {
            var fileSystem = new FileSystem();
            var result = fileSystem.RemoveInvalidFileNameChars(inputFileName);
            Assert.Equal(expectedFileName, result);
        }

        [Fact]
        public void IsWritePermittedFilePath_ValidRootedPath_ReturnsTrue()
        {
            var _fileSystem = new FileSystem();
            var validPath = Path.Combine(_fileSystem.GetDefaultRootTestEngine(), "testfile.txt");
            Assert.True(_fileSystem.IsWritePermittedFilePath(validPath));
        }
        [Fact]
        public void IsWritePermittedFilePath_SameAsRootedPath_ReturnsFalse()
        {
            var _fileSystem = new FileSystem();
            var validPath = Path.Combine(_fileSystem.GetDefaultRootTestEngine(), "");
            Assert.False(_fileSystem.IsWritePermittedFilePath(validPath));
        }


        [Fact]
        public void IsWritePermittedFilePath_RelativePath_ReturnsFalse()
        {
            var _fileSystem = new FileSystem();
            var relativePath = @"..\testfile.txt";
            Assert.False(_fileSystem.IsWritePermittedFilePath(relativePath));
        }

        [Fact]
        public void IsWritePermittedFilePath_InvalidRootedPath_ReturnsFalse()
        {
            var _fileSystem = new FileSystem();
            var invalidPath = Path.Combine(_fileSystem.GetTempPath(), "invalidfolder", "testfile.txt");
            Assert.False(_fileSystem.IsWritePermittedFilePath(invalidPath));
        }

        [Fact]
        public void IsWritePermittedFilePath_NullPath_ReturnsFalse()
        {
            var _fileSystem = new FileSystem();
            Assert.False(_fileSystem.IsWritePermittedFilePath(null));
        }

        [Fact]
        public void IsWritePermittedFilePath_ValidPathWithParentDirectoryTraversal_ReturnsFalse()
        {
            var _fileSystem = new FileSystem();
            var pathWithParentTraversal = _fileSystem.GetDefaultRootTestEngine() + Path.DirectorySeparatorChar + @"..\testfile.txt";
            Assert.False(_fileSystem.IsWritePermittedFilePath(pathWithParentTraversal));
        }

        [Fact]
        public void WriteTextToFile_UnpermittedFilePath_ThrowsInvalidOperationException()
        {
            var _fileSystem = new FileSystem();
            var invalidFilePath = "C:\\InvalidFolder\\testfile.txt";
            var exception = Assert.Throws<InvalidOperationException>(() => _fileSystem.WriteTextToFile(invalidFilePath, ""));
            Assert.Contains(invalidFilePath, exception.Message);
        }

        [Fact]
        public void WriteTextToFile_ArrayText_UnpermittedFilePath_ThrowsInvalidOperationException()
        {
            var _fileSystem = new FileSystem();
            var invalidFilePath = "C:\\InvalidFolder\\testfile.txt";
            var exception = Assert.Throws<InvalidOperationException>(() => _fileSystem.WriteTextToFile(invalidFilePath, new string[] { "This should fail." }));
            Assert.Contains(invalidFilePath, exception.Message);
        }
    }
}
