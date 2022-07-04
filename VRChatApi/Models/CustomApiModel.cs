using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LunarUploader.VRChatApi.Models
{
    public abstract class CustomApiModel 
    {
        [JsonIgnore] public static JsonSerializerSettings SerializerSettings = new() {NullValueHandling = NullValueHandling.Ignore};

        [JsonIgnore] public VRChatApiClient ApiClient;

        [JsonIgnore] public string Endpoint { get; set; }

        [JsonProperty("id")] public string Id { get; set; }

        public CustomApiModel(VRChatApiClient apiClient) 
        {
            Endpoint = null;
            ApiClient = apiClient;
        }

        public CustomApiModel(string endpoint) 
        {
            Endpoint = endpoint;
            ApiClient = null;
        }

        public CustomApiModel(VRChatApiClient apiClient, string endpoint)
        {
            Endpoint = endpoint;
            ApiClient = apiClient;
        }

        public string MakeRequestEndpoint(bool includeId = true) 
        {
            return Endpoint + (!string.IsNullOrEmpty(Id) && includeId ? $"/{Id}" : string.Empty);
        }

        public static JsonContent AvatarPostJsonContent(CustomApiAvatar caa) 
        {
            Dictionary<string, object> avatarDict = new()
            {
                ["assetUrl"] = caa.assetUrl,
                ["assetVersion"] = caa.assetVersion.ToString(),
                ["authorId"] = caa.authorId,
                ["authorName"] = caa.authorName,
                ["created_at"] = caa.created_at,
                ["description"] = caa.description,
                ["id"] = caa.Id,
                ["imageUrl"] = caa.imageUrl,
                ["name"] = caa.name,
                ["platform"] = caa.platform,
                ["releaseStatus"] = caa.releaseStatus,
                ["tags"] = caa.tags,
                ["totalLikes"] = caa.totalLikes.ToString(),
                ["totalVisits"] = caa.totalVisits.ToString(),
                ["unityVersion"] = caa.unityVersion,
                ["updated_at"] = caa.updated_at,
            };
            return new JsonContent(JsonConvert.SerializeObject(avatarDict, SerializerSettings));
        }

        public static JsonContent AvatarPutJsonContentNameDescriptionImage(CustomApiAvatar caa)
        {
            Dictionary<string, object> avatarDict = new Dictionary<string, object>
            {
                ["id"] = caa.Id,
                ["name"] = caa.name,
                ["imageUrl"] = caa.imageUrl,
                ["description"] = caa.description
            };
            return new JsonContent(JsonConvert.SerializeObject(avatarDict, SerializerSettings));
        }

        public static JsonContent WorldPostJsonContent(CustomApiWorld caw) 
        {
            Dictionary<string, object> worldDict = new()
            {
                ["assetUrl"] = caw.assetUrl,
                ["assetVersion"] = caw.assetVersion.ToString(),
                ["authorId"] = caw.authorId,
                ["authorName"] = caw.authorName,
                ["capacity"] = caw.capacity,
                ["createdAt"] = caw.createdAt,
                ["created_at"] = caw.created_at,
                ["description"] = caw.description,
                ["id"] = caw.Id,
                ["imageUrl"] = caw.imageUrl,
                ["latestAssetVersion"] = caw.latestAssetVersion.ToString(),
                ["name"] = caw.name,
                ["occupants"] = caw.occupants,
                ["platform"] = caw.platform,
                ["privateOccupants"] = caw.privateOccupants,
                ["publicOccupants"] = caw.publicOccupants,
                ["publicationDate"] = caw.publicationDate,
                ["releaseStatus"] = caw.releaseStatus,
                ["tags"] = caw.tags,
                ["unityVersion"] = caw.unityVersion,
                ["updated_at"] = caw.updated_at,
                ["thumbnailImageUrl"] = caw.thumbnailImageUrl,
            };
            return new JsonContent(JsonConvert.SerializeObject(worldDict, SerializerSettings));
        }

        public static JsonContent WorldPutJsonContentNameDescriptionImage(CustomApiWorld caw) 
        {
            var worldDict = new Dictionary<string, object>();
            worldDict["id"] = caw.Id;
            worldDict["name"] = caw.name;
            worldDict["imageUrl"] = caw.imageUrl;
            worldDict["description"] = caw.description;
            return new JsonContent(JsonConvert.SerializeObject(worldDict, SerializerSettings));
        }

        public static StringContent ToJsonContent<T>(T serialize) where T : CustomApiModel 
        {
            return new StringContent(JsonConvert.SerializeObject(serialize, SerializerSettings), Encoding.UTF8, "application/json");
        }
    }
}