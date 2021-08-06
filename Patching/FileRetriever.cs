namespace Patching
{
    public interface FileRetriever
    {
        string RetrievePatcherVersionFile();

        string RetrieveHearthstoneAccessVersionFile(string hearthstoneVersion);

        string RetrieveHearthstoneAccessZip(string hearthstoneVersion);
    }
}
