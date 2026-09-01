using System;
using System.Collections.Generic;
using UnityEngine;

#if ONESTORE_PURCHASING
using OneStore.Auth;
using OneStore.Common;
using OneStore.Purchasing;
#endif

namespace Portfolio.Game.Network
{
#if ONESTORE_PURCHASING
    public sealed class OneStorePurchaseService : StorePurchaseService, IPurchaseCallback
    {
        [SerializeField] private string licenseKey;
        [SerializeField] private string clientId;

        private PurchaseClientImpl client;
        private List<ProductDetail> productDetails = new List<ProductDetail>();

        public override string StoreName => "OneStore";
        public string UpdateUrl => $"https://onesto.re/{clientId}";

        public override void Initialize()
        {
            client = new PurchaseClientImpl(licenseKey);
            client.Initialize(this);
            client.QueryProductDetails(productIds.AsReadOnly(), ProductType.INAPP);
        }

        public override void BuyProduct(string productId)
        {
            if (!IsRegisteredProduct(productId))
            {
                RaisePurchaseFailed("Product id is not registered.");
                return;
            }

            var flowParams = new PurchaseFlowParams.Builder()
                .SetProductId(productId)
                .SetProductType(ProductType.INAPP)
                .SetQuantity(1)
                .Build();

            client.Purchase(flowParams);
        }

        public void OnNeedLogin()
        {
            new OneStoreAuthClientImpl().LaunchSignInFlow(result =>
            {
                if (result.IsSuccessful())
                    Initialize();
                else
                    RaisePurchaseFailed("One Store login failed.");
            });
        }

        public void OnNeedUpdate()
        {
            client.LaunchUpdateOrInstallFlow(_ => { });
        }

        public void OnProductDetailsSucceeded(List<ProductDetail> details)
        {
            productDetails = details;
            RaiseInitialized();
        }

        public void OnProductDetailsFailed(IapResult result)
        {
            RaisePurchaseFailed(result.Message);
        }

        public void OnPurchaseSucceeded(List<PurchaseData> purchases)
        {
            foreach (PurchaseData purchase in purchases)
                NotifyPurchaseCompleted(purchase);
        }

        public void OnPurchaseFailed(IapResult result)
        {
            RaisePurchaseFailed(result.Message);
        }

        public void OnConsumeSucceeded(PurchaseData purchase) { }
        public void OnConsumeFailed(IapResult result) => RaisePurchaseFailed(result.Message);
        public void OnAcknowledgeSucceeded(PurchaseData purchase, ProductType type) { }
        public void OnAcknowledgeFailed(IapResult result) => RaisePurchaseFailed(result.Message);
        public void OnManageRecurringProduct(IapResult result, PurchaseData purchase, RecurringAction action) { }
        public void OnSetupFailed(IapResult result) => RaisePurchaseFailed(result.Message);

        private void NotifyPurchaseCompleted(PurchaseData purchase)
        {
            ProductDetail detail = productDetails.Find(item => item.productId == purchase.ProductId);

            if (detail == null)
            {
                RaisePurchaseFailed("Could not find One Store product detail.");
                return;
            }

            var payload = ReceiptPayload.FromJson(purchase.JsonReceipt);

            RaisePurchaseCompleted(new StorePurchaseResult
            {
                Success = true,
                Message = "Successful",
                ProductId = purchase.ProductId,
                Receipt = new StoreReceipt { Store = "OneStore", PayloadData = payload },
                CurrencyCode = detail.priceCurrencyCode,
                Price = uint.Parse(detail.price)
            });
        }

        public override string GetPrice(string productId)
        {
            return productDetails.Find(item => item.productId == productId)?.price ?? string.Empty;
        }
    }
#else
    public sealed class OneStorePurchaseService : StorePurchaseService
    {
        public override string StoreName => "OneStore";

        public override void Initialize()
        {
            RaisePurchaseFailed("One Store SDK is not installed.");
        }

        public override void BuyProduct(string productId)
        {
            RaisePurchaseFailed("One Store SDK is not installed.");
        }
    }
#endif
}
