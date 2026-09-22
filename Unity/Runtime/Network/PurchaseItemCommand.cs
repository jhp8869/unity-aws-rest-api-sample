using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Game.Network
{
    public interface IPlayerContext
    {
        string PlayerId { get; }
    }

    public interface IInventorySync
    {
        void ApplyPurchasedItem(ItemSnapshot item);
        void ApplyGrantedItem(ItemSnapshot item);
        void ApplyConsumedItem(ItemSnapshot item);
    }

    public sealed class PurchaseItemCommand : ApiCommand
    {
        private readonly IPlayerContext playerContext;
        private readonly IInventorySync inventorySync;
        private readonly string shopId;
        private readonly List<string> itemIds;
        private readonly Action<PurchaseItemResponse> callback;

        public PurchaseItemCommand(
            IPlayerContext playerContext,
            IInventorySync inventorySync,
            string shopId,
            List<string> itemIds,
            Action<PurchaseItemResponse> callback)
        {
            this.playerContext = playerContext;
            this.inventorySync = inventorySync;
            this.shopId = shopId;
            this.itemIds = itemIds;
            this.callback = callback;
        }

        protected override void ExecuteInternal()
        {
            var request = new PurchaseItemRequest
            {
                PlayerId = playerContext.PlayerId,
                ShopId = shopId,
                PurchaseItemIds = itemIds
            };

            RestApi.PurchaseItem(
                request,
                result =>
                {
                    ApplyInventoryChanges(result);
                    callback?.Invoke(result);
                    Success();
                },
                Error);
        }

        private void ApplyInventoryChanges(PurchaseItemResponse response)
        {
            if (response?.purchaseItems == null)
                return;

            foreach (PurchasedItemBundle bundle in response.purchaseItems)
            {
                if (bundle.purchasedItem != null)
                    inventorySync.ApplyPurchasedItem(bundle.purchasedItem);

                foreach (ItemSnapshot item in bundle.grantedItems ?? new List<ItemSnapshot>())
                    inventorySync.ApplyGrantedItem(item);

                foreach (ItemSnapshot item in bundle.consumedItems ?? new List<ItemSnapshot>())
                    inventorySync.ApplyConsumedItem(item);
            }
        }
    }
}
