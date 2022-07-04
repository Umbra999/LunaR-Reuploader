using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LunarUploader.VRChatApi.Models 
{
    
    public class CustomApiUser : CustomApiModel 
    {
        #region ApiFields

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("userIcon")]
        public string UserIcon { get; set; }

        [JsonProperty("bio")]
        public string Bio { get; set; }

        [JsonProperty("bioLinks")]
        public List<object> BioLinks { get; set; }

        [JsonProperty("pastDisplayNames")]
        public List<object> PastDisplayNames { get; set; }

        [JsonProperty("hasEmail")]
        public bool HasEmail { get; set; }

        [JsonProperty("hasPendingEmail")]
        public bool HasPendingEmail { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("obfuscatedEmail")]
        public string ObfuscatedEmail { get; set; }

        [JsonProperty("obfuscatedPendingEmail")]
        public string ObfuscatedPendingEmail { get; set; }

        [JsonProperty("emailVerified")]
        public bool EmailVerified { get; set; }

        [JsonProperty("hasBirthday")]
        public bool HasBirthday { get; set; }

        [JsonProperty("unsubscribe")]
        public bool Unsubscribe { get; set; }

        [JsonProperty("friends")]
        public List<string> Friends { get; set; }

        [JsonProperty("friendGroupNames")]
        public List<object> FriendGroupNames { get; set; }

        [JsonProperty("currentAvatarImageUrl")]
        public string CurrentAvatarImageUrl { get; set; }

        [JsonProperty("currentAvatarThumbnailImageUrl")]
        public string CurrentAvatarThumbnailImageUrl { get; set; }

        [JsonProperty("fallbackAvatar")]
        public string FallbackAvatar { get; set; }

        [JsonProperty("currentAvatar")]
        public string CurrentAvatar { get; set; }

        [JsonProperty("currentAvatarAssetUrl")]
        public string CurrentAvatarAssetUrl { get; set; }

        [JsonProperty("accountDeletionDate")]
        public object AccountDeletionDate { get; set; }

        [JsonProperty("acceptedTOSVersion")]
        public int AcceptedTOSVersion { get; set; }

        [JsonProperty("steamId")]
        public string SteamId { get; set; }

        [JsonProperty("oculusId")]
        public string OculusId { get; set; }

        [JsonProperty("hasLoggedInFromClient")]
        public bool HasLoggedInFromClient { get; set; }

        [JsonProperty("homeLocation")]
        public string HomeLocation { get; set; }

        [JsonProperty("twoFactorAuthEnabled")]
        public bool TwoFactorAuthEnabled { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusDescription")]
        public string StatusDescription { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("developerType")]
        public string DeveloperType { get; set; }

        [JsonProperty("last_login")]
        public DateTime? LastLogin { get; set; }

        [JsonProperty("last_platform")]
        public string LastPlatform { get; set; }

        [JsonProperty("allowAvatarCopying")]
        public bool AllowAvatarCopying { get; set; }

        [JsonProperty("date_joined")]
        public string DateJoined { get; set; }

        [JsonProperty("isFriend")]
        public bool IsFriend { get; set; }

        [JsonProperty("friendKey")]
        public string FriendKey { get; set; }

        [JsonProperty("onlineFriends")]
        public List<object> OnlineFriends { get; set; }

        [JsonProperty("activeFriends")]
        public List<object> ActiveFriends { get; set; }

        [JsonProperty("offlineFriends")]
        public List<string> OfflineFriends { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("worldId")]
        public string WorldId { get; set; }

        [JsonProperty("instanceId")]
        public string InstanceId { get; set; }

        #endregion

        public CustomApiUser(VRChatApiClient apiClient) : base(apiClient, "users") { }


        public async Task<CustomApiUser> Login(string usernameOrEmail, string password)
        {
            var httpClient = ApiClient.HttpClient;
            var requestHeaders = httpClient.DefaultRequestHeaders;
            requestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(usernameOrEmail)}:{Uri.EscapeDataString(password)}")));

            var res = await ApiClient.HttpFactory.GetStringAsync("auth/user" + ApiClient.GetApiKeyAsQuery()).ConfigureAwait(false);
            if (res.Contains("Invalid Username or Password") || res.Contains("Missing Credentials"))
            {
                Console.WriteLine("Invalid credentials!");
                return null;
            }

            CustomApiUser apiUser = JsonConvert.DeserializeObject<CustomApiUser>(res);
            apiUser.ApiClient = ApiClient;
            foreach (Cookie cookie in ((CookieContainer)ApiClient.ObjectStore["CookieContainer"]).GetCookies((Uri)ApiClient.ObjectStore["ApiUri"]))
            {
                if (cookie.Name.Equals("auth", StringComparison.OrdinalIgnoreCase)) ApiClient.ObjectStore["AuthCookie"] = cookie.Value;
            }

            requestHeaders.Remove("Authorization");
            return apiUser;
        }
    }
}