// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

namespace Microsoft.PowerApps.TestEngine.System
{
    /// <summary>
    /// Wrapper for any System.IO methods needed
    /// </summary>
    [Export(typeof(IFileSystem))]
    public class FileSystem : IFileSystem
    {
        public readonly string[] windowsReservedNames = { "CON", "PRN", "AUX", "NUL", "CLOCK$", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        public void CreateDirectory(string directoryName)
        {
            directoryName = Path.GetFullPath(directoryName);
            if (CanAccessDirectoryPath(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or write to path: '{0}' not permitted.", directoryName));
            }
        }

        public bool Exists(string directoryName)
        {
            directoryName = Path.GetFullPath(directoryName);
            if (CanAccessDirectoryPath(directoryName))
            {
                return Directory.Exists(directoryName);
            }
            return false;
        }

        public string[] GetDirectories(string path)
        {
            path = Path.GetFullPath(path);
            if (CanAccessDirectoryPath(path))
            {
                var directories = Directory.GetDirectories(path, "*.*", searchOption: SearchOption.AllDirectories).Where(CanAccessFilePath);
                return directories.ToArray();
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or read from path: '{0}' not permitted.", path));
            }
        }

        public bool FileExists(string fileName)
        {
            fileName = Path.GetFullPath(fileName);
            if (CanAccessFilePath(fileName))
            {
                return File.Exists(fileName);
            }
            return false;
        }

        public string[] GetFiles(string directoryName)
        {
            directoryName = Path.GetFullPath(directoryName);
            if (CanAccessDirectoryPath(directoryName))
            {
                var files = Directory.GetFiles(directoryName, "*.*", searchOption: SearchOption.AllDirectories).Where(CanAccessFilePath);
                return files.ToArray();
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or read from path: '{0}' not permitted.", directoryName));
            }
        }

        public string[] GetFiles(string directoryName, string searchPattern)
        {
            directoryName = Path.GetFullPath(directoryName);
            if (CanAccessDirectoryPath(directoryName))
            {
                var files = Directory.GetFiles(directoryName, searchPattern).Where(CanAccessFilePath);
                return files.ToArray();
            }
            else
            {
                throw new InvalidOperationException(string.Format("Path invalid or read from path: '{0}' not permitted.", directoryName));
            }
        }

        public void WriteTextToFile(string filePath, string text, bool overwrite = false)
        {
            filePath = Path.GetFullPath(filePath);
            if (IsWritePermittedFilePath(filePath))
            {
                if (File.Exists(filePath))
                {
                    if (!overwrite)
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
                    File.WriteAllText(filePath, text);
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Write to path: '{0}' not permitted, ensure path is rooted in base TestEngine path.", filePath));
            }
        }

        public void WriteTextToFile(string filePath, string[] text)
        {
            filePath = Path.GetFullPath(filePath);
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
                throw new InvalidOperationException(string.Format("Write to path: '{0}' not permitted, ensure path is rooted in base TestEngine path.", filePath));
            }
        }

        public void WriteFile(string filePath, byte[] data)
        {
            filePath = Path.GetFullPath(filePath);
            if (IsWritePermittedFilePath(filePath))
            {
                File.WriteAllBytes(filePath, data);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Write to path: '{0}' not permitted, ensure path is rooted in base TestEngine path.", filePath));
            }
        }

        public string ReadAllText(string filePath)
        {
            filePath = Path.GetFullPath(filePath);
            if (CanAccessFilePath(filePath))
            {
                switch (Path.GetExtension(filePath).ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                    case ".bmp":
                        // Read the image file as bytes
                        byte[] imageBytes = File.ReadAllBytes(filePath);
                        // Encode the bytes to Base64
                        string encodedString = Convert.ToBase64String(imageBytes);
                        return encodedString;
                    default:
                        return File.ReadAllText(filePath);
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid file path '{0}'.", filePath));
            }
        }

        public string RemoveInvalidFileNameChars(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public string GetTempPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.GetTempPath();
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public string GetDefaultRootTestEngine()
        {
            return Path.Combine(GetTempPath(), "Microsoft", "TestEngine") + Path.DirectorySeparatorChar;
        }

        public bool IsPermittedOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return true;
            }
            return false;
        }

        public bool CanAccessDirectoryPath(string directoryPath)
        {
            try
            {
                if (!IsPermittedOS())
                {
                    return false;
                }
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    return false;
                }

                var fullPath = Path.GetFullPath(directoryPath);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return !WindowsReservedLocationExistsInPath(fullPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return !LinuxReservedLocationExistsInPath(fullPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return !OsxReservedLocationExistsInPath(fullPath);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool WindowsReservedLocationExistsInPath(string fullPath)
        {
            fullPath = Path.GetFullPath(fullPath);
            //check if its a network path if so fail
            var fullPathUri = new Uri(fullPath.StartsWith(@"\\?\") ? fullPath.Replace(@"\\?\", "") : fullPath, UriKind.Absolute);
            if (fullPathUri.IsUnc)
            {
                return true;
            }
            // Ensure the path ends with a directory separator
            if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !fullPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                fullPathUri = new Uri(fullPathUri.ToString().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, UriKind.Absolute);
            }

            //check if any of reserved base locations referred then fail
            IEnumerable<Uri> windowsRestrictedPaths = new List<Uri>
            {
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Windows) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + Path.DirectorySeparatorChar),

                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos) + Path.DirectorySeparatorChar),

                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow") + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") + Path.DirectorySeparatorChar),
                new Uri(Environment.GetFolderPath(Environment.SpecialFolder.Favorites) + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Searches") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Links") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft") + Path.DirectorySeparatorChar),
            };
            if (windowsRestrictedPaths.Any(baseUri => baseUri.IsBaseOf(fullPathUri)))
            {
                return true;
            }

            //check if any directory is not not valid format
            var root = Path.GetPathRoot(fullPath);
            if (root == null)
            {
                return true;
            }
            var restOfPath = fullPath.Substring(root.Length);
            var pathSegments = restOfPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).ToList();

