namespace Portfolio.Game.Network
{
    public interface ISessionStore
    {
        string IdToken { get; }
        string AccessToken { get; }
        string RefreshToken { get; }
        bool ShouldRefresh { get; }
        void SetSession(LoginSession session);
        void UpdateTokens(string idToken, string accessToken);
    }
}
