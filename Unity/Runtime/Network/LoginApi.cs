using Newtonsoft.Json;

namespace Portfolio.Game.Network
{
    /// <summary>
    /// MiningWarrior의 실제 GoogleLogin 요청 형식에 맞춘다.
    /// </summary>
    public sealed class GoogleLoginRequest : IApiRequest<GoogleLoginResponse>
    {
        [JsonIgnore] public string Endpoint => "GoogleLogin";
        [JsonIgnore] public AuthType AuthType => AuthType.None;

        [JsonProperty("authorization_code")]
        public string AuthorizationCode;
        public string Timezone;
        public bool register;
    }

    public sealed class GoogleLoginResponse
    {
        public string PlayerId;
        public string IdToken;
        public string AccessToken;
        public string RefreshToken;
        public string TokenType;

        public LoginSession ToSession()
        {
            return new LoginSession
            {
                PlayerId = PlayerId,
                IdToken = IdToken,
                AccessToken = AccessToken,
                RefreshToken = RefreshToken,
                TokenType = TokenType
            };
        }
    }

    /// <summary>
    /// 실제 Unity Editor 로그인 분기에서 사용하는 CustomLogin 요청 형식이다.
    /// </summary>
    public sealed class CustomLoginRequest : IApiRequest<GoogleLoginResponse>
    {
        [JsonIgnore] public string Endpoint => "CustomLogin";
        [JsonIgnore] public AuthType AuthType => AuthType.None;

        public string customId;
        public string customPw;
        public string Timezone;
        public bool register;
    }
}
