using System;
using UnityEngine;

namespace Portfolio.Game.Network
{
    public sealed class ValidatePurchaseCommand<TRequest> : ApiCommand
        where TRequest : IApiRequest<ValidatePurchaseResponse>
    {
        private readonly TRequest request;
        private readonly IInventorySync inventorySync;
        private readonly Action<ValidatePurchaseResponse> callback;

        public ValidatePurchaseCommand(
            TRequest request,
            IInventorySync inventorySync,
            Action<ValidatePurchaseResponse> callback)
        {
            this.request = request;
            this.inventorySync = inventorySync;
            this.callback = callback;
        }

        protected override void ExecuteInternal()
        {
            RestApi.ValidatePurchase(
                request,
                result =>
                {
                    ApplyInventoryChanges(result);
                    callback?.Invoke(result);
                    Success();
                },
                Error);
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
