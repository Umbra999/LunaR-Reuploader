using LunarUploader.Misc;
using LunarUploader.VRChatApi;
using LunarUploader.VRChatApi.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LunarUploader
{
    internal class ReuploadHelper
    {
        public static string SDKVersion = "2021.09.30.16.29";
        public readonly VRChatApiClient apiClient;
        public readonly CustomApiUser customApiUser;

        public ReuploadHelper(string Username, string Password, string ProxyIP = null, string ProxyUser = null, string ProxyPW = null)
        {
            apiClient = new VRChatApiClient(10, GenerateHWID(), ProxyIP != null ? new WebProxy() { Address = new Uri("http://" + ProxyIP), Credentials = new NetworkCredential(ProxyUser, ProxyPW) } : null);
            customApiUser = apiClient.CustomApiUser.Login(Username, Password).Result;
        }

        public static string GenerateHWID()
        {
            var random = new Random(Environment.TickCount);
            byte[] bytes = new byte[20];
            random.NextBytes(bytes);
            string HWID = string.Join("", bytes.Select(it => it.ToString("x2")));
            return HWID;
        }

        internal async Task ReUploadAvatarAsync(string Name, string AssetPath, string ImagePath, bool Private)
        {
            string AvatarID = $"avtr_{Guid.NewGuid()}";

            AssetsToolsObjectStore assetsToolsObject = new(AssetPath, "", AvatarID, AssetsToolsObjectStore.AssetsToolsObjectType.Avatar, true, CancellationToken.None);
            assetsToolsObject.LoadAndUnpack();
            assetsToolsObject.ReplaceAvatarId();
            assetsToolsObject.ReplaceCAB();
            assetsToolsObject.PackAndSave();

            if (assetsToolsObject.Error) return;

            string reuploadedAvatarPath = assetsToolsObject.AssetsFilePath;
            var avatarFile = new AvatarObjectStore(apiClient, Name, apiClient.CustomRemoteConfig.SdkUnityVersion, reuploadedAvatarPath);
            await avatarFile.Reupload().ConfigureAwait(false);

            ImageObjectStore imageFile = new(apiClient, Name, ImagePath, apiClient.CustomRemoteConfig.SdkUnityVersion);
            await imageFile.Reupload().ConfigureAwait(false);

            CustomApiAvatar newAvatar = await new CustomApiAvatar(apiClient)
            {
                assetUrl = avatarFile.FileUrl,
                assetVersion = 1,
                authorId = customApiUser.Id,
                authorName = customApiUser.DisplayName,
                created_at = "01.01.0001 00:00:00",
                description = Name,
                Id = AvatarID,
                imageUrl = imageFile.FileUrl,
                name = Name,
                platform = "standalonewindows",
                releaseStatus = Private ? "private" : "public",
                tags = new string[0],
                totalLikes = 0,
                totalVisits = 0,
                unityVersion = apiClient.CustomRemoteConfig.SdkUnityVersion,
                updated_at = "01.01.0001 00:00:00",
                
            }.Post().ConfigureAwait(false);

            if (newAvatar == null) Console.WriteLine("Failed to upload Avatar");
            else Console.WriteLine($"Avatar Uploaded: {newAvatar.name} [{newAvatar.Id}]");

            Console.ReadLine();
        }

        internal async Task ReUploadWorldAsync(string Name, string AssetPath, string ImagePath, int Capacity)
        {
            string WorldID = $"wrld_{Guid.NewGuid()}";

            AssetsToolsObjectStore assetsToolsObject = new(AssetPath, "", WorldID, AssetsToolsObjectStore.AssetsToolsObjectType.World, true, CancellationToken.None);
            assetsToolsObject.LoadAndUnpack();
            assetsToolsObject.ReplaceWorldId();
            assetsToolsObject.ReplaceCAB();
            assetsToolsObject.PackAndSave();

            if (assetsToolsObject.Error) return;

            string reuploadedWorldPath = assetsToolsObject.AssetsFilePath;
            WorldObjectStore worldFile = new(apiClient, Name, apiClient.CustomRemoteConfig.SdkUnityVersion, reuploadedWorldPath);
            await worldFile.Reupload().ConfigureAwait(false);

            var imageFile = new ImageObjectStore(apiClient, Name, ImagePath, apiClient.CustomRemoteConfig.SdkUnityVersion);
            await imageFile.Reupload().ConfigureAwait(false);

            CustomApiWorld newWorld = await new CustomApiWorld(apiClient)
            {
                assetUrl = worldFile.FileUrl,
                assetVersion = 4,
                authorId = customApiUser.Id,
                authorName = customApiUser.DisplayName,
                capacity = Capacity,
                createdAt = "01.01.0001 00:00:00",
                created_at = "01.01.0001 00:00:00",
                description = Name,
                Id = WorldID,
                imageUrl = imageFile.FileUrl,
                latestAssetVersion = 0,
                name = Name,
                occupants = 0,
                platform = "standalonewindows",
                privateOccupants = 0,
                publicOccupants = 0,
                publicationDate = "01.01.0001 00:00:00",
                releaseStatus = "private",
                tags = new string[0],
                unityVersion = apiClient.CustomRemoteConfig.SdkUnityVersion,
                updated_at = "01.01.0001 00:00:00"

            }.Post().ConfigureAwait(false);

            if (newWorld == null) Console.WriteLine("Failed to upload World");
            else Console.WriteLine($"World Uploaded: {newWorld.name} [{newWorld.Id}]");

            Console.ReadLine();
        }

        internal async Task DeleteAvatarAsync(string AvatarID)
        {
            CustomApiAvatar Avatar = new(apiClient);
            Avatar = await Avatar.Get(AvatarID);
            await Avatar.Delete();
        }

        internal async Task DeleteWorldAsync(string WorldID)
        {
            CustomApiWorld World = new(apiClient);
            World = await World.Get(WorldID);
            await World.Delete();
        }
    }
}