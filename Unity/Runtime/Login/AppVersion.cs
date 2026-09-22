using System;
using UnityEngine;

namespace Portfolio.Game.Login
{
    [Serializable]
    public sealed class AppVersion : IEquatable<AppVersion>
    {
        public int Major;
        public int Minor;

        public static AppVersion FromUnityVersion()
        {
            string[] parts = Application.version.Split('.');
            int.TryParse(parts.Length > 0 ? parts[0] : "0", out int major);
            int.TryParse(parts.Length > 1 ? parts[1] : "0", out int minor);
            return new AppVersion { Major = major, Minor = minor };
        }

        public bool Equals(AppVersion other) => other != null && Major == other.Major && Minor == other.Minor;
        public override bool Equals(object obj) => Equals(obj as AppVersion);
        public override int GetHashCode() => (Major, Minor).GetHashCode();
    }

    [Serializable]
    public sealed class ServiceStatusData
    {
        public AppVersion[] AcceptedVersion;
        public string State;
        public string Message;

        public bool IsChecking() => string.Equals(State, "Checking", StringComparison.OrdinalIgnoreCase);

        public bool IsAcceptVersion(AppVersion version)
        {
            if (AcceptedVersion == null || version == null) return false;
            foreach (AppVersion accepted in AcceptedVersion)
                if (accepted != null && accepted.Equals(version)) return true;
            return false;
        }
    }
}
