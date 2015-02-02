using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EZPlayer.Subtitle
{
    public static class SubtitleUtil
    {
        public static void PrepareSubtitle(string mediaFilePath)
        {
            if (mediaFilePath.ToUpperInvariant().StartsWith("HTTP"))
            {
                return;
            }
            var files = FindAllSubtitleFiles(mediaFilePath);
            foreach (var f in files)
            {
                var fileContent = File.ReadAllBytes(f);
                var encoding = EncodingDetector.Detect(fileContent);

                if (encoding != Encoding.UTF8)
                {
                    File.Copy(f, f + "." + encoding.WebName, true);
                    var utf8Bytes = Encoding.Convert(encoding,
                        Encoding.UTF8,
                        fileContent);
                    File.WriteAllBytes(f, utf8Bytes);
                }
            }
        }

        private static string[] FindAllSubtitleFiles(string mediaFilePath)
        {
            var dir = Path.GetDirectoryName(mediaFilePath);
            var fileName = Path.GetFileNameWithoutExtension(mediaFilePath);
            var pattern = @"\.srt|\.sub";

            var files = GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
            return files.ToArray();
        }

        // Regex version
        private static IEnumerable<string> GetFiles(string path, string searchPatternExpression, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression);
            return Directory.EnumerateFiles(path, "*", searchOption).Where(file => reSearchPattern.IsMatch(Path.GetExtension(file)));
        }
    }
}
