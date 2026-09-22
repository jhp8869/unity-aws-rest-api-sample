using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public JObject AccountData;
    }

    public sealed class UpdatePlayerAccountRequest : IApiRequest<UpdatePlayerAccountResponse>
    {
        [JsonIgnore] public string Endpoint => "UpdatePlayerAccount";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;
        public string PlayerId;
        public JObject AccountData;
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
        public JObject PlayerInfo;
    }
}
