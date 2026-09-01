using System;
using UnityEngine;

namespace Portfolio.Game.Network
{
    [Serializable]
    public sealed class StorePurchaseResult
    {
        public bool Success;
        public string Message;
        public string ProductId;
        public StoreReceipt Receipt;
        public string CurrencyCode;
        public uint Price;
    }

    [Serializable]
    public class StoreReceipt
    {
        public string Store;
        public ReceiptPayload PayloadData;
    }

    [Serializable]
    public sealed class GoogleReceipt : StoreReceipt
    {
        public string TransactionID;
        public string Payload;

        public static GoogleReceipt FromJson(string json)
        {
            var receipt = JsonUtility.FromJson<GoogleReceipt>(json);
            receipt.Store = "Google";
            receipt.PayloadData = ReceiptPayload.FromJson(receipt.Payload);
            return receipt;
        }
    }

    [Serializable]
    public sealed class ReceiptPayload
    {
        public ReceiptJson JsonData;
        public string signature;
        public string json;

        public static ReceiptPayload FromJson(string json)
        {
            var payload = JsonUtility.FromJson<ReceiptPayload>(json);
            payload.JsonData = JsonUtility.FromJson<ReceiptJson>(payload.json);
            return payload;
        }
    }

    [Serializable]
    public sealed class ReceiptJson
    {
        public string orderId;
        public string packageName;
        public string productId;
        public long purchaseTime;
        public int purchaseState;
        public string purchaseToken;
    }
}
