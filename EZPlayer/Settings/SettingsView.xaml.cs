using Org.Mentalis.Utilities;
using System.Windows;

namespace EZPlayer.View
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsView : Window
    {
        public SettingsView()
        {
            InitializeComponent();
            _postPlayAction = GetPostAction();
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

        private string GetPostAction()
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
    }
}
