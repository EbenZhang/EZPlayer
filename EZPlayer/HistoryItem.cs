using System;

namespace EZPlayer
{
    [Serializable]
    public class HistoryItem
    {
        public float Position { get; set; }
        public string FilePath { get; set; }
    }
}
