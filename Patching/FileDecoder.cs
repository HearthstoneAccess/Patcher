using System.IO;

namespace Patching
{
    public class FileDecoder
    {
        public string GetHearthstoneAccessVersion(string hearthstoneAccessVersionFile)
        {
            return File.ReadAllText(hearthstoneAccessVersionFile);
        }

        public string GetPatcherVersion(string patcherVersionFile)
        {
            return File.ReadAllText(patcherVersionFile);
        }
    }
}
