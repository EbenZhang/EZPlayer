using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using EZPlayer.Common;
using log4net;
using HistoryItemContainer = System.Collections.Generic.List<EZPlayer.History.HistoryItem>;

namespace EZPlayer.History
{
    public class HistoryModel
    {
        private string m_lastPlayInfoFilePath = null;
        private string m_historyInfoFilePath = null;
        public HistoryItemContainer m_historyItems = new HistoryItemContainer();
        public HistoryModel(string lastPlayInfoFilePath, string historyInfoPath)
        {
            m_lastPlayInfoFilePath = lastPlayInfoFilePath;
            m_historyInfoFilePath = historyInfoPath;
            Load();
        }
        public HistoryItem LastPlayedFile
        {
            get
            {
                if (!File.Exists(m_lastPlayInfoFilePath))
                {
                    return null;
                }
                try
                {
                    using (var s = File.Open(m_lastPlayInfoFilePath, FileMode.Open))
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
                using (var stream = File.Open(m_lastPlayInfoFilePath, FileMode.Create))
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
            using (var stream = File.Open(m_historyInfoFilePath, FileMode.Create))
            {
                new XmlSerializer(typeof(HistoryItemContainer))
                    .Serialize(stream, m_historyItems);
            }
        }

        private void Load()
        {
            if (!File.Exists(m_historyInfoFilePath))
            {
                return;
            }
            try
            {
                using (var s = File.Open(m_historyInfoFilePath, FileMode.Open))
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
