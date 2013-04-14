using EZPlayer.Common;
using log4net;
using SubtitleTools.Infrastructure.Core.OpenSubtitlesOrg.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace EZPlayer.Subtitle
{
    public class OpenSubtitleSearcher : ISubtitleSearcher
    {
        IOpenSubtitlesDb m_openSubtitles = OpenSubtitlesFactory.Create(
            Properties.Settings.Default.OpenSubUserAgent,
            Properties.Settings.Default.OpenSubURI);
        string m_token = null;

        private SubtitlesSearchResult Search(string movieFilePath)
        {
            var fileInfo = new MovieFileInfo(movieFilePath, subFileName: string.Empty);
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;

            SearchInfo info = new SearchInfo()
            {
                moviehash = fileInfo.MovieHash,
                sublanguageid = "all",
                moviebytesize = fileInfo.MovieFileLength
            };
            return m_openSubtitles.SearchSubtitles(m_token, new SearchInfo[] { info });
        }
        public string DownloadSubtitles(string movieFilePath)
        {
            TryLogin();
            try
            {
                var preferSubLanguage = CultureInfo.CurrentCulture.Name.Substring(0, 2).ToLower();

                var searchResult = Search(movieFilePath);

                var subtitleDataInfo = new Dictionary<string, SubtitleDataInfo>();
                foreach (var item in searchResult.data)
                {
                    var iso639 = item.ISO639.ToLower();
                    if (subtitleDataInfo.ContainsKey(iso639))
                    {
                        continue;
                    }
                    if (iso639 != "en"
                        && iso639 != preferSubLanguage)
                    {
                        continue;
                    }

                    subtitleDataInfo.Add(iso639, item);
                }

                foreach (var item in subtitleDataInfo.Values)
                {
                    var fileName = GetSubtitleFileName(movieFilePath, item);
                    if(File.Exists(fileName))
                    {
                        continue;
                    }
                    var downloadResult = m_openSubtitles.DownloadSubtitles(m_token, new string[] { item.IDSubtitleFile });
                    if (downloadResult.status == "200 OK")
                    {
                        if (downloadResult.data == null || downloadResult.data.Length == 0)
                        {
                            continue;
                        }
                        var gzBase64Data = downloadResult.data[0].data;
                        if (string.IsNullOrWhiteSpace(gzBase64Data))
                        {
                            throw new Exception("Received gzBase64Data is empty.");
                        }

                        //from: http://trac.opensubtitles.org/projects/opensubtitles/wiki/XmlRpcIntro
                        //it's gzipped without header.
                        var gzBuffer = Convert.FromBase64String(gzBase64Data);
                        var content = Compression.DecompressGz(gzBuffer);
                        File.WriteAllBytes(fileName, content);
                    }
                }

                if (subtitleDataInfo.ContainsKey(preferSubLanguage))
                {
                    return GetSubtitleFileName(movieFilePath, subtitleDataInfo[preferSubLanguage]);
                }
                else if (subtitleDataInfo.ContainsKey("en"))
                {
                    return GetSubtitleFileName(movieFilePath, subtitleDataInfo["en"]);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(OpenSubtitleSearcher)).Error(ex.AllMessages());
                return null;
            }
        }

        private static string GetSubtitleFileName(string movieFilePath, SubtitleDataInfo item)
        {
            var fileName = string.Format(@"{0}\{1}.{2}{3}",
                Path.GetDirectoryName(movieFilePath),
                Path.GetFileNameWithoutExtension(movieFilePath),
                item.LanguageName,
                Path.GetExtension(item.SubFileName));
            return fileName;
        }

        void TryLogin()
        {
            if (!string.IsNullOrEmpty(m_token))
            {
                return;
            }

            var loginInfo = m_openSubtitles.LogIn(string.Empty,
                string.Empty,
                string.Empty,
                m_openSubtitles.UserAgent);
            var status = loginInfo.status;
            if (string.IsNullOrWhiteSpace(status) || status != "200 OK")
            {
                throw new Exception("Couldn't login.");
            }
            m_token = loginInfo.token;
        }
    }
}
