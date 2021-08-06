using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Patching
{
    public class Patcher
    {
        private static string PATCHER_VERSION = "1.0.1";

        private string m_hearthstoneDir;

        private bool m_initialized;

        // File handlers
        private FileRetriever m_fileRetriever;
        private FileDecoder m_fileDecoder;

        // Patcher version
        private string m_latestPatcherVersion; // 2.0.0 (Patcher-only)

        // Versions
        private string m_installedHearthstoneVersion; // 1.0.0.1000
        private string m_installedHearthstoneAccessVersion; // 1
        private string m_latestHearthstoneAccessVersion; // 1

        public Patcher(FileRetriever fileRetriever, FileDecoder fileDecoder)
        {
            m_fileRetriever = fileRetriever;
            m_fileDecoder = fileDecoder;
        }

        public void PatchHearthstone()
        {
            AssertInitialized();

            try
            {
                var hsaZipPath = m_fileRetriever.RetrieveHearthstoneAccessZip(m_installedHearthstoneVersion);
                ApplyPatch(hsaZipPath);
            }
            catch (FileNotFoundException)
            {
                throw new SystemException($"Unexpected error: could not find Hearthstone Access zip for Hearthstone version {m_installedHearthstoneVersion}");
            }
        }

        private void ApplyPatch(string hsaZipPath)
        {
            var zipDir = Path.GetDirectoryName(hsaZipPath);
            var patchName = Path.GetFileNameWithoutExtension(hsaZipPath);

            ZipFile.ExtractToDirectory(hsaZipPath, zipDir);

            var patchDir = Path.Combine(zipDir, patchName);

            // TODO: Consider removing dirs from older versions (e.g. outdated speeches) in the future
            CloneDirectoryRecursively(patchDir, m_hearthstoneDir);
        }

        private void CloneDirectoryRecursively(string srcDir, string dstDir)
        {
            foreach (var dir in Directory.GetDirectories(srcDir))
            {
                var srcDirName = Path.GetFileName(dir);
                var dstDirName = Path.Combine(dstDir, srcDirName);

                if (!Directory.Exists(dstDirName))
                {
                    Directory.CreateDirectory(dstDirName);
                }

                CloneDirectoryRecursively(dir, dstDirName);
            }

            foreach (var file in Directory.GetFiles(srcDir))
            {
                var srcFileName = Path.GetFileName(file);
                var dstFile = Path.Combine(dstDir, srcFileName);

                File.Copy(file, dstFile, true);
            }
        }

        public void SetHearthstoneDirectory(string hsDir)
        {
            if (!PatchingUtils.IsHearthstoneDirectory(hsDir))
            {
                throw new InvalidOperationException();
            }

            m_hearthstoneDir = hsDir;
        }

        private string GetInstalledHearthstoneVersion()
        {
            string productInfo = File.ReadAllText(PatchingUtils.GetProductDbPath(m_hearthstoneDir), Encoding.ASCII);

            Regex regex = new Regex("([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+)");
            var versions = regex.Matches(productInfo);
            if (versions.Count != 1)
            {
                throw new SystemException("Could not extract the current Hearthstone version from .product.db");
            }

            return versions[0].Value;
        }

        private string GetInstalledHearthstoneAccessVersion()
        {
            var path = PatchingUtils.GetHearthstoneAccessVersionPath(m_hearthstoneDir);

            if (!File.Exists(path))
            {
                // HSA hasn't been installed yet
                return null;
            }

            return File.ReadAllText(path, Encoding.ASCII);
        }

        public bool IsHearthstoneAccessOutOfDate()
        {
            if (m_installedHearthstoneAccessVersion == null)
            {
                return true;
            }

            return !m_installedHearthstoneAccessVersion.Equals(m_latestHearthstoneAccessVersion);
        }

        public void LoadHearthstoneAccessVersion()
        {
            if (m_hearthstoneDir == null)
            {
                throw new InvalidOperationException();
            }

            m_installedHearthstoneVersion = GetInstalledHearthstoneVersion();

            var hsaVersionFile = m_fileRetriever.RetrieveHearthstoneAccessVersionFile(m_installedHearthstoneVersion);

            m_latestHearthstoneAccessVersion = m_fileDecoder.GetHearthstoneAccessVersion(hsaVersionFile);
            m_installedHearthstoneAccessVersion = GetInstalledHearthstoneAccessVersion();

            m_initialized = true;
        }

        public bool IsOutdated()
        {
            try
            {
                var patcherVersionFile = m_fileRetriever.RetrievePatcherVersionFile();
                m_latestPatcherVersion = m_fileDecoder.GetPatcherVersion(patcherVersionFile);

                return !PATCHER_VERSION.Equals(m_latestPatcherVersion);
            }
            catch (FileNotFoundException)
            {
                return true;
            }
        }

        private void AssertInitialized()
        {
            if (!m_initialized)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
