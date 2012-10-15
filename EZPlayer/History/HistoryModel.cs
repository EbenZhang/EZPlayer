using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using EZPlayer.Common;
using log4net;
using HistoryItemContainer = System.Collections.Generic.List<EZPlayer.History.HistoryItem>;
using System.Diagnostics;

namespace EZPlayer.History
{
    public class HistoryModel
    {
        public static string HISTORY_INFO_FILE_PATH = Path.Combine(AppDataDir.EZPLAYER_DATA_DIR, "history.xml");

        private HistoryItemContainer m_historyItems = new HistoryItemContainer();

        public HistoryItemContainer HistoryItems
        {
            get
            {
                return m_historyItems;
            }
        }
        protected HistoryModel()
        {
            Load();
        }

        public static HistoryModel Instance = new HistoryModel();

        public void Reload()
        {
            Load();
        }

        public HistoryItem LastPlayedFile
        {
            get
            {
                if (m_historyItems.Count > 0)
                {
                    return m_historyItems[0];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                AddItem(value);
            }
        }

        public HistoryItem GetHistoryInfo(string filePath)
        {
            var matched = m_historyItems.Find(item => item.FilePath == filePath);
            if (matched != null)
            {
                return matched;
            }
            return null;
        }

        public void Save()
        {
            using (var stream = File.Open(HISTORY_INFO_FILE_PATH, FileMode.Create))
            {
                new XmlSerializer(typeof(HistoryItemContainer))
                    .Serialize(stream, m_historyItems);
            }
        }

        private void Load()
        {
            if (!File.Exists(HISTORY_INFO_FILE_PATH))
            {
                m_historyItems.Clear();
                return;
            }
            try
            {
                using (var s = File.Open(HISTORY_INFO_FILE_PATH, FileMode.Open))
                {
                    m_historyItems = new XmlSerializer(typeof(HistoryItemContainer))
                        .Deserialize(s) as HistoryItemContainer;

                    m_historyItems = m_historyItems
                        .Where(item => File.Exists(item.FilePath) && (DateTime.Now - item.PlayedDate) < TimeSpan.FromDays(90))
                        .OrderByDescending(item => item.PlayedDate)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(HistoryModel)).Error(ex.AllMessages());
            }
        }

        private void AddItem(HistoryItem item)
        {
            if (m_historyItems.Count != 0)
            {
                Trace.Assert(item.PlayedDate >= m_historyItems[0].PlayedDate);
            }
            var matched = m_historyItems.Find(i => i.FilePath == item.FilePath);
            m_historyItems.RemoveAll(i => i.FilePath == item.FilePath);
            m_historyItems.Insert(0, item);
        }
    }
}
