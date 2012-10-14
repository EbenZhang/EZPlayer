using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EZPlayer.ViewModel;

namespace EZPlayer.History
{
    public class HistoryViewModel : ViewModelBase
    {
        private HistoryModel m_model = HistoryModel.Instance;

        public class HistoryItemUI
        {
            private HistoryItem m_item = null;
            
            public HistoryItemUI(HistoryItem item)
            {
                m_item = item;
            }
            public string FileName
            {
                get
                {
                    return Path.GetFileNameWithoutExtension(m_item.FilePath);
                }
            }
            public string FilePath
            {
                get
                {
                    return m_item.FilePath;
                }
            }
            public int Progress
            {
                get
                {
                    return (int)(m_item.Position * 100);
                }
                set
                {
                    throw new Exception("An attempt to modify Read-Only property");
                }
            }

            public HistoryItem ItemData
            {
                get
                {
                    return m_item;
                }
            }
        }
        public List<HistoryItemUI> HistoryItems
        {
            get
            {
                return m_model.HistoryItems
                    .Select(item => new HistoryItemUI(item))
                    .ToList();
            }
        }

        public void RemoveItems(List<HistoryItemUI> items)
        {
            items.ForEach(item => m_model.HistoryItems.Remove(item.ItemData));
            m_model.Save();
            NotifyPropertyChange(() => HistoryItems);
        }
    }
}
