using Newtonsoft.Json;

namespace Portfolio.Game.Network
{
    public sealed class ValidateGooglePurchaseRequest : IApiRequest<ValidatePurchaseResponse>
    {
        [JsonIgnore] public string Endpoint => "ValidateGooglePlayPurchase";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;

        public string PlayerId;
        public string ReceiptJson;
        public string Signature;
        public string CurrencyCode;
        public uint Price;
    }

    public sealed class ValidateOneStorePurchaseRequest : IApiRequest<ValidatePurchaseResponse>
    {
        [JsonIgnore] public string Endpoint => "ValidateOneStorePurchase";
        [JsonIgnore] public AuthType AuthType => AuthType.Login;

        public string PlayerId;
        public string ReceiptJson;
        public string CurrencyCode;
        public uint Price;
    }

    public sealed class ValidatePurchaseResponse
    {
        public ItemSnapshot purchaseItem;
        public ItemSnapshot[] Items;
        public ItemSnapshot[] ConsumeItemResult;
    }
}
