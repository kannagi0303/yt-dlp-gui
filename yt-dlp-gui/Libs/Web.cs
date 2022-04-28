using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Libs {
    public class Web {
        public static async Task<(string content, Uri ResponseUri, bool isRedirect)> Load(string url, string encoding = "") {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "yt-dlp-gui");
            var uri = new Uri(url);
            var res = await client.GetAsync(uri);

            var body = "";
            var ResponseUri = uri;
            var isRedirect = false;
            if (res.IsSuccessStatusCode) {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); //註冊Encoding擴展支援
                if (string.IsNullOrWhiteSpace(encoding)) {
                    body = await res.Content.ReadAsStringAsync();
                } else {
                    var enc = Encoding.GetEncoding(encoding);
                    body = await res.Content.ReadAsByteArrayAsync().ContinueWith(ba => enc.GetString(ba.Result));
                }
                ResponseUri = res.RequestMessage?.RequestUri ?? uri;
                isRedirect = !uri.Equals(ResponseUri);
            } else {
                Debug.WriteLine(res.StatusCode);
                Debug.WriteLine(res.Content);
            }
            
            return (body, ResponseUri, isRedirect);
        }
        public static bool Head(string url) {
            var client = new HttpClient();
            var uri = new Uri(url);
            var res = client.Send(new HttpRequestMessage(HttpMethod.Head, uri));
            return res.StatusCode == System.Net.HttpStatusCode.OK;
        }
        public static async Task<List<GitRelease>> GetLastTag() {
            var res = await Load(@"https://api.github.com/repos/Kannagi0303/yt-dlp-gui/releases");
            if (!string.IsNullOrWhiteSpace(res.content)) {
                try {
                    return JsonConvert.DeserializeObject<List<GitRelease>>(res.content);
                } catch { };
            }
            return null;
        }
    }
    public class GitRelease {
        public string tag_name { get; set; } = string.Empty;
        public string body { get; set; } = string.Empty;
        public List<GitReleaseAssets> assets { get; set; } = new();
    }
    public class GitReleaseAssets {
        public string name { get; set; } = string.Empty;
        public string browser_download_url { get; set; } = string.Empty;
    }
}
