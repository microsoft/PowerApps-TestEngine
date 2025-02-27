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
    public class FileSystemTests : IDisposable
    {
        private FileSystem fileSystem;
        private string testFileName;
        private string testFolderName;

        public FileSystemTests()
        {
            fileSystem = new FileSystem();
            testFileName = string.Empty;
            testFolderName = string.Empty;
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(testFileName) && File.Exists(testFileName))
            {
                File.Delete(testFileName);
            }
            if (!string.IsNullOrEmpty(testFolderName) && Directory.Exists(testFolderName))
            {
                Directory.Delete(testFolderName, true);
                testFolderName = "";
            }
        }

        [Theory]
        [InlineData("file.json", true)]
        [InlineData("C:/folder/file.yaml", true)]
        [InlineData("C:\\folder\\file.dat", false)]
        [InlineData("C:\\folder", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("C:/fold:er", false)]
        [InlineData("C:/fold>er/fg", false)]
        [InlineData("C:/folder/f>g", false)]
        [InlineData("C:/folder/f:g", false)]
        [InlineData("C:/folder/fg/", false)]
        [InlineData("../folder/fg", false)]
        [InlineData("../folder/f:g", false)]
        [InlineData("\\\\RandomUNC", false)]
        [InlineData(@"\\?\C:\folder", false)]
        [InlineData(@"C:\CON\a.cfx", false)]
        [InlineData(@"C:\a\a.json.", true)] //it normalizes path to not have . at the end
        [InlineData(@"C:\CON", false)]
        [InlineData(@"C:\folder\AUX", false)]
        [InlineData(@"C:\folder\PRN.yaml", false)]
        [InlineData(@"C:\WINDOWS\system32", false)]
        [InlineData(@"C:\folder\file.com", false)]
        public void CanAccessFilePathTest_Windows(string? filePath, bool expectedResult)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = fileSystem.CanAccessFilePath(filePath);
                Assert.Equal(expectedResult, result);
            }
        }

        [Theory]
        [InlineData(@"abc.json", true)]
        [InlineData(@"/root/", false)]
        public void CanAccessFilePathTest_Linux(string? filePath, bool expectedResult)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var result = fileSystem.CanAccessFilePath(filePath);
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
            var validPath = Path.Combine(fileSystem.GetDefaultRootTestEngine(), "testfile.json");
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
            var relativePath = @"..\testfile.json";
            Assert.False(fileSystem.IsWritePermittedFilePath(relativePath));
        }

        [Fact]
        public void IsWritePermittedFilePath_InvalidRootedPath_ReturnsFalse()
        {
            var invalidPath = Path.Combine(fileSystem.GetTempPath(), "invalidfolder", "testfile.json");
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
            var pathWithParentTraversal = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.yaml";
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
            var invalidFilePath = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.json";
            var exception = Assert.Throws<InvalidOperationException>(() => fileSystem.WriteTextToFile(invalidFilePath, ""));
            Assert.Contains(Path.GetFullPath(invalidFilePath), exception.Message);
        }

        [Fact]
        public void WriteTextToFile_ArrayText_UnpermittedFilePath_ThrowsInvalidOperationException()
        {
            var invalidFilePath = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.cfx";
            var exception = Assert.Throws<InvalidOperationException>(() => fileSystem.WriteTextToFile(invalidFilePath, new string[] { "This should fail." }));
            Assert.Contains(Path.GetFullPath(invalidFilePath), exception.Message);
        }

        [Fact]
        public void WriteFile_ArrayText_UnpermittedFilePath_ThrowsInvalidOperationException()
        {
            var invalidFilePath = fileSystem.GetDefaultRootTestEngine() + @"..\testfile.json";
            var exception = Assert.Throws<InvalidOperationException>(() => fileSystem.WriteFile(invalidFilePath, Encoding.UTF8.GetBytes("This should fail.")));
            Assert.Contains(Path.GetFullPath(invalidFilePath), exception.Message);
        }

        [Theory]
        [MemberData(nameof(DirectoryPathTestDataWindows))]
        public void CanAccessDirectoryPath_Windows_ReturnsValidity(string path, bool validity)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(fileSystem.CanAccessDirectoryPath(path), validity);
            }
        }

        [Theory]
        [MemberData(nameof(DirectoryPathTestDataLinux))]
        public void CanAccessDirectoryPath_Linux_OSX_ReturnsValidity(string path, bool validity)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.Equal(fileSystem.CanAccessDirectoryPath(path), validity);
            }
        }

        public static IEnumerable<object[]> DirectoryPathTestDataWindows()
        {
            return new List<object[]>
            {
                new object[] { null, false },
                new object[] { @"", false }, // Empty string
                new object[] { @"   ", false }, // Whitespace string
                new object[] { @"C:\Valid\Directory", true }, // Valid absolute Windows path
                new object[] { @"relative\directory", true }, // Valid relative Windows path
                new object[] { @"\\network\share\directory", false }, // UNC path (network)
                new object[] { @"C:\ ", false }, // Ends with a space
                new object[] { @"C:\.", false }, // Ends with a period
                new object[] { @"C:\Valid\..\Directory", true }, // Valid path with `..` (resolved)
                new object[] { @"\\?\C:\Very\Long\Path", true }, // Long path prefix
                new object[] { @"C:\folder\" + new string('a', 250), true }, // Valid length
                new object[] { @"C:\フォルダー", true }, // Valid Unicode path
                new object[] { @"file:///C:/Valid/Directory", true },
                new object[] { @"C:\folder\subfolder\NUL", false }, // Reserved name deep in path
                new object[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + Path.DirectorySeparatorChar + "test", false },
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
        [InlineData(@"C:\folder\my.folder", false)]
        [InlineData(@"C:\folder\my.context", false)]
        [InlineData(@"C:\folder\subfolder\file", false)]
        [InlineData(@"C:\myfolder\subfolder", false)]
        [InlineData(@"C:\myfolderCON\subfolderext", false)]
        [InlineData(@"C:\myfolder CON\subfolderext", false)]
        [InlineData(@"C:\folder\file.com", false)]
        [InlineData("\\\\RandomUNC", true)]

        // Invalid cases (reserved names in path)
        //[InlineData(@"C:\CON", true)]              // Reserved root folder
        //[InlineData(@"C:\folder\AUX", true)]       // Reserved folder
        //[InlineData(@"C:\folder\PRN.txt", false)]
        //[InlineData(@"C:\folder\COM1", true)]      // Reserved COM name
        [InlineData(@"C:\LPT2\file.txt", true)]    // Reserved folder in path
        [InlineData(@"C:\CLOCK$\file.txt", true)]  // Reserved CLOCK$ folder
        //[InlineData(@"C:\myfolder\COM9.file", false)]
        //[InlineData(@"C:\myfolder\COM9.file.", true)] //autonormalized
        //[InlineData(@"C:\myfolder\COM9.file ", true)] //autonormalized
        //[InlineData(@"C:\myfolder \COM9.file", true)]
        //[InlineData(@"C:\myfolder.\COM9.file ", true)] //autonormalized

        public void WindowsReservedLocationExistsInPath_ReturnsValidity(string fileFullPath, bool reservedExists)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Equal(fileSystem.WindowsReservedLocationExistsInPath(fileFullPath), reservedExists);
            }
        }

        [Theory]
        [InlineData(@"/usr/local/bin", true)]
        public void LinuxReservedLocationExistsInPath_Linux_ReturnsValidity(string fileFullPath, bool reservedExists)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Equal(fileSystem.LinuxReservedLocationExistsInPath(fileFullPath), reservedExists);
            }
        }

        [Theory]
        [InlineData(@"/usr/local/bin", true)]
        public void OsxReservedLocationExistsInPath_OSX_ReturnsValidity(string fileFullPath, bool reservedExists)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.Equal(fileSystem.OsxReservedLocationExistsInPath(fileFullPath), reservedExists);
            }
        }

        //this test is valid for windows, linux and osx
        [Fact]
        public void IsPermittedOS_ReturnsTrue()
        {
            Assert.True(fileSystem.IsPermittedOS());
        }

        [Theory]
        [InlineData("validFile.txt", true)] // Valid file name
        [InlineData("CON", false)] // Reserved name without extension
        [InlineData("test.", false)] // Trailing period
        [InlineData("test ", false)] // Trailing space
        [InlineData("AUX.txt", false)] // Reserved name with extension
        [InlineData("file.txt", true)] // Valid file name
        [InlineData("COM1", false)] // Reserved name without extension
        [InlineData("notReserved.txt", true)] // Valid file name
        [InlineData("file..txt", false)] // Double dot
        [InlineData(".hiddenFile", true)] // Hidden file (dot at the start)
        [InlineData("LPT1.txt", false)] // Reserved name with extension
        [InlineData("example", true)] // Valid file name
        public void TestIsValidWindowsFileName(string fileName, bool expectedValidity)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var result = fileSystem.IsValidWindowsFileName(fileName);
                Assert.Equal(expectedValidity, result);
            }
        }

        [Fact]
        public void CanDeleteJsonFile()
        {
            // Arrange
            testFileName = Path.Combine(fileSystem.GetDefaultRootTestEngine(), "test.json");
            if (!Directory.Exists(Path.GetDirectoryName(testFileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(testFileName));
            }
            File.WriteAllText(testFileName, "data");

            // Act
            fileSystem.Delete(testFileName);

            // Assert
            Assert.False(File.Exists(testFileName));
        }


        [Theory]
        [InlineData("test.yaml")]
        [InlineData("test.csx")]
        public void CannotDeleteOtherFiles(string fileName)
        {
            // Arrange
            testFileName = Path.Combine(fileSystem.GetDefaultRootTestEngine(), fileName);
            if (!Directory.Exists(Path.GetDirectoryName(testFileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(testFileName));
            }

            File.WriteAllText(testFileName, "data");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => fileSystem.Delete(testFileName));
            Assert.True(File.Exists(testFileName));
            File.Delete(testFileName);
        }

        [Fact]
        public void CanDeleteFolder()
        {
            // Arrange
            testFolderName = Path.Combine(fileSystem.GetDefaultRootTestEngine(), ".TestDir");
            if (!Directory.Exists(testFolderName))
            {
                Directory.CreateDirectory(testFolderName);
            }

            // Act
            fileSystem.DeleteDirectory(testFolderName);

            // Assert
            Assert.False(Directory.Exists(testFolderName));
        }

        [Fact]
        public void CannotDeleteFolder()
        {
            Assert.Throws<InvalidOperationException>(() => fileSystem.DeleteDirectory(Path.Combine(fileSystem.GetTempPath(), Guid.NewGuid().ToString())));
        }
    }
}