            // Iterate over folder names
            foreach (var pathSegment in pathSegments)
            {
                //none of the folders should have invalid names
                if (string.IsNullOrWhiteSpace(pathSegment) || pathSegment.EndsWith(" ") || pathSegment.EndsWith("."))
                {
                    return true;
                }
                if (windowsReservedNames.Contains(pathSegment, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool LinuxReservedLocationExistsInPath(string fullPath)
        {
            fullPath = Path.GetFullPath(fullPath);

            if (fullPath.Equals("/"))
            {
                return true;
            }
            //check if its a network path if so fail
            var fullPathUri = new Uri(fullPath.StartsWith(@"\\?\") ? fullPath.Replace(@"\\?\", "") : fullPath, UriKind.Absolute);
            if (fullPathUri.IsUnc)
            {
                return true;
            }
            // Ensure the path ends with a directory separator
            if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !fullPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                fullPathUri = new Uri(fullPathUri.ToString().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, UriKind.Absolute);
            }

            IEnumerable<Uri> LinuxRestrictedPaths = new List<Uri>
            {
                new Uri("/bin/", UriKind.Absolute),
                new Uri("/sbin/", UriKind.Absolute),
                new Uri("/boot/", UriKind.Absolute),
                new Uri("/root/", UriKind.Absolute),
                new Uri("/dev/", UriKind.Absolute),
                new Uri("/etc/", UriKind.Absolute),
                //new Uri("/home/", UriKind.Absolute),
                new Uri("/lib/", UriKind.Absolute),
                new Uri("/lib64/", UriKind.Absolute),
                new Uri("/mnt/", UriKind.Absolute),
                new Uri("/opt/", UriKind.Absolute),
                new Uri("/proc/", UriKind.Absolute),
                new Uri("/run/", UriKind.Absolute),
                new Uri("/srv/", UriKind.Absolute),
                new Uri("/sys/", UriKind.Absolute),
                //new Uri("/tmp/", UriKind.Absolute),
                new Uri("/usr/", UriKind.Absolute),
                new Uri("/var/", UriKind.Absolute),
                new Uri(@"/media/", UriKind.Absolute),
                new Uri(@"/lost+found/", UriKind.Absolute),
                new Uri(@"/snap/", UriKind.Absolute),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".ssh") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".config") + Path.DirectorySeparatorChar),
                new Uri("/bin/", UriKind.Absolute),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".gnupg") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".cache") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".docker") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".kube") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".npm") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".gem") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".m2") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".terraform.d") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".aws") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".azure") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".google") + Path.DirectorySeparatorChar),
            };
            if (LinuxRestrictedPaths.Any(baseUri => baseUri.IsBaseOf(fullPathUri)))
            {
                return true;
            }
            return false;
        }

        public bool OsxReservedLocationExistsInPath(string fullPath)
        {
            fullPath = Path.GetFullPath(fullPath);

            if (fullPath.Equals("/"))
            {
                return true;
            }
            //check if its a network path if so fail
            var fullPathUri = new Uri(fullPath.StartsWith(@"\\?\") ? fullPath.Replace(@"\\?\", "") : fullPath, UriKind.Absolute);
            if (fullPathUri.IsUnc)
            {
                return true;
            }
            // Ensure the path ends with a directory separator
            if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !fullPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                fullPathUri = new Uri(fullPathUri.ToString().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, UriKind.Absolute);
            }

            IEnumerable<Uri> OsxRestrictedPaths = new List<Uri>
            {
                new Uri("/bin/", UriKind.Absolute),
                new Uri("/sbin/", UriKind.Absolute),
                new Uri("/boot/", UriKind.Absolute),
                new Uri("/root/", UriKind.Absolute),
                new Uri("/dev/", UriKind.Absolute),
                new Uri("/etc/", UriKind.Absolute),
                //new Uri("/home/", UriKind.Absolute),
                new Uri("/lib/", UriKind.Absolute),
                new Uri("/lib64/", UriKind.Absolute),
                new Uri("/mnt/", UriKind.Absolute),
                new Uri("/opt/", UriKind.Absolute),
                new Uri("/proc/", UriKind.Absolute),
                new Uri("/run/", UriKind.Absolute),
                new Uri("/srv/", UriKind.Absolute),
                new Uri("/sys/", UriKind.Absolute),
                //new Uri("/tmp/", UriKind.Absolute),
                new Uri("/usr/", UriKind.Absolute),
                new Uri("/var/", UriKind.Absolute),
                new Uri(@"/media/", UriKind.Absolute),
                new Uri(@"/lost+found/", UriKind.Absolute),
                new Uri(@"/snap/", UriKind.Absolute),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".ssh") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".config") + Path.DirectorySeparatorChar),
                new Uri("/bin/", UriKind.Absolute),
                new Uri("/private/", UriKind.Absolute),
                new Uri("/Library/", UriKind.Absolute),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Library") + Path.DirectorySeparatorChar),

                new Uri("/System/", UriKind.Absolute),
                new Uri("/Applications/", UriKind.Absolute),
                new Uri("/Volumes/", UriKind.Absolute),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".gnupg") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".cache") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".docker") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".kube") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".npm") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".gem") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".m2") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".terraform.d") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".aws") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".azure") + Path.DirectorySeparatorChar),
                new Uri(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".google") + Path.DirectorySeparatorChar),
            };
            if (OsxRestrictedPaths.Any(baseUri => baseUri.IsBaseOf(fullPathUri)))
            {
                return true;
            }
            return false;
        }

        public bool CanAccessFilePath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }
                var fullFilePath = Path.GetFullPath(filePath);
                var directoryName = Path.GetDirectoryName(fullFilePath);
                if (!CanAccessDirectoryPath(directoryName))
                {
                    return false;
                }

                var fileName = Path.GetFileName(fullFilePath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    return false;
                }
                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return false;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (!IsValidWindowsFileName(fileName))
                    {
                        return false;
                    }
                }

                //if it belongs to writable base location allow all file types otherwise only json, yaml and csx
                var fullPathUri = new Uri(fullFilePath.StartsWith(@"\\?\") ? fullFilePath.Replace(@"\\?\", "") : fullFilePath);
                var baseUri = new Uri(GetDefaultRootTestEngine(), UriKind.Absolute);
                if (!baseUri.IsBaseOf(fullPathUri))
                {
                    var ext = Path.GetExtension(fileName);
                    if (
                        !(
                            ext.Equals(".yml", StringComparison.OrdinalIgnoreCase)
                            ||
                            ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase)
                            ||
                            ext.Equals(".json", StringComparison.OrdinalIgnoreCase)
                            ||
                            ext.Equals(".csx", StringComparison.OrdinalIgnoreCase)
                            ||
                            ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        return false;
                    }
                }
                //just get this to check if its a valid file path, if its not then it throws 
                var g = new FileInfo(fullFilePath).IsReadOnly;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidWindowsFileName(string fileName)
        {
            // Check for trailing period or space
            if (fileName.EndsWith(" ") || fileName.EndsWith("."))
                return false;

            // Reserved names in Windows
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                return false;
            }
            if (windowsReservedNames.Contains(nameWithoutExtension, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            var ext = Path.GetExtension(fileName);
            if (windowsReservedNames.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public bool IsWritePermittedFilePath(string filePath)
        {
            try
            {
                if (CanAccessFilePath(filePath) && Path.IsPathRooted(filePath))
                {
                    var fullPath = Path.GetFullPath(filePath);
                    var fullPathUri = new Uri(fullPath.StartsWith(@"\\?\") ? fullPath.Replace(@"\\?\", "") : fullPath);
                    var baseUri = new Uri(GetDefaultRootTestEngine(), UriKind.Absolute);
                    if (baseUri.IsBaseOf(fullPathUri))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Delete(string fileName)
        {
            if (!FileExists(fileName))
            {
                return;
            }

            if (!IsWritePermittedFilePath(fileName))
            {
                return;
            }

            if (Path.GetExtension(fileName) != ".json")
            {
                throw new InvalidOperationException();
            }

            File.Delete(fileName);
        }

        public void DeleteDirectory(string directoryName)
        {
            directoryName = Path.GetFullPath(directoryName);
            if (CanAccessDirectoryPath(directoryName))
            {
                var fullPathUri = new Uri(directoryName.StartsWith(@"\\?\") ? directoryName.Replace(@"\\?\", "") : directoryName);
                var baseUri = new Uri(GetDefaultRootTestEngine(), UriKind.Absolute);
                if (baseUri.IsBaseOf(fullPathUri))
                {
                    Directory.Delete(directoryName, true);
                    return;
                }
            }
            throw new InvalidOperationException(string.Format("Path invalid or write to path: '{0}' not permitted.", directoryName));
        }
    }
}
