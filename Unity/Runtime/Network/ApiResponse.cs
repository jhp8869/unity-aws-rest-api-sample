using System;

namespace Portfolio.Game.Network
{
    [Serializable]
    public sealed class ApiResponse
    {
        public int retCode;
        public string body;
        public string error;
    }
}
