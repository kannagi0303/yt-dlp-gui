using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Libs {
    public class Web {
        public static async Task<(string content, Uri ResponseUri, bool isRedirect)> Load(string url, string encoding = "") {
            var client = new HttpClient();
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
            }
            return (body, ResponseUri, isRedirect);
        }
        public static bool Head(string url) {
            var client = new HttpClient();
            var uri = new Uri(url);
            var res = client.Send(new HttpRequestMessage(HttpMethod.Head, uri));
            return res.StatusCode == System.Net.HttpStatusCode.OK;
        }
    }
}
