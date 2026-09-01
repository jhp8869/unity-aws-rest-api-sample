using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Portfolio.Game.Network
{
    public sealed class UnityRestClient
    {
        private readonly string baseUrl;
        private readonly ISessionStore sessionStore;
        private readonly int maxRetries;
        private readonly float baseDelaySeconds;

        public UnityRestClient(
            string baseUrl,
            ISessionStore sessionStore,
            int maxRetries = 3,
            float baseDelaySeconds = 1.5f)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.sessionStore = sessionStore;
            this.maxRetries = Math.Max(1, maxRetries);
            this.baseDelaySeconds = Math.Max(0.1f, baseDelaySeconds);
        }

        public IEnumerator Post<TRequest, TResponse>(
            TRequest request,
            Action<TResponse> onSuccess,
            Action<ApiError> onError)
            where TRequest : IApiRequest<TResponse>
        {
            string url = $"{baseUrl}/{request.Endpoint}";
            string json = JsonConvert.SerializeObject(request);
            byte[] payload = Encoding.UTF8.GetBytes(json);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var webRequest = CreatePostRequest(url, payload, request.AuthType);
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    HandleResponse(webRequest.downloadHandler.text, onSuccess, onError);
                    yield break;
                }

                if (attempt < maxRetries)
                {
                    float delay = Mathf.Pow(baseDelaySeconds, attempt);
                    yield return new WaitForSeconds(delay);
                    continue;
                }

                onError?.Invoke(new ApiError(-1, webRequest.error));
            }
        }

        private UnityWebRequest CreatePostRequest(string url, byte[] payload, AuthType authType)
        {
            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(payload),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");

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
                onError?.Invoke(new ApiError(-2, $"Invalid response format: {e.Message}"));
                return;
            }

            if (response == null)
            {
                onError?.Invoke(new ApiError(-2, "Empty response"));
                return;
            }

            if (response.retCode != 0)
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
                onError?.Invoke(new ApiError(-3, $"Invalid response body format: {e.Message}"));
            }
        }
    }
}
