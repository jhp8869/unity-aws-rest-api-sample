using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Portfolio.Game.Network
{
    public sealed class UnityIapGooglePurchaseService : StorePurchaseService
    {
        private StoreController storeController;
        private readonly Dictionary<string, PendingOrder> pendingOrders = new Dictionary<string, PendingOrder>();

        public bool IsConnected { get; private set; }
        public bool IsProductFetched { get; private set; }
        public override string StoreName => "Google";

        private async void Awake()
        {
            Initialize();
        }

        public override async void Initialize()
        {
            storeController = UnityIAPServices.StoreController();

            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
            storeController.OnStoreDisconnected += OnStoreDisconnected;
            storeController.OnProductsFetched += OnProductsFetched;
            storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            storeController.OnPurchasesFetched += OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

            await storeController.Connect();
            IsConnected = true;

            FetchProducts();
        }

        private void OnDestroy()
        {
            if (storeController == null)
                return;

            storeController.OnPurchasePending -= OnPurchasePending;
            storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            storeController.OnPurchaseFailed -= OnPurchaseFailed;
            storeController.OnStoreDisconnected -= OnStoreDisconnected;
            storeController.OnProductsFetched -= OnProductsFetched;
            storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
            storeController.OnPurchasesFetched -= OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
        }

        public override void BuyProduct(string productId)
        {
            if (!IsRegisteredProduct(productId))
            {
                RaisePurchaseFailed("Product id is not registered.");
                return;
            }

            storeController.PurchaseProduct(productId);
        }

        private void FetchProducts()
        {
            var products = productIds
                .Select(id => new ProductDefinition(id, ProductType.Consumable))
                .ToList();

            storeController.FetchProducts(products);
        }

        private void OnProductsFetched(List<Product> products)
        {
            IsProductFetched = true;
            storeController.FetchPurchases();
            RaiseInitialized();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            IsProductFetched = false;
            RaisePurchaseFailed(failure.FailureReason);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            IsConnected = false;
            RaisePurchaseFailed(description.message);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            // Pending orders can be redelivered on app launch.
            // Unity IAP will also invoke OnPurchasePending for orders that need processing.
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            RaisePurchaseFailed(failure.message);
        }

        private void OnPurchasePending(PendingOrder order)
        {
            NotifyPurchaseReadyForServerValidation(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmed:
                    NotifyPurchaseCompleted(confirmed);
                    break;
                case FailedOrder failed:
                    RaisePurchaseFailed(failed.Details);
                    break;
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            RaisePurchaseFailed(order.Details);
        }

        public void ConfirmAfterServerValidation(string productId)
        {
            if (!pendingOrders.TryGetValue(productId, out PendingOrder order))
            {
                RaisePurchaseFailed("Could not find pending order to confirm.");
                return;
            }

            storeController.ConfirmPurchase(order);
            pendingOrders.Remove(productId);
        }

        private void NotifyPurchaseReadyForServerValidation(PendingOrder order)
        {
            Product product = GetFirstProductInOrder(order);

            if (product == null)
            {
                RaisePurchaseFailed("Could not find confirmed product.");
                return;
            }

            var receipt = GoogleReceipt.FromJson(order.Info.Receipt);
            pendingOrders[product.definition.id] = order;

            RaisePurchaseCompleted(new StorePurchaseResult
            {
                Success = true,
                Message = "ReadyForServerValidation",
                ProductId = product.definition.id,
                Receipt = receipt,
                CurrencyCode = product.metadata.isoCurrencyCode,
                Price = (uint)((double)product.metadata.localizedPrice * 100)
            });
        }

        private void NotifyPurchaseCompleted(ConfirmedOrder order)
        {
            Product product = GetFirstProductInOrder(order);

            if (product != null)
                pendingOrders.Remove(product.definition.id);
        }

        private static Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product;
        }

        public override string GetPrice(string productId)
        {
            Product product = storeController?.GetProductById(productId);
            return product?.metadata.localizedPriceString ?? string.Empty;
        }
    }
}
