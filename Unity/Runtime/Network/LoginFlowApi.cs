using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Portfolio.Game.Login;

namespace Portfolio.Game.Network
{
    public sealed class GetAppVersionRequest : IApiRequest<GetAppVersionResponse>
    {
        [JsonIgnore] public string Endpoint => "GetAppVersion";
        [JsonIgnore] public AuthType AuthType => AuthType.None;
    }

    public sealed class GetAppVersionResponse
    {
        public ServiceStatusData Version;
        public string Time;
    }

    public sealed class GetPlayerAccountRequest : IApiRequest<GetPlayerAccountResponse>
    {
        [JsonIgnore] public string Endpoint => "GetPlayerAccount";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;
        public string PlayerId;
    }

    public sealed class GetPlayerAccountResponse
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> fields;

        [JsonIgnore]
        public JObject AccountData => JObject.FromObject(fields ?? new Dictionary<string, JToken>());
    }

    public sealed class UpdatePlayerAccountRequest : IApiRequest<UpdatePlayerAccountResponse>, IApiRequestPayload
    {
        [JsonIgnore] public string Endpoint => "UpdatePlayerAccount";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;
        public string PlayerId;
        public JObject AccountData;

        public string ToJson()
        {
            JObject payload = AccountData == null ? new JObject() : (JObject)AccountData.DeepClone();
            payload["PlayerId"] = PlayerId;
            return payload.ToString(Formatting.None);
        }
    }

    public sealed class UpdatePlayerAccountResponse { }

    public sealed class GetServerDataRequest : IApiRequest<GetServerDataResponse>
    {
        [JsonIgnore] public string Endpoint => "GetServerData";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;
        public JObject RequestData = new JObject
        {
            ["ServerList"] = new JObject(),
            ["ServerState"] = new JObject()
        };
    }

    public sealed class GetServerDataResponse
    {
        public JObject ServerList;
        public JObject ServerState;

        public JObject GetData(string type)
        {
            return type == "ServerList" ? ServerList : type == "ServerState" ? ServerState : null;
        }
    }

    public sealed class GetPlayerInfoRequest : IApiRequest<GetPlayerInfoResponse>
    {
        [JsonIgnore] public string Endpoint => "GetPlayerInfo";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;
        public string PlayerId;
    }

    public sealed class GetPlayerInfoResponse
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> fields;

        [JsonIgnore]
        public JObject PlayerInfo => JObject.FromObject(fields ?? new Dictionary<string, JToken>());
    }
}
