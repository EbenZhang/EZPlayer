using System.IO;

namespace EZPlayer
{
    public interface IDirectorySearcher
    {
        string[] SearchFiles(string dirPath, string pattern, SearchOption option);
    }

    public class DirectorySearcher : IDirectorySearcher
    {
        private DirectorySearcher() { }
        private static DirectorySearcher m_searcher = new DirectorySearcher();
        public static DirectorySearcher Instance
        {
            get { return m_searcher; }
        }
        public string[] SearchFiles(string dirPath, string pattern, SearchOption option)
        {
            return Directory.GetFiles(dirPath, pattern, option);
        }
    }
}
