using Newtonsoft.Json;

namespace Portfolio.Game.Network
{
    public sealed class LoginWithProviderRequest : IApiRequest<LoginWithProviderResponse>
    {
        [JsonIgnore] public string Endpoint => "LoginWithProvider";
        [JsonIgnore] public AuthType AuthType => AuthType.None;

        public string authorizationCode;
        public string timezoneOffset;
        public bool register;
    }

    public sealed class LoginWithProviderResponse
    {
        public string playerId;
        public string idToken;
        public string accessToken;
        public string refreshToken;
        public string tokenType;

        public LoginSession ToSession()
        {
            return new LoginSession
            {
                PlayerId = playerId,
                IdToken = idToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = tokenType
            };
        }
    }
}
