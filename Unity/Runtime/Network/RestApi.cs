using System;

namespace Portfolio.Game.Network
{
    /// <summary>
    /// 원본 MiningWarrior의 RestApi facade에 대응한다.
    /// LoginProcess는 HTTP 구현이 아니라 이 API 메서드만 호출한다.
    /// </summary>
    public static class RestApi
    {
        public static void SetSession(LoginSession session)
            => HttpServiceBase.Instance.SetSession(session);

        public static void Authenticate(GoogleLoginRequest request, Action<GoogleLoginResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void CustomLogin(CustomLoginRequest request, Action<GoogleLoginResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void GetAppVersion(GetAppVersionRequest request, Action<GetAppVersionResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void GetPlayerAccount(GetPlayerAccountRequest request, Action<GetPlayerAccountResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void UpdatePlayerAccount(UpdatePlayerAccountRequest request, Action<UpdatePlayerAccountResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void GetServerData(GetServerDataRequest request, Action<GetServerDataResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void GetPlayerInfo(GetPlayerInfoRequest request, Action<GetPlayerInfoResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void PurchaseItem(PurchaseItemRequest request, Action<PurchaseItemResponse> callback, Action<ApiError> error)
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);

        public static void ValidatePurchase<TRequest>(TRequest request, Action<ValidatePurchaseResponse> callback, Action<ApiError> error)
            where TRequest : IApiRequest<ValidatePurchaseResponse>
            => HttpServiceBase.Instance.MakeApiCall(request, callback, error);
    }
}
