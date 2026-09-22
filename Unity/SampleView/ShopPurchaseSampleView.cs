using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Portfolio.Game.Network;

namespace Portfolio.Game.SampleView
{
    public sealed class ShopPurchaseSampleView : MonoBehaviour, IPlayerContext, IInventorySync
    {
        [SerializeField] private ApiRequestQueue requestQueue;

        [Header("Player")]
        [SerializeField] private string playerId = "sample-player-id";
        [SerializeField] private string shopId = "normal_shop";
        [SerializeField] private string itemId = "gold_pack_01";

        [Header("UI")]
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Text statusText;

        public string PlayerId => playerId;

        private void Awake()
        {
            purchaseButton.onClick.AddListener(PurchaseItem);
            requestQueue.Failed += OnQueueFailed;
            SetStatus("Ready");
        }

        private void OnDestroy()
        {
            if (requestQueue != null) requestQueue.Failed -= OnQueueFailed;
        }

        private void PurchaseItem()
        {
            SetStatus("Purchasing item...");
            purchaseButton.interactable = false;

            var command = new PurchaseItemCommand(
                this,
                this,
                shopId,
                new List<string> { itemId },
                _ =>
                {
                    purchaseButton.interactable = true;
                    SetStatus("Purchase completed");
                });

            requestQueue.Enqueue(command);
        }

        private void OnQueueFailed(ApiError error)
        {
            purchaseButton.interactable = true;

            switch (error.Code)
            {
                case ApiErrorCode.LackResources:
                    SetStatus("Not enough currency");
                    break;
                case ApiErrorCode.PurchaseLimitExceeded:
                    SetStatus("Purchase limit reached");
                    break;
                case ApiErrorCode.Conflict:
                    // 다른 요청이 먼저 저장됨: 플레이어 데이터를 다시 받은 뒤 사용자에게 재시도 안내
                    SetStatus("Data changed, please refresh and retry");
                    break;
                default:
                    SetStatus($"Purchase failed: {error}");
                    break;
            }
        }

        public void ApplyPurchasedItem(ItemSnapshot item)
        {
            SetStatus($"Purchased: {item.itemId} x{item.amount}");
        }

        public void ApplyGrantedItem(ItemSnapshot item)
        {
            SetStatus($"Granted: {item.itemId} x{item.amount}");
        }

        public void ApplyConsumedItem(ItemSnapshot item)
        {
            SetStatus($"Consumed: {item.itemId} x{item.amount}");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;

            Debug.Log(message);
        }
    }
}
