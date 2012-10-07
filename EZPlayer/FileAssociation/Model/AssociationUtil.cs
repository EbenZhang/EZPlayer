using System;
using System.Collections.Generic;
using EZPlayer.Common;
using Microsoft.Win32;

namespace EZPlayer.FileAssociation.Model
{
    public class AssociationUtil
    {
        private string m_appName;
        private string m_appPath;

        public AssociationUtil(string applicationName, string applicationPath, List<ExtensionItem> extList)
        {
            m_appName = applicationName;
            m_appPath = applicationPath;
            CreateAppInfo();
            foreach (var ext in extList)
            {
                if (ext.IsAssociated)
                {
                    AssociateExtWithApp(ext.Ext);
                    DeleteUserChoice(ext.Ext);
                }
                else
                {
                    Unassociate(ext.Ext);
                }
            }

            SHChangeNotifier.SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED,
                HChangeNotifyFlags.SHCNF_IDLIST,
                IntPtr.Zero, IntPtr.Zero);
        }

        private void Unassociate(string ext)
        {
            var keyPath = @"SOFTWARE\Classes\" + ext;
            var extKey = Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (extKey != null)
            {
                string defaultValue = extKey.GetValue("", "") as string;
                if (defaultValue == m_appName)
                {
                    extKey.SetValue("", "", RegistryValueKind.String);
                }
            }
        }

        private static string GetUserChoicePath(string ext)
        {
            var userChoicePath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + ext + @"\UserChoice";
            return userChoicePath;
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
            var keyPath = @"SOFTWARE\Classes\" + ext;
            var extKey = Registry.CurrentUser.OpenSubKey(keyPath, true);
            if (extKey == null)
            {
                extKey = Registry.CurrentUser.CreateSubKey(keyPath);
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
