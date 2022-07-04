using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LunarUploader.VRChatApi.Models {
    
    public class CustomApiWorld : CustomApiModel 
    {
        [JsonProperty("name")] public string name { get; set; }

        [JsonProperty("description")] public string description { get; set; }

        [JsonProperty("featured")] public bool featured { get; set; }

        [JsonProperty("authorId")] public string authorId { get; set; }

        [JsonProperty("authorName")] public string authorName { get; set; }

        [JsonProperty("capacity")] public int capacity { get; set; }

        [JsonProperty("tags")] public string[] tags { get; set; }

        [JsonProperty("releaseStatus")] public string releaseStatus { get; set; }

        [JsonProperty("platform")] public string platform { get; set; }

        [JsonProperty("imageUrl")] public string imageUrl { get; set; }

        [JsonProperty("thumbnailImageUrl")] public string thumbnailImageUrl { get; set; }

        [JsonProperty("assetUrl")] public string assetUrl { get; set; }

        [JsonProperty("version")] public int version { get; set; }

        [JsonProperty("assetVersion")] public int assetVersion { get; set; }

        [JsonProperty("organization")] public string organization { get; set; }

        [JsonProperty("unityVersion")] public string unityVersion { get; set; }

        [JsonProperty("previewYoutubeId")] public object previewYoutubeId { get; set; }

        [JsonProperty("favorites")] public int favorites { get; set; }

        [JsonProperty("created_at")] public string created_at { get; set; }

        [JsonProperty("createdAt")] public string createdAt { get; set; }

        [JsonProperty("updated_at")] public string updated_at { get; set; }

        [JsonProperty("latestAssetVersion")] public int latestAssetVersion { get; set; }

        [JsonProperty("publicationDate")] public string publicationDate { get; set; }

        [JsonProperty("labsPublicationDate")] public string labsPublicationDate { get; set; }

        [JsonProperty("visits")] public int visits { get; set; }

        [JsonProperty("popularity")] public int popularity { get; set; }

        [JsonProperty("heat")] public int heat { get; set; }

        [JsonProperty("publicOccupants")] public int publicOccupants { get; set; }

        [JsonProperty("privateOccupants")] public int privateOccupants { get; set; }

        [JsonProperty("occupants")] public int occupants { get; set; }

        [JsonProperty("instances")] public List<List<object>> instances { get; set; }

        public CustomApiWorld(VRChatApiClient apiClient) : base(apiClient, "worlds") { }

        public async Task<CustomApiWorld> Get(string id) 
        {
            var ret = await ApiClient.HttpFactory.GetAsync<CustomApiWorld>(MakeRequestEndpoint() + $"/{id}" + ApiClient.GetApiKeyAsQuery()).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }

        public async Task<CustomApiWorld> Save() 
        {
            CustomApiWorld ret;
            if (string.IsNullOrEmpty(Id)) ret = await ApiClient.HttpFactory.PostAsync<CustomApiWorld>(MakeRequestEndpoint() + ApiClient.GetApiKeyAsQuery(), ToJsonContent(this)).ConfigureAwait(false);
            else ret = await ApiClient.HttpFactory.PutAsync<CustomApiWorld>(MakeRequestEndpoint() + ApiClient.GetApiKeyAsQuery(), ToJsonContent(this)).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }

        public async Task<CustomApiWorld> Post() 
        {
            var ret = await ApiClient.HttpFactory.PostAsync<CustomApiWorld>(MakeRequestEndpoint(false) + ApiClient.GetApiKeyAsQuery(), WorldPostJsonContent(this)).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }

        public async Task<CustomApiWorld> PutNameDescriptionImage()
        {
            var ret = await ApiClient.HttpFactory.PutAsync<CustomApiWorld>(MakeRequestEndpoint() + ApiClient.GetApiKeyAsQuery(), WorldPutJsonContentNameDescriptionImage(this)).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }

        public async Task<CustomApiWorld> Delete() 
        {
            var ret = await ApiClient.HttpFactory.DeleteAsync<CustomApiWorld>(MakeRequestEndpoint() + ApiClient.GetApiKeyAsQuery()).ConfigureAwait(false);
            ret.ApiClient = ApiClient;
            return ret;
        }
    }
}