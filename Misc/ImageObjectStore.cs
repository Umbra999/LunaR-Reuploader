using System;
using System.Threading.Tasks;
using LunarUploader.VRChatApi;
using LunarUploader.VRChatApi.Models;

namespace LunarUploader.Misc
{
    internal class ImageObjectStore : FileObjectStore 
    {
        private readonly bool _isQuest;
        private readonly string _UnityVersion;
        private readonly bool _deleteFiles;
        private readonly string _existingId;
        private readonly string _Name;

        public ImageObjectStore(VRChatApiClient client, string Name, string path, string UnityVersion, bool quest = false, bool deleteFiles = true, string existingId = "") : base(client, path)
        {
            _isQuest = quest;
            _UnityVersion = UnityVersion;
            _deleteFiles = deleteFiles;
            _existingId = existingId;
            _Name = Name;
        }

        internal override async Task Reupload() 
        {
            try
            {
                string friendlyImageName = friendlyImageName = GetFriendlyImageName(_UnityVersion, _isQuest ? "android" : "standalonewindows");
                if (!await CustomApiFileHelper.UploadFile(_apiClient, _path, friendlyImageName, _existingId, _deleteFiles, OnImageUploadSuccess, OnImageUploadFailure).ConfigureAwait(false)) Console.WriteLine("Failed to upload image!");

            } catch (Exception e) 
            {
                Console.WriteLine(e);
            }
        }

        private void OnImageUploadSuccess(CustomApiFile file)
        {
            FileUrl = file.GetFileUrl();
            Console.WriteLine($"Image uri: {FileUrl}");
        }

        private void OnImageUploadFailure(string error) {
            Console.WriteLine($"Image error: {error}");
        }
        private string GetFriendlyImageName(string unityVersion, string platform) 
        {
            return "Avatar - " + _Name + " - Image- " + unityVersion + "_4_" + platform + "_Release";
        }
    }
}
