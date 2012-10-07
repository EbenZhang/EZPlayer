using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using EZPlayer.Common;
using log4net;
using HistoryItemContainer = System.Collections.Generic.List<EZPlayer.History.HistoryItem>;

namespace EZPlayer.History
{
    class HistoryModel
    {
        private static readonly string LAST_PLAY_INFO_FILE = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "lastplay.xml");
        private static readonly string HISTORY_INFO_FILE = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "history.xml");
        public HistoryItemContainer m_historyItems = new HistoryItemContainer();
        public HistoryModel()
        {
            Load();
        }
        public HistoryItem LastPlayedFile
        {
            get
            {
                if (!File.Exists(LAST_PLAY_INFO_FILE))
                {
                    return null;
                }
                try
                {
                    using (var s = File.Open(LAST_PLAY_INFO_FILE, FileMode.Open))
                    {
                        var lastItem = new XmlSerializer(typeof(HistoryItem)).Deserialize(s) as HistoryItem;
                        return lastItem;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger(typeof(HistoryModel)).Error(ex.AllMessages());
                    return null;
                }
            }
            set
            {
                using (var stream = File.Open(LAST_PLAY_INFO_FILE, FileMode.Create))
                {
                    new XmlSerializer(typeof(HistoryItem)).Serialize(stream, value);
                }
                AddLastPlayedFileToHistory();
            }
        }

        public HistoryItem GetHistoryInfo(string filePath)
        {
            var matched = m_historyItems.Find(item => item.FilePath == filePath);
            if (matched != null)
            {
                return matched;
            }
            if (LastPlayedFile != null && LastPlayedFile.FilePath == filePath)
            {
                return LastPlayedFile;
            }
            return null;
        }

        public void Save()
        {
            using (var stream = File.Open(HISTORY_INFO_FILE, FileMode.Create))
            {
                new XmlSerializer(typeof(HistoryItemContainer))
                    .Serialize(stream, m_historyItems);
            }
        }

        private void Load()
        {
            if (!File.Exists(HISTORY_INFO_FILE))
            {
                return;
            }
            try
            {
                using (var s = File.Open(HISTORY_INFO_FILE, FileMode.Open))
                {
                    m_historyItems = new XmlSerializer(typeof(HistoryItemContainer))
                        .Deserialize(s) as HistoryItemContainer;

                    m_historyItems = m_historyItems.Where(item => (DateTime.Now - item.PlayedDate) < TimeSpan.FromDays(90)).ToList();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(HistoryModel)).Error(ex.AllMessages());
            }
        }

        private void AddLastPlayedFileToHistory()
        {
            var matched = m_historyItems.Find(item => item.FilePath == LastPlayedFile.FilePath);
            m_historyItems.RemoveAll(item => item.FilePath == LastPlayedFile.FilePath);
            m_historyItems.Add(LastPlayedFile);
        }
    }
}
