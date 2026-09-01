using System;
using UnityEngine;

namespace Portfolio.Game.Network
{
    public sealed class ValidatePurchaseCommand<TRequest> : ApiCommand
        where TRequest : IApiRequest<ValidatePurchaseResponse>
    {
        private readonly MonoBehaviour coroutineRunner;
        private readonly UnityRestClient apiClient;
        private readonly TRequest request;
        private readonly IInventorySync inventorySync;
        private readonly Action<ValidatePurchaseResponse> callback;

        public ValidatePurchaseCommand(
            MonoBehaviour coroutineRunner,
            UnityRestClient apiClient,
            TRequest request,
            IInventorySync inventorySync,
            Action<ValidatePurchaseResponse> callback)
        {
            this.coroutineRunner = coroutineRunner;
            this.apiClient = apiClient;
            this.request = request;
            this.inventorySync = inventorySync;
            this.callback = callback;
        }

        protected override void ExecuteInternal()
        {
            coroutineRunner.StartCoroutine(apiClient.Post<TRequest, ValidatePurchaseResponse>(
                request,
                result =>
                {
                    ApplyInventoryChanges(result);
                    callback?.Invoke(result);
                    Success();
                },
                Error));
        }

        private void ApplyInventoryChanges(ValidatePurchaseResponse result)
        {
            if (result?.purchaseItem != null)
                inventorySync.ApplyPurchasedItem(result.purchaseItem);

            foreach (ItemSnapshot item in result?.Items ?? Array.Empty<ItemSnapshot>())
                inventorySync.ApplyGrantedItem(item);

            foreach (ItemSnapshot item in result?.ConsumeItemResult ?? Array.Empty<ItemSnapshot>())
                inventorySync.ApplyConsumedItem(item);
        }
    }
}
