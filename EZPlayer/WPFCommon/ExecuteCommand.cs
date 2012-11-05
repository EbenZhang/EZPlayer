using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace EZPlayer
{
    public class ExecuteCommand
    {
        public static void Execute(ICommand cmd, object param = null)
        {
            if (cmd.CanExecute(param))
            {
                cmd.Execute(param);
            }
        }
    }
}
