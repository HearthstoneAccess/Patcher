using System;
using System.IO;
using System.Net;

namespace Patching
{
    public class GithubFileRetriever : FileRetriever
    {
        private static bool DEV_MODE = false;

        private static string GITHUB_TOKEN = DEV_MODE? File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "dev-github-token.txt")) : null;

        private static string GITHUB_GROUP_NAME = DEV_MODE ? File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "dev-github-group.txt")) : "HearthstoneAccess";

        private static string HEARTHSTONE_ACCESS_REPO_RAW_PATH = $"https://raw.githubusercontent.com/{GITHUB_GROUP_NAME}/HearthstoneAccess";

        private static string PATCHER_REPO_RAW_PATH = $"https://raw.githubusercontent.com/{GITHUB_GROUP_NAME}/Patcher";

        public static string PATCHER_REPO_RELEASES_PATH = $"https://github.com/{GITHUB_GROUP_NAME}/Patcher/releases";

        private DirectoryInfo m_tmpDir;

        public GithubFileRetriever()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            m_tmpDir = CreateTemporaryDirectory();
        }

        ~GithubFileRetriever()
        {
            if (m_tmpDir != null && m_tmpDir.Exists)
            {
                Directory.Delete(m_tmpDir.FullName, true);
            }
        }

        private DirectoryInfo CreateTemporaryDirectory()
        {
            string tmpPath = Path.GetTempFileName();
            File.Delete(tmpPath);
            return Directory.CreateDirectory(tmpPath);
        }

        public string RetrieveHearthstoneAccessVersionFile(string hearthstoneVersion)
        {
            var fileName = "hsa-version";
            var url = $"{HEARTHSTONE_ACCESS_REPO_RAW_PATH}/{hearthstoneVersion}/{fileName}";

            return RetrieveFile(url, fileName);
        }

        public string RetrieveHearthstoneAccessZip(string hearthstoneVersion)
        {
            var fileName = $"patch.zip";
            var url = $"{HEARTHSTONE_ACCESS_REPO_RAW_PATH}/{hearthstoneVersion}/{fileName}";

            return RetrieveFile(url, fileName);
        }

        private string RetrieveFile(string url, string fileName)
        {
            var path = Path.Combine(m_tmpDir.FullName, fileName);

            using (WebClient wc = new WebClient())
            {
                try
                {
                    if (DEV_MODE)
                    {
                        wc.Headers.Add("Authorization", $"token {GITHUB_TOKEN}");
                    }

                    wc.Headers.Add(HttpRequestHeader.UserAgent, "HSAPatcher");

                    wc.DownloadFile(url, path);
                    return path;
                }
                catch (WebException we)
                {
                    if (we.Response is HttpWebResponse)
                    {
                        HttpWebResponse response = (HttpWebResponse)we.Response;
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            throw new FileNotFoundException();
                        }
                    }
                    else
                    {
                        throw we;
                    }
                }
                catch(Exception e)
                {
                    throw e;
                }
            }

            throw new SystemException($"Error retrieving file {fileName} from url: {url}");
        }

        public string RetrievePatcherVersionFile()
        {
            var fileName = "patcher-version";
            var url = $"{PATCHER_REPO_RAW_PATH}/master/{fileName}";

            return RetrieveFile(url, fileName);
        }
    }
}
