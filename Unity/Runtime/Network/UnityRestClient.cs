using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Portfolio.Game.Network
{
    /// <summary>
    /// UnityWebRequest 기반 REST 클라이언트.
    ///
    /// 재시도 정책
    /// - 재시도 대상: 연결 오류, HTTP 5xx. (4xx 는 다시 보내도 같은 답이므로 즉시 실패)
    /// - 모든 시도에 같은 <c>Idempotency-Key</c> 를 붙인다. "요청은 서버에 도착했지만 응답이 유실된"
    ///   경우 재시도가 구매를 두 번 처리하지 않도록 서버가 이 키로 중복을 걸러낼 수 있게 한다.
    /// - 지수 백오프 + 지터.
    /// </summary>
    public sealed class UnityRestClient
    {
        private readonly MonoBehaviour coroutineRunner;
        private readonly string baseUrl;
        private readonly ISessionStore sessionStore;
        private readonly int maxAttempts;
        private readonly float baseDelaySeconds;
        private readonly int timeoutSeconds;

        public UnityRestClient(
            MonoBehaviour coroutineRunner,
            string baseUrl,
            ISessionStore sessionStore,
            int maxAttempts = 3,
            float baseDelaySeconds = 1f,
            int timeoutSeconds = 10)
        {
            this.coroutineRunner = coroutineRunner;
            this.baseUrl = baseUrl.TrimEnd('/');
            this.sessionStore = sessionStore;
            this.maxAttempts = Math.Max(1, maxAttempts);
            this.baseDelaySeconds = Math.Max(0.1f, baseDelaySeconds);
            this.timeoutSeconds = Math.Max(1, timeoutSeconds);
        }

        /// <summary>
        /// 원본 HttpServiceBase.MakeApiCall에 대응하는 REST 진입점이다.
        /// 호출자는 코루틴을 직접 실행하지 않고 성공/실패 콜백만 받는다.
        /// </summary>
        public void MakeApiCall<TRequest, TResponse>(
            TRequest request,
            Action<TResponse> onSuccess,
            Action<ApiError> onError)
            where TRequest : IApiRequest<TResponse>
        {
            coroutineRunner.StartCoroutine(Post(request, onSuccess, onError));
        }

        private IEnumerator Post<TRequest, TResponse>(
            TRequest request,
            Action<TResponse> onSuccess,
            Action<ApiError> onError)
            where TRequest : IApiRequest<TResponse>
        {
            string url = $"{baseUrl}/{request.Endpoint}";
            string json = request is IApiRequestPayload payload
                ? payload.ToJson()
                : JsonConvert.SerializeObject(request);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            string idempotencyKey = Guid.NewGuid().ToString();

            ApiError lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                bool retryable;
                using (var webRequest = CreatePostRequest(url, payload, request.AuthType, idempotencyKey))
                {
                    yield return webRequest.SendWebRequest();

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        HandleResponse(webRequest.downloadHandler.text, onSuccess, onError);
                        yield break;
                    }

                    bool serverError = webRequest.responseCode >= 500;
                    bool connectionError = webRequest.result == UnityWebRequest.Result.ConnectionError;
                    retryable = serverError || connectionError;
                    lastError = new ApiError(ApiErrorCode.NetworkError, $"{webRequest.responseCode} {webRequest.error}");
                }

                if (!retryable)
                {
                    // 4xx 등: 재시도해도 같은 결과
                    onError?.Invoke(lastError);
                    yield break;
                }

                if (attempt < maxAttempts)
                {
                    float delay = baseDelaySeconds * Mathf.Pow(2f, attempt - 1) * UnityEngine.Random.Range(0.8f, 1.2f);
                    yield return new WaitForSeconds(delay);
                }
            }

            onError?.Invoke(lastError);
        }

        private UnityWebRequest CreatePostRequest(string url, byte[] payload, AuthType authType, string idempotencyKey)
        {
            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(payload),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = timeoutSeconds
            };

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Idempotency-Key", idempotencyKey);

            if (authType == AuthType.Login)
                request.SetRequestHeader("Authorization", sessionStore.IdToken);

            return request;
        }

        private static void HandleResponse<TResponse>(
            string responseText,
            Action<TResponse> onSuccess,
            Action<ApiError> onError)
        {
            ApiResponse response;

            try
            {
                response = JsonConvert.DeserializeObject<ApiResponse>(responseText);
            }
            catch (Exception e)
            {
                onError?.Invoke(new ApiError(ApiErrorCode.InvalidResponse, $"Invalid response format: {e.Message}"));
                return;
            }

            if (response == null)
            {
                onError?.Invoke(new ApiError(ApiErrorCode.InvalidResponse, "Empty response"));
                return;
            }

            if (response.retCode != ApiErrorCode.Success)
            {
                onError?.Invoke(new ApiError(response.retCode, response.error));
                return;
            }

            try
            {
                TResponse body = JsonConvert.DeserializeObject<TResponse>(response.body);
                onSuccess?.Invoke(body);
            }
            catch (Exception e)
            {
                onError?.Invoke(new ApiError(ApiErrorCode.InvalidResponseBody, $"Invalid response body format: {e.Message}"));
            }
        }
    }
}
