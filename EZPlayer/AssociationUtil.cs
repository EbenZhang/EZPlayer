using System;
using EZPlayer.Commmon;
using Microsoft.Win32;

namespace EZPlayer.FileAssociator
{
    public class AssociationUtil
    {
        private string m_appName;
        private string m_appPath;

        public AssociationUtil(string applicationName, string applicationPath, string[] extList)
        {
            m_appName = applicationName;
            m_appPath = applicationPath;
            CreateAppInfo();
            foreach (string ext in extList)
            {
                AssociateExtWithApp(ext);
                DeleteUserChoice(ext);
            }

            SHChangeNotifier.SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED,
                HChangeNotifyFlags.SHCNF_IDLIST,
                IntPtr.Zero, IntPtr.Zero);
        }

        private void CreateAppInfo()
        {
            var classesKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes", true);
            var appKey = classesKey.CreateSubKey(m_appName);
            var iconKey = appKey.CreateSubKey("DefaultIcon");
            var iconPath = m_appPath + ",0";
            iconKey.SetValue("", iconPath , RegistryValueKind.Unknown);

            RegistryKey shellOpenKey = appKey.CreateSubKey("shell\\open\\command");
            string openCommand = m_appPath + " \"%1\"";
            shellOpenKey.SetValue("", openCommand, RegistryValueKind.String);
            appKey.SetValue("", openCommand, RegistryValueKind.String);
        }

        private void AssociateExtWithApp(string ext)
        {
            var extKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\" + ext, true);
            if (extKey == null)
            {
                throw new ApplicationException(ext);
            }
            extKey.SetValue("", m_appName, RegistryValueKind.String);            
        }

        private static void DeleteUserChoice(string ext)
        {
            var userChoicePath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + ext + @"\UserChoice";
            Registry.CurrentUser.DeleteSubKey(userChoicePath, false);
        }
    }
}
