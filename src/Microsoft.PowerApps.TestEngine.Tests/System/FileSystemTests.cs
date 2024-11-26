// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.PowerApps.TestEngine.System;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.System
{
    public class FileSystemTests
    {
        private FileSystem fileSystem;
        public FileSystemTests()
        {
            fileSystem = new FileSystem();
        }

        [Theory]
        [InlineData("file.txt", true)]
        [InlineData("C:/folder/file.txt", true)]
        [InlineData("C:\\folder\\file", true)]
        [InlineData("C:\\folder", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("C:/fold:er", false)]
        [InlineData("C:/fold>er/fg", false)]
        [InlineData("C:/folder/f>g", false)]
        [InlineData("C:/folder/f:g", false)]
        [InlineData("C:/folder/fg/", false)]
        [InlineData("../folder/fg", true)]
        [InlineData("../folder/f:g", false)]
        [InlineData("\\\\RandomUNC", false)]
        [InlineData(@"\\?\C:\folder", true)]
        [InlineData(@"C:\CON\a.txt", false)]
        [InlineData(@"C:\a\a.txt.", false)]
        [InlineData(@"C:\CON", false)]
        [InlineData(@"C:\folder\AUX", false)]
        [InlineData(@"C:\folder\PRN.txt", false)]
        [InlineData(@"C:\\WINDOWS\\system32", false)]
        public void IsValidFilePathTest_Windows(string? filePath, bool expectedResult)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = fileSystem.IsValidFilePath(filePath);
                Assert.Equal(expectedResult, result);
            }
        }

        [Theory]
        [InlineData(@"/usr/local/abc.txt", true)]
        [InlineData(@"/root/", false)]
        public void IsValidFilePathTest_Linux(string? filePath, bool expectedResult)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var result = fileSystem.IsValidFilePath(filePath);
                Assert.Equal(expectedResult, result);
            }
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
            var result = fileSystem.RemoveInvalidFileNameChars(inputFileName);
            Assert.Equal(expectedFileName, result);
        }

        [Fact]
        public void IsWritePermittedFilePath_ValidRootedPath_ReturnsTrue()
        {
            var validPath = Path.Combine(fileSystem.GetDefaultRootTestEngine(), "testfile.txt");
            Assert.True(fileSystem.IsWritePermittedFilePath(validPath));
        }

        [Fact]
        public void IsWritePermittedFilePath_SameAsRootedPath_ReturnsFalse()
        {
            var validPath = Path.Combine(fileSystem.GetDefaultRootTestEngine(), "");
            Assert.False(fileSystem.IsWritePermittedFilePath(validPath));
        }

        [Fact]
        public void IsWritePermittedFilePath_RelativePath_ReturnsFalse()
        {
            var relativePath = @"..\testfile.txt";
            Assert.False(fileSystem.IsWritePermittedFilePath(relativePath));
        }

        [Fact]
        public void IsWritePermittedFilePath_InvalidRootedPath_ReturnsFalse()
        {
            var invalidPath = Path.Combine(fileSystem.GetTempPath(), "invalidfolder", "testfile.txt");
            Assert.False(fileSystem.IsWritePermittedFilePath(invalidPath));
        }

        [Fact]
        public void IsWritePermittedFilePath_NullPath_ReturnsFalse()
        {
            Assert.False(fileSystem.IsWritePermittedFilePath(null));
        }

        [Fact]
        public void IsWritePermittedFilePath_ValidPathWithParentDirectoryTraversal_ReturnsFalse()
        {
            var pathWithParentTraversal = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.txt";
            Assert.False(fileSystem.IsWritePermittedFilePath(pathWithParentTraversal));
        }

        [Fact]
        public void IsWritePermittedFilePath_UNCPath_ReturnsFalse()
        {
            var validPath = "\\\\RandomUNC";
            Assert.False(fileSystem.IsWritePermittedFilePath(validPath));
        }

        [Fact]
        public void WriteTextToFile_UnpermittedFilePath_ThrowsInvalidOperationException()
        {
            var invalidFilePath = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.txt";
            var exception = Assert.Throws<InvalidOperationException>(() => fileSystem.WriteTextToFile(invalidFilePath, ""));
            Assert.Contains(invalidFilePath, exception.Message);
        }

        [Fact]
        public void WriteTextToFile_ArrayText_UnpermittedFilePath_ThrowsInvalidOperationException()
        {
            var invalidFilePath = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.txt";
            var exception = Assert.Throws<InvalidOperationException>(() => fileSystem.WriteTextToFile(invalidFilePath, new string[] { "This should fail." }));
            Assert.Contains(invalidFilePath, exception.Message);
        }

        [Fact]
        public void WriteFile_ArrayText_UnpermittedFilePath_ThrowsInvalidOperationException()
        {
            var invalidFilePath = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.txt";
            var exception = Assert.Throws<InvalidOperationException>(() => fileSystem.WriteFile(invalidFilePath, Encoding.UTF8.GetBytes("This should fail.")));
            Assert.Contains(invalidFilePath, exception.Message);
        }

        [Theory]
        [MemberData(nameof(DirectoryPathTestDataWindows))]
        public void IsNonUNCDirectoryPath_Windows_ReturnsValidity(string path, bool validity)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(fileSystem.IsNonUNCDirectoryPath(path), validity);
            }
        }

        [Theory]
        [MemberData(nameof(DirectoryPathTestDataLinux))]
        public void IsNonUNCDirectoryPath_Linux_ReturnsValidity(string path, bool validity)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Equal(fileSystem.IsNonUNCDirectoryPath(path), validity);
            }
        }

        public static IEnumerable<object[]> DirectoryPathTestDataWindows()
        {
            return new List<object[]>
            {
                new object[] { @"C:\Valid\Directory", true }, // Valid absolute Windows path
                new object[] { @"relative\directory", true }, // Valid relative Windows path
                new object[] { @"\\network\share\directory", false }, // UNC path (network)
                new object[] { @"C:\ ", false }, // Ends with a space
                new object[] { @"C:\.", false }, // Ends with a period
                new object[] { @"", false }, // Empty string
                new object[] { @"   ", false }, // Whitespace string
                new object[] { @"C:\Valid\..\Directory", true }, // Valid path with `..` (resolved)
                new object[] { @"\\?\C:\Very\Long\Path", true }, // Long path prefix
                new object[] { @"C:\folder\" + new string('a', 250), true }, // Valid length
                new object[] { @"C:\フォルダー", true }, // Valid Unicode path
                new object[] { @"file:///C:/Valid/Directory", true }, // Valid Unicode path
            };
        }

        public static IEnumerable<object[]> DirectoryPathTestDataLinux()
        {
            return new List<object[]>
            {
                new object[] { @"/valid/directory", true }, // Valid absolute Linux path
                new object[] { @"./relative/directory", true }, // Valid relative Linux path
                new object[] { @"\\network\share\directory", false }, // UNC path (network)
                new object[] { @"", false }, // Empty string
                new object[] { @"   ", false }, // Whitespace string
                new object[] { @"../relative/dir", true }, // relative Linux path
            };
        }

        [Theory]
        // Valid cases (no reserved names)
        [InlineData(@"C:\folder\my.file", false)]
        [InlineData(@"C:\folder\my.context", false)]
        [InlineData(@"C:\folder\subfolder\file.txt", false)]
        [InlineData(@"C:\myfolder\subfolder.ext", false)]
        [InlineData(@"C:\myfolderCON\subfolder.ext", false)]
        [InlineData(@"C:\myfolder CON\subfolder.ext", false)]
        [InlineData(@"C:\folder\file.com", false)] // Extension should not match reserved names

        // Invalid cases (reserved names in path)
        [InlineData(@"C:\CON", true)]              // Reserved root folder
        [InlineData(@"C:\folder\AUX", true)]       // Reserved folder
        [InlineData(@"C:\folder\PRN.txt", true)]   // Reserved file name (extension ignored)
        [InlineData(@"C:\folder\COM1", true)]      // Reserved COM name
        [InlineData(@"C:\LPT2\file.txt", true)]    // Reserved folder in path
        [InlineData(@"C:\CLOCK$\file.txt", true)]  // Reserved CLOCK$ folder
        [InlineData(@"C:\folder\subfolder\NUL", true)] // Reserved name deep in path
        [InlineData(@"C:\myfolder\COM9.file", true)]   // File with reserved name

        public void WindowsReservedFolderNamesExistInFilePath_ReturnsValidity(string fileFullPath, bool reservedExists)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(fileSystem.WindowsReservedFolderNamesExistInFilePath(fileFullPath), reservedExists);
            }
        }

        [Theory]
        [InlineData(@"/usr/local/bin", false)]
        public void WindowsReservedFolderNamesExistInFilePath_OSLinx_ReturnsValidity(string fileFullPath, bool reservedExists)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Equal(fileSystem.WindowsReservedFolderNamesExistInFilePath(fileFullPath), reservedExists);
            }
        }
    }
}
