using CookComputing.XmlRpc;

namespace SubtitleTools.Infrastructure.Core.OpenSubtitlesOrg.API
{
    public static class OpenSubtitlesFactory
    {
        public static IOpenSubtitlesDb Create(string userAgent, string url)
        {
            var ret = XmlRpcProxyGen.Create<IOpenSubtitlesDb>();
            ret.Url = url;
            ret.Expect100Continue = false;
            ret.UserAgent = userAgent;
            return ret;
        }
    }
}
