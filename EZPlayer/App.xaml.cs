using System.Globalization;
using System.Windows;
using System;
using System.Linq;

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
    }
}
