using System;
using System.Runtime.InteropServices;

namespace EZPlayer.Commmon
{
    public class SHChangeNotifier
    {
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(HChangeNotifyEventID wEventId,
                                           HChangeNotifyFlags uFlags,
                                           IntPtr dwItem1,
                                           IntPtr dwItem2);
    }
}
