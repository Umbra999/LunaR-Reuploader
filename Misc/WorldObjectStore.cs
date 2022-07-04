using System;
using System.Threading;
using System.Threading.Tasks;
using LunarUploader.VRChatApi;
using LunarUploader.VRChatApi.Models;

namespace LunarUploader.Misc 
{
    internal class WorldObjectStore : FileObjectStore
    {
        private string _UnityVersion;
        private bool _quest;
        private readonly string _Name;

        internal WorldObjectStore(VRChatApiClient client, string name, string unityversion, string path, bool quest = false, CancellationToken? ct = null) : base(client, path) 
        {
            _UnityVersion = unityversion;
            _quest = quest;
            _Name = name;
        }

        internal override async Task Reupload() 
        {
            try 
            {

                var friendlyAssetBundleName = GetFriendlyWorldName(_UnityVersion , _quest ? "android" : "standalonewindows");

                if (!await CustomApiFileHelper.UploadFile(_apiClient, _path, friendlyAssetBundleName, string.Empty, true, OnAvatarUploadSuccess, OnAvatarUploadFailure).ConfigureAwait(false)) Console.WriteLine("Failed to upload world!");
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        private void OnAvatarUploadSuccess(CustomApiFile file) 
        {
            FileUrl = file.GetFileUrl();
            Console.WriteLine($"World uri: {FileUrl}");
        }

        private void OnAvatarUploadFailure(string error) 
        {
            Console.WriteLine($"World error: {error}");
        }

        private string GetFriendlyWorldName(string unityVersion, string platform)
        {
            return "World - " + _Name + " - Asset bundle - " + unityVersion + "_4_" + platform + "_Release";
        }
    }
}
