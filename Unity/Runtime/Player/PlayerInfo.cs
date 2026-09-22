using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Portfolio.Game.Player
{
    [Serializable]
    public sealed class PlayerInfo
    {
        public string PlayerId;
        public string ServerId;
        public string DisplayName;
        public DateTime? ReserveDeleteDate;
        public BanInfo BanInfo = new BanInfo();
        public Dictionary<string, string> DeviceInfo = new Dictionary<string, string>();
        public Dictionary<AgreementContents, bool> Agreement = new Dictionary<AgreementContents, bool>();
        public JObject AccountData = new JObject();
        public JObject PlayerData = new JObject();
        public JObject PlayerDatas => PlayerData;

        public enum AgreementContents
        {
            ConsentToPersonalInfoCollection,
            ConsentToTermsAndConditions,
            ConsentToReceiveAds,
            ConsentToNightlyNotifications
        }

        public void ApplyAccount(JObject data)
        {
            if (data == null) return;
            AccountData = data;
            PlayerId = data.Value<string>("PlayerId") ?? PlayerId;
            ServerId = data.Value<string>("ServerId") ?? ServerId;
            DisplayName = data.Value<string>("DisplayName") ?? DisplayName;

            JObject agreement = data["Agreement"] as JObject;
            if (agreement != null)
            {
                foreach (AgreementContents content in Enum.GetValues(typeof(AgreementContents)))
                {
                    if (agreement.TryGetValue(content.ToString(), out JToken value))
                        Agreement[content] = value.Value<bool>();
                }
            }

            JToken deleteAccount = data["DeleteAccount"];
            if (deleteAccount == null || string.IsNullOrWhiteSpace(deleteAccount.ToString()))
                ReserveDeleteDate = null;
            else if (long.TryParse(deleteAccount.ToString(), out long timestamp))
                ReserveDeleteDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

            JObject ban = data["BanInfo"] as JObject;
            if (ban != null)
            {
                BanInfo.Active = ban.Value<bool?>("Active") ?? false;
                BanInfo.Reason = ban.Value<string>("Reason");
            }
        }

        /// <summary>원본 PlayerInfo.Parse와 같은 의미의 누적 파싱 진입점이다.</summary>
        public void Parse(JObject data) => ApplyAccount(data);

        public static Dictionary<AgreementContents, bool> ParseAgreement(JObject data)
        {
            var result = new Dictionary<AgreementContents, bool>();
            JObject agreement = data?["Agreement"] as JObject;
            if (agreement == null) return result;

            foreach (AgreementContents content in Enum.GetValues(typeof(AgreementContents)))
            {
                if (agreement.TryGetValue(content.ToString(), out JToken value))
                    result[content] = value.Value<bool>();
            }
            return result;
        }

        public Dictionary<string, string> CollectDeviceInfo()
        {
            DeviceInfo["deviceModel"] = SystemInfo.deviceModel;
            DeviceInfo["deviceName"] = SystemInfo.deviceName;
            DeviceInfo["operatingSystem"] = SystemInfo.operatingSystem;
            DeviceInfo["processorType"] = SystemInfo.processorType;
            return new Dictionary<string, string>(DeviceInfo);
        }

        public void ApplyPlayerData(JObject data)
        {
            if (data != null) PlayerData = data;
        }
    }

    [Serializable]
    public sealed class BanInfo
    {
        public bool Active;
        public string Reason;
    }
}
