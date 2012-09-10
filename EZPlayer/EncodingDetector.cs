using Ude;

namespace EZPlayer
{
    public class EncodingDetector
    {
        public static string Detect(byte[] fileContent)
        {
            var detector = new CharsetDetector();
            detector.Feed(fileContent, 0, fileContent.Length);
            detector.DataEnd();

            var charset = detector.Charset;
            if(charset.ToLower() == "big-5")
            {
                charset = charset.Replace("-", "");
            }
            return charset;
        }
    }
}
