using System.IO;
using System.Text;

namespace EZPlayer.Subtitle
{
    public static class SubtitleUtil
    {
        public static void PrepareSubtitle(string mediaFilePath)
        {
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
            var pattern = string.Format("*.srt", fileName);
            var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
            return files;
        }
    }
}
