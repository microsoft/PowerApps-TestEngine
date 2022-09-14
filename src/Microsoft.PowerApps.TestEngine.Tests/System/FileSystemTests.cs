﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestEngine.System;
using Xunit;

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
        [InlineData("C:/fold:er", false)]
        public void IsValidFilePathTest(string filePath, bool expectedResult)
        {
            var fileSystem = new FileSystem();
            var result = fileSystem.IsValidFilePath(filePath);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("file.txt", "file.txt")]
        [InlineData("C:/folder/file.txt", "Cfolderfile.txt")]
        [InlineData("C:\\folder\\file", "Cfolderfile")]
        [InlineData("", "")]
        [InlineData("tem|<p", "temp")]
        public void RemoveInvalidFileNameCharsTest(string inputFileName, string expectedFileName)
        {
            var fileSystem = new FileSystem();
            var result = fileSystem.RemoveInvalidFileNameChars(inputFileName);
            Assert.Equal(expectedFileName, result);
        }
    }
}
