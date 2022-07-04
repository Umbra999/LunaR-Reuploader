using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace LunarUploader.VRChatApi
{
    public class HttpFactory 
    {
        private readonly HttpClient _httpClient;
        public HttpFactory(HttpClient httpClient) 
        {
            _httpClient = httpClient;
        }

        public async Task<TJson> GetAsync<TJson>(string uri, CancellationToken? ct = null) where TJson : class 
        {
            try 
            {
                using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) Console.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                response.EnsureSuccessStatusCode();
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var streamReader = new StreamReader(contentStream);
                using var jsonReader = new JsonTextReader(streamReader);
                return JsonSerializer.Create().Deserialize<TJson>(jsonReader);
            } 
            catch (Exception e) 
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public async Task<string> GetStringAsync(string uri, CancellationToken? ct = null) 
        {
            try 
            {
                using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false);
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var streamReader = new StreamReader(contentStream);
                return await streamReader.ReadToEndAsync().ConfigureAwait(false);
            } catch (Exception e) 
            {
                Console.WriteLine(e);
                _httpClient.DefaultRequestHeaders.Remove("Content-Type");
            }

            return string.Empty;
        }

        public async Task<TJson> PostAsync<TJson>(string uri, HttpContent content, CancellationToken? ct = null) where TJson : class {
            try 
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, uri) {Content = content};
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false);      
                if (!response.IsSuccessStatusCode) Console.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                response.EnsureSuccessStatusCode();
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var streamReader = new StreamReader(contentStream);
                using var jsonReader = new JsonTextReader(streamReader);
                return JsonSerializer.Create().Deserialize<TJson>(jsonReader);
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            return null;
        }

        public async Task<TJson> PutAsync<TJson>(string uri, HttpContent content, CancellationToken? ct = null) where TJson : class 
        {
            try 
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, uri) { Content = content };
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) Console.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                response.EnsureSuccessStatusCode();
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var streamReader = new StreamReader(contentStream);
                using var jsonReader = new JsonTextReader(streamReader);
                return JsonSerializer.Create().Deserialize<TJson>(jsonReader);
            } 
            catch (Exception e) 
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public async Task<string> PutStringAsync(string uri, HttpContent content, HttpClient awsHttpClient = null, CancellationToken? ct = null) {
            try {
                awsHttpClient ??= _httpClient;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                using var request = new HttpRequestMessage(HttpMethod.Put, uri) { Content = content };
                using var response = await awsHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false);
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var streamReader = new StreamReader(contentStream);
                return await streamReader.ReadToEndAsync().ConfigureAwait(false);
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            return string.Empty;
        }

        public async Task<(string, EntityTagHeaderValue)> PutStringAndETagAsync(string uri, HttpContent content, HttpClient awsHttpClient = null, CancellationToken? ct = null)
        {
            try 
            {
                awsHttpClient ??= _httpClient;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                using var request = new HttpRequestMessage(HttpMethod.Put, uri) { Content = content };
                using var response = await awsHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false);
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var streamReader = new StreamReader(contentStream);
                return (await streamReader.ReadToEndAsync().ConfigureAwait(false), response.Headers.ETag);
            } catch (Exception e) {
                Console.WriteLine(e);
            }

            return (string.Empty, EntityTagHeaderValue.Any);
        }

        public async Task<TJson> DeleteAsync<TJson>(string uri, CancellationToken? ct = null) where TJson : class 
        {
            try 
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) Console.WriteLine(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                response.EnsureSuccessStatusCode();
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var streamReader = new StreamReader(contentStream);
                using var jsonReader = new JsonTextReader(streamReader);
                return JsonSerializer.Create().Deserialize<TJson>(jsonReader);
            }
            catch (Exception e) 
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public async Task<string> DownloadToRandomPathAsync(string uri, CancellationToken? ct = null, IProgress<double> progress = null) 
        {
            try 
            {
                using var response = (await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct ?? CancellationToken.None).ConfigureAwait(false)).EnsureSuccessStatusCode();
                var contentHeaders = response.Content.Headers;
                var contentLength = contentHeaders.ContentLength;
                var mediaType = contentHeaders.ContentType.MediaType;
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var path = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + GetExtensionFromMimetype(mediaType));
                var bufferSize = contentLength.HasValue && contentLength.Value > Math.Pow(1024, 2) * 10 ? 1 << 15 : 1 << 17;
                using var fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous);
                var buffer = new byte[bufferSize];
                long read = 0;
                int chunk = 0;
                while ((chunk = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct ?? CancellationToken.None).ConfigureAwait(false)) > 0) 
                {
                    read += chunk;
                    if (progress != null && contentLength.HasValue)  progress.Report(Math.Round((double)read / contentLength.Value * 100, 2));
                    await fileStream.WriteAsync(buffer, 0, chunk, ct ?? CancellationToken.None).ConfigureAwait(false);
                }

                if (progress != null && contentLength.HasValue) progress.Report(Math.Round((double)read / contentLength.Value * 100, 2));
                return path;
            } 
            catch (Exception e) 
            {
                Console.WriteLine(e);
            }

            return string.Empty;
        }

        private static string GetExtensionFromMimetype(string mimeType)
        {
            mimeType = mimeType.ToLower();
            if (mimeType == "application/x-world") return ".vrcw";
            if (mimeType == "application/x-avatar") return ".vrca";
            if (mimeType == "application/x-msdownload") return ".dll";
            if (mimeType == ".application/gzip") return ".unitypackage";
            if (mimeType == "application/gzip") return ".gz";
            if (mimeType == "image/jpg") return ".jpg";
            if (mimeType == "image/jpeg") return ".jpeg";
            if (mimeType == "image/png") return ".png";
            if (mimeType == "application/x-rsync-signature") return ".sig";
            if (mimeType == "application/x-rsync-delta") return ".delta";

            Console.WriteLine("Unknown file mime-type for extension: " + mimeType);
            return "";
        }
    }

    public class ProgressableByteArrayContent : HttpContent 
    {
        private readonly byte[] _content;
        private readonly int _offset;
        private readonly int _count;
        private readonly IProgress<double> _progress;
        private readonly int _chunkSize;

        public ProgressableByteArrayContent(byte[] content, IProgress<double> progress, int chunkSize = 131072) : this(content, 0, content.Length, progress, chunkSize) { }

        public ProgressableByteArrayContent(byte[] content, int offset, int count, IProgress<double> progress, int chunkSize = 131072) 
        {
            _content = content;
            _offset = offset;
            _count = count;
            _progress = progress;
            _chunkSize = ChangeChunkSizeIfNecessary(chunkSize);
        }

        private int ChangeChunkSizeIfNecessary(int chunkSize) 
        {
            return _count < chunkSize ? _count : chunkSize;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) 
        {
            long written = 0;
            for (int i = 0; i < _content.Length; i += _chunkSize) 
            {
                int count = Math.Min(_chunkSize, _content.Length - i);
                written += count;
                _progress.Report(Math.Round((double)written / _count * 100, 2));
                await stream.WriteAsync(_content, i, count).ConfigureAwait(false);
            }

            _progress.Report(Math.Round((double)written / _count * 100, 2));
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _count;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() 
        {
            return Task.FromResult<Stream>(new MemoryStream(_content, _offset, _count, false, false));
        }
    }

    public class ProgressableStringContent : ProgressableByteArrayContent
    {
        private const string _defaultMediaType = "text/plain";

        public ProgressableStringContent(string content, IProgress<double> progress) : this(content, null, progress){
            
        }

        public ProgressableStringContent(string content, Encoding encoding, IProgress<double> progress) : this(content, encoding, null, progress) { }

        public ProgressableStringContent(string content, Encoding encoding, string mediaType, IProgress<double> progress) : base(GetContentByteArray(content, encoding), progress) 
        {
            Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(mediaType) ? _defaultMediaType : mediaType) { CharSet = encoding == null ? Encoding.UTF8.WebName : encoding.WebName };
        }

        private static byte[] GetContentByteArray(string content, Encoding encoding) 
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetBytes(content);
        }
    }

    public class ProgressableJsonContent : ProgressableStringContent 
    {
        public ProgressableJsonContent(string content, IProgress<double> progress) : base(content, Encoding.UTF8, "application/json", progress) { }
    }

    public class JsonContent : StringContent 
    {
        public JsonContent(string content) : base(content, Encoding.UTF8, "application/json") { }
    }
}
