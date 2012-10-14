using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using System.Linq;
using System.Windows.Controls;

namespace EZPlayer.History
{
    /// <summary>
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    public partial class HistoryView : Window
    {
        public HistoryView()
        {
            InitializeComponent();
            FileList = new List<string>();
        }

        public List<string> FileList
        {
            get;
            private set;
        }

        private void OnBtnBrowse(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Open media file for playback",
                FileName = "Media File",
                Filter = "All files |*.*"
            };

            openFileDialog.Multiselect = true;

            // Process open file dialog box results
            if (openFileDialog.ShowDialog() == true)
            {
                FileList.AddRange(openFileDialog.FileNames);
                Close();
            }
        }

        private void OnBtnPlayClick(object sender, RoutedEventArgs e)
        {
            var selectedItems = m_listBoxhistoryItems.SelectedItems;
            if (selectedItems.Count == 0)
            {
                return;
            }
            foreach (HistoryViewModel.HistoryItemUI item in selectedItems)
            {
                FileList.Add(item.FilePath);
            }
            Close();
        }

        private void OnBtnClearClick(object sender, RoutedEventArgs e)
        {
            ClearSelectedItems();
        }

        private void OnDeleteKeyPressed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            ClearSelectedItems();
        }

        private void ClearSelectedItems()
        {
            var viewModel = this.DataContext as HistoryViewModel;

            var items = m_listBoxhistoryItems.SelectedItems.Cast<HistoryViewModel.HistoryItemUI>().ToList();
            viewModel.RemoveItems(items);
        }
    }
}
