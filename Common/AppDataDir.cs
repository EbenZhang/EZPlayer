using System;
using System.IO;

namespace EZPlayer.Common
{
    public class AppDataDir
    {
        public readonly static string USER_APP_DATA_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public readonly static string EZPLAYER_DATA_DIR = Path.Combine(USER_APP_DATA_DIR, "EZPlayer");
    }
}
