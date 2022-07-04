using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LunarUploader.VRChatApi.Models {
    
    public class CustomApiAvatar : CustomApiModel 
    {
        [JsonProperty(PropertyName = "apiVersion")] public int apiVersion { get; set; }

        [JsonProperty(PropertyName = "assetUrl")] public string assetUrl { get; set; }

        [JsonProperty(PropertyName = "assetVersion")] public int assetVersion{ get; set; }

        [JsonProperty(PropertyName = "authorId")] public string authorId { get; set; }

        [JsonProperty(PropertyName = "authorName")] public string authorName { get; set; }

        [JsonProperty(PropertyName = "created_at")] public string created_at { get; set; }

        [JsonProperty(PropertyName = "updated_at")] public string updated_at { get; set; }

        [JsonProperty(PropertyName = "description")] public string description { get; set; }

        [JsonProperty(PropertyName = "featured")] public bool featured { get; set; }

        [JsonProperty(PropertyName = "imageUrl")] public string imageUrl { get; set; }

        [JsonProperty(PropertyName = "name")] public string name { get; set; }

        [JsonProperty(PropertyName = "platform")] public string platform { get; set; }

        [JsonProperty(PropertyName = "releaseStatus")] public string releaseStatus { get; set; }

        [JsonProperty(PropertyName = "tags")] public string[] tags { get; set; }

        [JsonProperty(PropertyName = "thumbnailImageUrl")] public string thumbnailImageUrl { get; set; }

        [JsonProperty(PropertyName = "unityPackageUrl")] public string unityPackageUrl { get; set; }

        [JsonProperty(PropertyName = "totalLikes")] public int totalLikes { get; set; }

        [JsonProperty(PropertyName = "totalVisits")] public int totalVisits { get; set; }

        [JsonProperty(PropertyName = "unityVersion")] public string unityVersion { get; set; }

        [JsonProperty(PropertyName = "version")] public int version { get; set; }

        public CustomApiAvatar(VRChatApiClient apiClient) : base(apiClient, "avatars") { }

        public async Task<CustomApiAvatar> Get(string id) {
            var ret = await ApiClient.HttpFactory.GetAsync<CustomApiAvatar>(MakeRequestEndpoint() + $"/{id}" + ApiClient.GetApiKeyAsQuery()).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }

        public async Task<CustomApiAvatar> Post() 
        {
            var ret = await ApiClient.HttpFactory.PostAsync<CustomApiAvatar>(MakeRequestEndpoint(false) + ApiClient.GetApiKeyAsQuery(), AvatarPostJsonContent(this)).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }

        public async Task<CustomApiAvatar> PutNameDescriptionImage()
        {
            var ret = await ApiClient.HttpFactory.PutAsync<CustomApiAvatar>(MakeRequestEndpoint() + ApiClient.GetApiKeyAsQuery(), AvatarPutJsonContentNameDescriptionImage(this)).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }

        public async Task<CustomApiAvatar> Delete()
        {
            var ret = await ApiClient.HttpFactory.DeleteAsync<CustomApiAvatar>(MakeRequestEndpoint() + ApiClient.GetApiKeyAsQuery()).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }
    }
}