using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using LunarUploader.VRChatApi;
using LunarUploader;

namespace LunarUploader.Misc 
{
    internal static class DownloadHelper 
    {
        private static CancellationTokenSource _cancellationTokenSource;
        private static HttpClientHandler _httpClientHandler;
        private static HttpClient _httpClient;
        private static HttpFactory _httpFactory;

        internal static CancellationToken CancellationToken 
        {
            get => _cancellationTokenSource?.Token ?? CancellationToken.None;
        }

        internal static HttpClient HttpClient 
        {
            get => _httpClient;
        }

        public static void Setup(string UnityVersion) 
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _httpClientHandler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            _httpClient = new HttpClient(_httpClientHandler, true) { Timeout = TimeSpan.FromMinutes(90) };
            var requestHeaders = _httpClient.DefaultRequestHeaders;

            requestHeaders.Clear();
            requestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            requestHeaders.UserAgent.ParseAdd("UnityPlayer/2019.4.31f1 (UnityWebRequest/1.0, libcurl/7.75.0-DEV)");
            requestHeaders.Add("Host", "api.vrchat.cloud");
            requestHeaders.Add("X-Unity-Version", UnityVersion);
            _httpFactory = new HttpFactory(_httpClient);
        }


        public static string DownloadToRandomPath(string uri,IProgress<double> progress = null) => _httpFactory.DownloadToRandomPathAsync(uri, CancellationToken, progress).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
