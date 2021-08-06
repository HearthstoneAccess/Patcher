using System;
using System.Collections.Generic;
using System.IO;

namespace Patching
{
    public class PatchingUtils
    {
        // HS files
        public static string HEARTHSTONE_DIR_NAME = "Hearthstone";
        private static string HEARTHSTONE_DATA_DIR_NAME = "Hearthstone_Data";
        private static string HEARTHSTONE_MANAGED_DIR_RELATIVE_PATH = Path.Combine(HEARTHSTONE_DATA_DIR_NAME, "Managed");
        private static string HEARTHSTONE_ASSEMBLY_CSHARP_DLL_RELATIVE_PATH = Path.Combine(HEARTHSTONE_MANAGED_DIR_RELATIVE_PATH, "Assembly-CSharp.dll");
        private static string HEARTHSTONE_PRODUCT_DB_FILE_NAME = ".product.db";

        // HSA files
        private static string HSA_ACCESSIBILITY_DIR_NAME = "Accessibility";
        private static string HSA_VERSION_FILE_NAME = "hsa-version";

        private static int SEARCH_DEPTH = 3; // Should be more than enough for most and complexity grows steeply beyond this

        public static List<string> FindHearthstoneCandidateDirectories()
        {
            var ret = new List<string>();

            var drives = DriveInfo.GetDrives();

            var candidateDirs = new List<string>();

            // Find all dirs named Hearthstone in all drives
            foreach (var drive in drives)
            {
                candidateDirs.AddRange(FindSubdirectories(drive.RootDirectory, HEARTHSTONE_DIR_NAME, SEARCH_DEPTH));
            }

            // Filter out the Hearthstone dirs that appear to be real HS dirs
            foreach (var dir in candidateDirs)
            {
                if (IsHearthstoneDirectory(dir))
                {
                    ret.Add(dir);
                }
            }

            return ret;
        }

        public static bool IsHearthstoneDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            var assemblyCSharpDllPath = GetAssemblyCSharpDllPath(path);
            var productDbPath = GetProductDbPath(path);

            return File.Exists(assemblyCSharpDllPath) && File.Exists(productDbPath);
        }

        public static string GetAssembliesPath(string hsDir)
        {
            return Path.Combine(hsDir, HEARTHSTONE_MANAGED_DIR_RELATIVE_PATH);
        }

        private static string GetAssemblyCSharpDllPath(string hsDir)
        {
            return Path.Combine(hsDir, HEARTHSTONE_ASSEMBLY_CSHARP_DLL_RELATIVE_PATH);
        }

        public static string GetProductDbPath(string hsDir)
        {
            return Path.Combine(hsDir, HEARTHSTONE_PRODUCT_DB_FILE_NAME);
        }

        public static string GetHearthstoneAccessVersionPath(string hsDir)
        {
            return Path.Combine(hsDir, HSA_ACCESSIBILITY_DIR_NAME, HSA_VERSION_FILE_NAME);
        }

        private static IEnumerable<string> FindSubdirectories(DirectoryInfo dir, string name, int searchDepth)
        {
            if (searchDepth <= 0)
            {
                yield break;
            }

            DirectoryInfo[] childDirs = new DirectoryInfo[0];

            try
            {
                childDirs = dir.GetDirectories();
            }
            catch
            {
                // Protect against permissions etc
            }

            foreach (var childDir in childDirs)
            {
                if (childDir.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return childDir.FullName;
                }

                if (!childDir.Attributes.HasFlag(FileAttributes.Hidden)) // TODO: Find a nice way to handle potential symlinks and blacklist Windows dirs etc
                {
                    // Do both as we may have e.g. Hearthstone/Hearthstone/etc. in secondary drives
                    foreach (var grandchildDir in FindSubdirectories(childDir, name, searchDepth - 1))
                    {
                        yield return grandchildDir;
                    }
                }
            }
        }
    }
}
