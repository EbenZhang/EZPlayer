using System;
using System.Windows.Threading;

namespace EZPlayer
{
    public class DelayTask
    {
        private DispatcherTimer m_timer;
        private Action m_action;
        public DelayTask(TimeSpan timeSpan, Action action)
        {
            m_action = action;
            m_timer = new DispatcherTimer()
            {
                Interval = timeSpan,
                IsEnabled = true
            };
            m_timer.Tick += new EventHandler(OnTimer);
            m_timer.Start();
        }

        void OnTimer(object sender, EventArgs e)
        {
            m_timer.Stop();
            m_action();
        }
    }
}
