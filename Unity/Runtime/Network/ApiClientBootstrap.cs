using UnityEngine;

namespace Portfolio.Game.Network
{
    /// <summary>
    /// UnityRestClient 는 일반 C# 클래스라 인스펙터에 직접 연결할 수 없다.
    /// 이 컴포넌트가 씬에서 클라이언트와 세션 저장소를 만들어 다른 컴포넌트에 제공한다.
    /// (원본 프로젝트에서는 DI 컨테이너가 이 역할을 한다.)
    /// </summary>
    public sealed class ApiClientBootstrap : MonoBehaviour
    {
        [SerializeField] private string baseUrl = "https://your-api-id.execute-api.ap-northeast-2.amazonaws.com/dev";
        [SerializeField] private int maxAttempts = 3;
        [SerializeField] private int timeoutSeconds = 10;

        public HttpServiceBase HttpService { get; private set; }
        // 기존 SampleView가 사용하므로 유지한다. LoginProcess는 RestApi를 사용한다.
        public UnityRestClient Client => HttpService?.Client;
        public SessionStore SessionStore { get; private set; }

        private void Awake()
        {
            SessionStore = new SessionStore();
            HttpService = gameObject.GetComponent<HttpServiceBase>();
            if (HttpService == null)
                HttpService = gameObject.AddComponent<HttpServiceBase>();

            HttpService.Initialize(baseUrl, SessionStore, maxAttempts, timeoutSeconds);
        }
    }
}
