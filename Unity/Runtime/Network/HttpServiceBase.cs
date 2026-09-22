using System;
using UnityEngine;

namespace Portfolio.Game.Network
{
    /// <summary>
    /// 원본 MiningWarrior의 HttpServiceBase에 대응하는 HTTP 실행 계층이다.
    /// API 호출자는 UnityWebRequest와 코루틴을 직접 다루지 않는다.
    /// </summary>
    public sealed class HttpServiceBase : MonoBehaviour
    {
        public static HttpServiceBase Instance { get; private set; }
        private UnityRestClient client;

        // 기존 구매 샘플과의 호환용이다. 신규 API 호출은 RestApi를 사용한다.
        public UnityRestClient Client => client;

        private void Awake()
        {
            Instance = this;
        }

        public void Initialize(
            string baseUrl,
            ISessionStore sessionStore,
            int maxAttempts,
            int timeoutSeconds)
        {
            client = new UnityRestClient(
                this,
                baseUrl,
                sessionStore,
                maxAttempts,
                timeoutSeconds: timeoutSeconds);
        }

        public void MakeApiCall<TRequest, TResponse>(
            TRequest request,
            Action<TResponse> onSuccess,
            Action<ApiError> onError)
            where TRequest : IApiRequest<TResponse>
        {
            if (client == null)
            {
                onError?.Invoke(new ApiError(ApiErrorCode.NetworkError, "HttpServiceBase is not initialized."));
                return;
            }

            client.MakeApiCall(request, onSuccess, onError);
        }
    }
}
