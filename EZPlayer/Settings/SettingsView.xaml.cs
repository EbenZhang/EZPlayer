using Org.Mentalis.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace EZPlayer.View
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            _postPlayAction = GetCurPostActionDesc();
            InitializeComponent();            
        }
        private string _postPlayAction;
        public string PostPlayAction
        {
            get
            {
                return _postPlayAction;
            }
            set
            {
                var shtd = FindResource("Shutdown").ToString();
                if (value == shtd)
                {
                    App.PostPlayAction = RestartOptions.ShutDown;
                }
                else if (value == FindResource("Hibernate").ToString())
                {
                    App.PostPlayAction = RestartOptions.Hibernate;
                }
                else
                {
                    App.PostPlayAction = null;
                }
            }
        }

        private string GetCurPostActionDesc()
        {
            if (App.PostPlayAction == null)
            {
                return FindResource("DoNothing").ToString();
            }
            switch (App.PostPlayAction.Value)
            {
                case RestartOptions.Hibernate:
                    return FindResource("Hibernate").ToString();
                case RestartOptions.ShutDown:
                    return FindResource("Shutdown").ToString();
            }
            return FindResource("DoNothing").ToString();
        }

        public ObservableCollection<string> PostActionOptions
        {
            get
            {
                var items = new ObservableCollection<string>();
                items.Add(FindResource("DoNothing").ToString());
                items.Add(FindResource("Shutdown").ToString());
                items.Add(FindResource("Hibernate").ToString());
                return items;
            }
        }
    }
}
