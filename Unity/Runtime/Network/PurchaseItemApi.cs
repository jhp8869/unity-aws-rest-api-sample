using System.Collections.Generic;
using Newtonsoft.Json;

namespace Portfolio.Game.Network
{
    public sealed class PurchaseItemRequest : IApiRequest<PurchaseItemResponse>
    {
        [JsonIgnore] public string Endpoint => "PurchaseItem";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;

        public string PlayerId;
        public string ShopId;
        public List<string> PurchaseItemIds;
    }

    public sealed class PurchaseItemResponse
    {
        public List<PurchasedItemBundle> purchaseItems;
    }

    public sealed class PurchasedItemBundle
    {
        public ItemSnapshot purchasedItem;
        public List<ItemSnapshot> grantedItems;
        public List<ItemSnapshot> consumedItems;
    }

    public sealed class ItemSnapshot
    {
        public string itemInstanceId;
        public string itemId;
        public long amount;
    }
}
