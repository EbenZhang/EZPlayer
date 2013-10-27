using Org.Mentalis.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EZPlayer.Power
{
    /// <summary>
    /// ShutdownPrompt.xaml 的交互逻辑
    /// </summary>
    public partial class ShutdownPrompt : Window
    {
        public ShutdownPrompt()
        {
            if (App.PostPlayAction == null)
            {
                throw new ApplicationException("Post Play Action Is Null");
            }
            switch (App.PostPlayAction.Value)
            {
                case RestartOptions.Hibernate:
                    m_actionDesc = FindResource("Hibernate").ToString();
                    break;
                case RestartOptions.ShutDown:
                    m_actionDesc = FindResource("Shutdown").ToString();
                    break;
                default:
                    throw new ApplicationException("Unknow Post Play Action");
            }
            m_fmt = FindResource("ShutdownPromptMsg").ToString();

            InitializeComponent();

            m_timer = new DispatcherTimer();
            m_timer.Interval = new TimeSpan(0,0,1);
            m_timer.Tick += tm_Tick;
            m_timer.IsEnabled = true;
            m_timer.Start();
        }

        void tm_Tick(object sender, EventArgs e)
        {
            if (m_secondsRemain < 0)
            {
                WindowsController.ExitWindows(App.PostPlayAction.Value, false);
            }
            m_secondsRemain--;
            TxtMsg.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
        }
        private string m_actionDesc = "DoNothing";
        private string m_fmt;
        DispatcherTimer m_timer;
        private int m_secondsRemain = 20;

        public string ShutdownPromptMsg
        {
            get
            {
                return string.Format(m_fmt, m_secondsRemain, m_actionDesc);
            }
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            m_timer.IsEnabled = false;
            m_timer.Stop();
            this.Close();
        }
    }
}
