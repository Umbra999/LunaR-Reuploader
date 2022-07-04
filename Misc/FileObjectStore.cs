using System;
using System.Threading.Tasks;
using LunarUploader.VRChatApi;

namespace LunarUploader.Misc 
{
    internal abstract class FileObjectStore 
    {
        private protected VRChatApiClient _apiClient;
        private protected string _path;

        internal string FileUrl { get; private protected set; }

        internal FileObjectStore(VRChatApiClient client, string path) 
        {
            _apiClient = client;
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            _path = path;
        }

        internal virtual Task Reupload() 
        {
            return Task.CompletedTask;
        }
    }
}
