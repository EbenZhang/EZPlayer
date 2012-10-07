using System;

namespace EZPlayer.History
{
    [Serializable]
    public class HistoryItem
    {
        public float Position;
        public string FilePath;
        public double Volume;
        public DateTime PlayedDate;
    }
}
