using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Portfolio.Game.Network;
using Portfolio.Game.Player;

namespace Portfolio.Game.Login
{
    /// <summary>
    /// 실제 MiningWarrior LoginProcess를 포트폴리오 샘플에 맞게 옮긴 첫 씬 진입점이다.
    /// UI 팝업과 Google Play SDK는 이벤트로 분리하고, 서버 통신 순서는 원본과 동일하게 유지한다.
    /// </summary>
    public sealed class LoginProcess : MonoBehaviour
    {
        private AppVersion appversion = new AppVersion();
        [SerializeField] private string mainSceneName = "Main";

        public event Action<string> Failure;
        public event Action<string> AccountBanDetected;
        public event Action<DateTime> DeleteRecoveryRequired;
        public event Action<JObject> AgreementRequired;
        public event Action<GetServerDataResponse> ServerSelectionRequired;
        public event Action MainSceneReady;

        private void Start()
        {
            CheckAppVersion();
        }

        /// <summary>실제 LoginProcess.Start()와 같이 버전/점검을 로그인보다 먼저 확인한다.</summary>
        public void CheckAppVersion()
        {
            RestApi.GetAppVersion(new GetAppVersionRequest(), OnAppVersionLoaded, OnApiError);
        }

        public void AndroidLogin(string authorizationCode, bool register = false, string timezone = null)
        {
            RestApi.Authenticate(
                new GoogleLoginRequest
                {
                    AuthorizationCode = authorizationCode,
                    Timezone = timezone ?? TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).ToString(@"hh\:mm"),
                    register = register
                },
                OnLoginSucceeded,
                OnApiError));
        }

        public void EditorLogin(string customId, string customPassword, bool register = false, string timezone = null)
        {
            RestApi.CustomLogin(
                new CustomLoginRequest
                {
                    customId = customId,
                    customPw = customPassword,
                    Timezone = timezone ?? TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).ToString(@"hh\:mm"),
                    register = register
                },
                OnLoginSucceeded,
                OnApiError));
        }

        /// <summary>탈퇴 예약 계정 팝업에서 취소를 선택했을 때 호출한다.</summary>
        public void CancelDeleteAndContinue()
        {
            UpdateAccount(new JObject { ["DeleteAccount"] = null }, DownloadAccount);
        }

        /// <summary>약관 팝업에서 필수 약관 동의를 완료했을 때 호출한다.</summary>
        public void AcceptRequiredAgreements(JObject agreement)
        {
            UpdateAccount(new JObject { ["Agreement"] = agreement }, DownloadAccount);
        }

        /// <summary>서버 선택 UI가 선택한 실제 ServerInfo JSON을 전달한다.</summary>
        public void SelectServer(JObject serverInfo)
        {
            JObject selected = new JObject { ["SelectServer"] = serverInfo };
            UpdateAccount(selected, LoadPlayerInfo);
        }

        private void OnAppVersionLoaded(GetAppVersionResponse result)
        {
            appversion.Refresh();
            if (result?.Version == null || result.Version.IsChecking())
            {
                Fail(result?.Version?.Message ?? "Server is under maintenance.");
                return;
            }
            if (!result.Version.IsAcceptVersion(appversion))
            {
                Fail("App update required for this service version.");
                return;
            }
        }

        private void OnLoginSucceeded(GoogleLoginResponse result)
        {
            RestApi.SetSession(result.ToSession());
            // 원본 MiningWarrior LoginProcess와 동일하게 로그인 직후 현재 플레이어에 저장한다.
            PlayerManager.Instance.GetCurrentPlayer.PlayerId = result.PlayerId;
            DownloadAccount();
        }

        private void DownloadAccount()
        {
            RestApi.GetPlayerAccount(
                new GetPlayerAccountRequest { PlayerId = PlayerManager.Instance.GetCurrentPlayer.PlayerId },
                OnAccountLoaded,
                OnApiError));
        }

        private void OnAccountLoaded(GetPlayerAccountResponse result)
        {
            PlayerManager.Instance.GetCurrentPlayer.ApplyAccount(result.AccountData);
            PlayerInfo current = PlayerManager.Instance.GetCurrentPlayer;

            if (current.BanInfo.Active)
            {
                AccountBanDetected?.Invoke(current.BanInfo.Reason);
                return;
            }
            if (current.ReserveDeleteDate.HasValue)
            {
                DeleteRecoveryRequired?.Invoke(current.ReserveDeleteDate.Value);
                return;
            }
            if (!HasRequiredAgreement(current.AccountData))
            {
                AgreementRequired?.Invoke(current.AccountData["Agreement"] as JObject ?? new JObject());
                return;
            }

            LoadServerData();
        }

        private void LoadServerData()
        {
            RestApi.GetServerData(new GetServerDataRequest(), OnServerDataLoaded, OnApiError);
        }

        private void OnServerDataLoaded(GetServerDataResponse result)
        {
            JObject serverList = result?.GetData("ServerList");
            if (serverList == null || !serverList.HasValues)
            {
                Fail("No available server was returned.");
                return;
            }
            if (serverList.Count == 1)
            {
                foreach (JProperty server in serverList.Properties())
                {
                    SelectServer(server.Value as JObject ?? new JObject { ["id"] = server.Name });
                    return;
                }
            }
            ServerSelectionRequired?.Invoke(result);
        }

        private void LoadPlayerInfo()
        {
            RestApi.GetPlayerInfo(
                new GetPlayerInfoRequest { PlayerId = PlayerManager.Instance.GetCurrentPlayer.PlayerId },
                result =>
                {
                    PlayerManager.Instance.GetCurrentPlayer.ApplyPlayerData(result.PlayerInfo);
                    MainSceneReady?.Invoke();
                    if (!string.IsNullOrEmpty(mainSceneName)) SceneManager.LoadScene(mainSceneName);
                },
                OnApiError));
        }

        private void UpdateAccount(JObject data, Action onSuccess)
        {
            RestApi.UpdatePlayerAccount(
                new UpdatePlayerAccountRequest
                {
                    PlayerId = PlayerManager.Instance.GetCurrentPlayer.PlayerId,
                    AccountData = data
                },
                _ => onSuccess?.Invoke(),
                OnApiError));
        }

        private static bool HasRequiredAgreement(JObject account)
        {
            JObject agreement = account?["Agreement"] as JObject;
            return agreement?.Value<bool?>("ConsentToPersonalInfoCollection") == true
                && agreement.Value<bool?>("ConsentToTermsAndConditions") == true;
        }

        private void OnApiError(ApiError error) => Fail(error?.Message ?? "REST request failed.");

        private void Fail(string message)
        {
            Failure?.Invoke(message);
        }
    }
}
