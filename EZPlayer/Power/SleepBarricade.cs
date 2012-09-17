using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace EZPlayer.Power
{
    public class SleepBarricade
    {
        [FlagsAttribute]
        private enum EXECUTION_STATE : uint
        {
            ES_INVALID = 0,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            // Legacy flag, should not be used.
            // ES_USER_PRESENT   = 0x00000004,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
        }

        private readonly DispatcherTimer m_preventSleepTimer;
        private Func<bool> m_shouldPreventSleep;
        private EXECUTION_STATE m_initialExcutionState = EXECUTION_STATE.ES_INVALID;

        public SleepBarricade(Func<bool> shouldPreventSleep)
        {
            m_shouldPreventSleep = shouldPreventSleep;

            m_preventSleepTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(10),
                IsEnabled = true
            };
            m_preventSleepTimer.Tick += OnPreventSleepTimer;
            m_preventSleepTimer.Start();
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        private void PreventSleep()
        {
            var lastState = SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS
                | EXECUTION_STATE.ES_DISPLAY_REQUIRED
                | EXECUTION_STATE.ES_SYSTEM_REQUIRED
                | EXECUTION_STATE.ES_AWAYMODE_REQUIRED);//Away mode for Windows >= Vista
            if (lastState == EXECUTION_STATE.ES_INVALID)
            {
                lastState = SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS
                    | EXECUTION_STATE.ES_DISPLAY_REQUIRED
                    | EXECUTION_STATE.ES_SYSTEM_REQUIRED); //Windows < Vista, forget away mode
            }

            if (m_initialExcutionState == EXECUTION_STATE.ES_INVALID)
            {
                m_initialExcutionState = lastState;
            }
        }

        public void OnPreventSleepTimer(object sender, EventArgs e)
        {
            if (m_shouldPreventSleep())
            {
                PreventSleep();
            }
            else
            {
                RestoreExecutionStateToInitial();
            }
        }

        private void RestoreExecutionStateToInitial()
        {
            SetThreadExecutionState(m_initialExcutionState);
        }
    }
}
