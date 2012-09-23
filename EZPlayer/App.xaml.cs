using System.Globalization;
using System.Windows;
using System;
using System.Linq;
using System.Deployment.Application;
using System.Reflection;

namespace EZPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CheckForShortcut();
            LoadLanguage();
        }

        private void LoadLanguage()
        {
            CultureInfo currentCultureInfo = CultureInfo.CurrentCulture;
            ResourceDictionary langResource = null;
            try
            {
                langResource =
                    Application.LoadComponent(
                    new Uri(@"I18N\" + currentCultureInfo.Name + ".xaml", UriKind.Relative))
                    as ResourceDictionary;
            }
            catch
            {
            }
            if (langResource != null)
            {
                if (this.Resources.MergedDictionaries.Count > 0)
                {
                    var defaultLangURI = new Uri(@"I18N\DefaultLang.xaml", UriKind.Relative);
                    var defaultLangResourceDict = this.Resources.MergedDictionaries.Single(dict => dict.Source == defaultLangURI);
                    Resources.MergedDictionaries.Remove(defaultLangResourceDict);
                    this.Resources.MergedDictionaries.Add(langResource);
                }
            }
        }

        /// <summary>
        /// This will create a Application Reference file on the users desktop
        /// if they do not already have one when the program is loaded.
        /// Check for them running the deployed version before doing this,
        /// so it doesn't kick it when you're running it from Visual Studio.
        /// </summary>
        static void CheckForShortcut()
        {
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                if (ad.IsFirstRun)  //first time user has run the app since installation or update
                {
                    Assembly code = Assembly.GetExecutingAssembly();
                    string company = string.Empty;
                    string description = string.Empty;
                    if (Attribute.IsDefined(code, typeof(AssemblyCompanyAttribute)))
                    {
                        AssemblyCompanyAttribute ascompany =
                            (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(code,
                            typeof(AssemblyCompanyAttribute));
                        company = ascompany.Company;
                    }
                    if (Attribute.IsDefined(code, typeof(AssemblyDescriptionAttribute)))
                    {
                        AssemblyDescriptionAttribute asdescription =
                            (AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(code,
                            typeof(AssemblyDescriptionAttribute));
                        description = asdescription.Description;
                    }
                    if (company != string.Empty && description != string.Empty)
                    {
                        string desktopPath = string.Empty;
                        desktopPath = string.Concat(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            "\\", description, ".appref-ms");
                        string shortcutName = string.Empty;
                        shortcutName = string.Concat(
                            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                            "\\", company, "\\", description, ".appref-ms");
                        System.IO.File.Copy(shortcutName, desktopPath, true);
                    }
                }
            }
        }
    }
}
