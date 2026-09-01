using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Portfolio.Game.Network
{
    public sealed class LoginSession
    {
        public string PlayerId;
        public string IdToken;
        public string AccessToken;
        public string RefreshToken;
        public string TokenType;
    }

    public sealed class SessionStore : ISessionStore
    {
        private LoginSession current;

        public string IdToken => current?.IdToken ?? string.Empty;
        public string AccessToken => current?.AccessToken ?? string.Empty;
        public string RefreshToken => current?.RefreshToken ?? string.Empty;

        public bool ShouldRefresh
        {
            get
            {
                DateTime? expiresAt = GetJwtExpiration(IdToken);
                return expiresAt.HasValue && (expiresAt.Value - DateTime.UtcNow).TotalMinutes < 10;
            }
        }

        public void SetSession(LoginSession session)
        {
            current = session;
        }

        public void UpdateTokens(string idToken, string accessToken)
        {
            if (current == null)
                current = new LoginSession();

            current.IdToken = idToken;
            current.AccessToken = accessToken;
        }

        private static DateTime? GetJwtExpiration(string jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt))
                return null;

            string[] parts = jwt.Split('.');
            if (parts.Length != 3)
                return null;

            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (values != null && values.TryGetValue("exp", out object exp))
                    return DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(exp)).UtcDateTime;
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
