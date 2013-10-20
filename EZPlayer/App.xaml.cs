using System;
using System.Deployment.Application;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using Vlc.DotNet.Core;
using System.IO;
using System.Diagnostics;
using Org.Mentalis.Utilities;

namespace EZPlayer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private readonly static string APP_START_PATH = Process.GetCurrentProcess().MainModule.FileName;
        private readonly static string APP_START_DIR = Path.GetDirectoryName(APP_START_PATH);

        public static RestartOptions? PostPlayAction;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CheckForShortcut();
            LoadLanguage();
            InitVlcContext();
        }

        private static void InitVlcContext()
        {
            // Set libvlc.dll and libvlccore.dll directory path
            VlcContext.LibVlcDllsPath = Path.Combine(APP_START_DIR, "VLC");

            // Set the vlc plugins directory path
            VlcContext.LibVlcPluginsPath = Path.Combine(VlcContext.LibVlcDllsPath, "plugins");

            /* Setting up the configuration of the VLC instance.
             * You can use any available command-line option using the AddOption function (see last two options). 
             * A list of options is available at 
             *     http://wiki.videolan.org/VLC_command-line_help
             * for example. */

            // Ignore the VLC configuration file
            VlcContext.StartupOptions.IgnoreConfig = true;

            VlcContext.StartupOptions.LogOptions.LogInFile = true;
#if DEBUG
            VlcContext.StartupOptions.LogOptions.Verbosity = VlcLogVerbosities.Debug;
            VlcContext.StartupOptions.LogOptions.ShowLoggerConsole = true;
#else
            //Set the startup options
            VlcContext.StartupOptions.LogOptions.ShowLoggerConsole = false;
            VlcContext.StartupOptions.LogOptions.Verbosity = VlcLogVerbosities.None;
#endif

            // Disable showing the movie file name as an overlay
            VlcContext.StartupOptions.AddOption("--no-video-title-show");

            // The only supporting Chinese font
            VlcContext.StartupOptions.AddOption("--freetype-font=DFKai-SB");

            // Pauses the playback of a movie on the last frame
            //VlcContext.StartupOptions.AddOption("--play-and-pause");

            // Initialize the VlcContext
            VlcContext.Initialize();
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
