using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using LunarUploader.VRChatApi.Models;
using LunarUploader;

namespace LunarUploader.VRChatApi 
{
    
    public class VRChatApiClient 
    {
        public ObjectStore ObjectStore { get; set; }

        public CustomRemoteConfig CustomRemoteConfig { get; set; }
        public CustomApiUser CustomApiUser { get; set; }
        public CustomApiAvatar CustomApiAvatar { get; set; }
        public CustomApiFile CustomApiFile { get; set; }
        public CustomApiWorld CustomApiWorld { get; set; }

        public HttpClient HttpClient;

        public HttpFactory HttpFactory;

        public VRChatApiClient(int objectStoreSize = 15, string hmac = "", WebProxy Proxy = null) 
        {
            ObjectStore = new ObjectStore(objectStoreSize);
            Initialize(hmac, Proxy);

            CustomRemoteConfig = new CustomRemoteConfig(this);
            CustomApiUser = new CustomApiUser(this);
            CustomApiAvatar = new CustomApiAvatar(this);
            CustomApiWorld = new CustomApiWorld(this);
            CustomApiFile = new CustomApiFile(this);
        }

        private void Initialize(string hmac, WebProxy Proxy = null) 
        {
            ObjectStore["ApiUri"] = new Uri("https://api.vrchat.cloud/api/1/", UriKind.Absolute);
            ObjectStore["CookieContainer"] = new CookieContainer();

            ObjectStore["HttpClientHandler"] = new HttpClientHandler() 
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = true,
                CookieContainer = (CookieContainer) ObjectStore["CookieContainer"],
                Proxy = Proxy,
            };
            ObjectStore["HttpClient"] = new HttpClient((HttpClientHandler) ObjectStore["HttpClientHandler"], true) 
            {
                BaseAddress = (Uri) ObjectStore["ApiUri"],
                Timeout = TimeSpan.FromMinutes(90)
            };

            HttpClient = (HttpClient) ObjectStore["HttpClient"];

            ObjectStore["HttpFactory"] = new HttpFactory(HttpClient);
            HttpFactory = (HttpFactory) ObjectStore["HttpFactory"];

            if (string.IsNullOrEmpty(hmac)) hmac = ReuploadHelper.GenerateHWID();
            HttpRequestHeaders requestHeaders = HttpClient.DefaultRequestHeaders;

            requestHeaders.Clear();
            requestHeaders.UserAgent.ParseAdd("VRC.Core.BestHTTP");
            requestHeaders.Host = "api.vrchat.cloud";
            requestHeaders.Add("Origin", "vrchat.com");
            requestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            requestHeaders.Add("X-SDK-Version", ReuploadHelper.SDKVersion);
            requestHeaders.Add("X-Platform", "standalonewindows");
            requestHeaders.Add("X-MacAddress", hmac);
        }

        public string GetApiKeyAsQuery() 
        {
            return $"?apiKey=JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26&organization=vrchat";
        }

        public string GetApiKeyAsExtraQuery()
        {
            return $"&apiKey=JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26&organization=vrchat";
        }
    }
}