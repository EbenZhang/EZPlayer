using System;
using System.Text;

namespace EZPlayer.Common
{
    public static class ExceptionExtension
    {
        public static string AllMessages(this Exception ex)
        {
            var msg = new StringBuilder();
            AppendExceptionMsg(msg, ex);
            return msg.ToString();
        }

        private static void AppendExceptionMsg(StringBuilder msg, Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            msg.Append(ex.Message);
            msg.Append("\r\n");
            msg.Append(ex.StackTrace);
            msg.Append("\r\n");

            AppendExceptionMsg(msg, ex.InnerException);
        }
    }
}
