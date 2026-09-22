using UnityEngine;
using UnityEngine.UI;
using Portfolio.Game.Network;

namespace Portfolio.Game.SampleView
{
    /// <summary>
    /// 스토어 결제 결과 → 서버 영수증 검증 → 보상 반영 흐름.
    /// 실제 프로젝트에서는 UnityIapGooglePurchaseService.PurchaseCompleted 이벤트에서 이 흐름을 시작하고,
    /// 서버 검증 성공 후에만 Unity IAP 의 pending order 를 confirm 한다.
    /// </summary>
    public sealed class PurchaseValidationSampleView : MonoBehaviour, IInventorySync
    {
        [SerializeField] private ApiRequestQueue requestQueue;

        [Header("Player")]
        [SerializeField] private string playerId = "sample-player-id";
        [SerializeField] private string productId = "ruby_pack_01";

        [Header("UI")]
        [SerializeField] private Button googlePurchaseButton;
        [SerializeField] private Text statusText;

        private void Awake()
        {
            googlePurchaseButton.onClick.AddListener(ValidateGooglePurchase);
            requestQueue.Failed += OnQueueFailed;
            SetStatus("Waiting for store purchase");
        }

        private void OnDestroy()
        {
            if (requestQueue != null) requestQueue.Failed -= OnQueueFailed;
        }

        private void ValidateGooglePurchase()
        {
            StorePurchaseResult purchase = CreateSampleGooglePurchaseResult();
            if (!purchase.Success)
            {
                SetStatus(purchase.Message);
                return;
            }

            var receipt = purchase.Receipt as GoogleReceipt;
            var request = new ValidateGooglePurchaseRequest
            {
                PlayerId = playerId,
                ReceiptJson = receipt?.PayloadData?.json,
                Signature = receipt?.PayloadData?.signature,
                CurrencyCode = purchase.CurrencyCode,
                Price = purchase.Price
            };

            SetStatus("Validating receipt on server...");

            var command = new ValidatePurchaseCommand<ValidateGooglePurchaseRequest>(
                request,
                this,
                _ => SetStatus("Receipt validated and reward synced — confirm pending order here"));

            requestQueue.Enqueue(command);
        }

        private void OnQueueFailed(ApiError error)
        {
            if (error.Code == ApiErrorCode.InvalidPurchase)
            {
                // 스토어가 거부한 영수증: 재시도 금지. pending order 는 confirm 하지 않고 남겨둔다.
                SetStatus("Store rejected the receipt");
                return;
            }

            if (error.IsRetryable)
            {
                // 네트워크/서버 오류: 서버는 purchaseToken 으로 중복 지급을 막으므로 같은 영수증을 다시 보내도 안전하다.
                SetStatus($"Temporary failure, will retry later: {error}");
                return;
            }

            SetStatus($"Validation failed: {error}");
        }

        private StorePurchaseResult CreateSampleGooglePurchaseResult()
        {
            const string receiptJson = "{\"orderId\":\"GPA.0000-0000\",\"packageName\":\"com.sample.app\",\"productId\":\"ruby_pack_01\",\"purchaseTime\":1720000000000,\"purchaseState\":0,\"purchaseToken\":\"sample-purchase-token\"}";
            string payload = "{\"json\":" + JsonEscape(receiptJson) + ",\"signature\":\"sample-signature\"}";
            string wrapper = "{\"Payload\":" + JsonEscape(payload) + "}";

            return new StorePurchaseResult
            {
                Success = true,
                ProductId = productId,
                CurrencyCode = "KRW",
                Price = 1100,
                Receipt = GoogleReceipt.FromJson(wrapper)
            };
        }

        private static string JsonEscape(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        public void ApplyPurchasedItem(ItemSnapshot item) => SetStatus($"Purchased: {item.itemId} x{item.amount}");
        public void ApplyGrantedItem(ItemSnapshot item) => SetStatus($"Granted: {item.itemId} x{item.amount}");
        public void ApplyConsumedItem(ItemSnapshot item) => SetStatus($"Consumed: {item.itemId} x{item.amount}");

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;

            Debug.Log(message);
        }
    }
}
