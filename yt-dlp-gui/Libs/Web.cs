using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        public static async Task Download(string downloadUrl, string savePath, IProgress<double> progress = null, string proxyUrl = null) {
            Debug.WriteLine($"save {downloadUrl} to {savePath} use {proxyUrl}");
            var httpClientHandler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(proxyUrl)) {
                var proxyUri = new Uri(proxyUrl);
                var proxy = new WebProxy(proxyUri);
                // 解析代理伺服器 URL 中的帳號和密碼
                if (!string.IsNullOrEmpty(proxyUri.UserInfo)) {
                    var userPass = proxyUri.UserInfo.Split(':');
                    var username = Uri.UnescapeDataString(userPass[0]);
                    var password = Uri.UnescapeDataString(userPass[1]);
                    var credentials = new NetworkCredential(username, password);
                    proxy.Credentials = credentials;
                    httpClientHandler.Proxy = proxy;
                    httpClientHandler.UseProxy = true;
                }

                httpClientHandler.Proxy = proxy;
                httpClientHandler.UseProxy = true;
            }
            var httpClient = new HttpClient(httpClientHandler);

            var fileName = downloadUrl.Substring(downloadUrl.LastIndexOf("/") + 1);
            //var fileExt = Path.GetExtension(fileName);
            //var filePath = Path.ChangeExtension(savePath, fileExt);

            var response = httpClient.GetAsync(downloadUrl).Result;
            if (response.IsSuccessStatusCode) {
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength == null) {
                    throw new Exception("Content length not found");
                }

                var totalBytes = contentLength.Value;
                var downloadedBytes = 0L;
                using (var contentStream = response.Content.ReadAsStreamAsync().Result) {
                    using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write)) {
                        var buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = contentStream.ReadAsync(buffer, 0, buffer.Length).Result) > 0) {
                            fileStream.WriteAsync(buffer, 0, bytesRead).Wait();
                            downloadedBytes += bytesRead;

                            if (progress != null) {
                                var percentage = (double)downloadedBytes / totalBytes * 100;
                                progress.Report(percentage);
                            }
                        }
                    }
                }
            } else {
                Console.WriteLine($"Failed to download file. Status code: {response.StatusCode}");
            }
        }
    }
    public class WebProxy :IWebProxy {
        private readonly Uri _proxyUri;
        public WebProxy(Uri proxyUri) {
            _proxyUri = proxyUri;
        }
        public ICredentials? Credentials { get; set; }
        public Uri? GetProxy(Uri destination) {
            return _proxyUri;
        }

        public bool IsBypassed(Uri host) {
            return false;
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
