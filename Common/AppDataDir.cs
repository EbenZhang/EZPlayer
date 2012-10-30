using System;
using System.IO;
using System.Diagnostics;

namespace EZPlayer.Common
{
    public class AppDataDir
    {
        private readonly static string USER_APP_DATA_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public readonly static string EZPLAYER_DATA_DIR = Path.Combine(USER_APP_DATA_DIR, "EZPlayer");
        public readonly static string PROCESS_PATH = Process.GetCurrentProcess().MainModule.FileName;
        public readonly static string PROCESS_DIR = Path.GetDirectoryName(PROCESS_PATH);
    }
}
