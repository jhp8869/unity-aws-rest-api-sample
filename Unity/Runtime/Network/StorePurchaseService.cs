using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Game.Network
{
    public abstract class StorePurchaseService : MonoBehaviour
    {
        [SerializeField] protected List<string> productIds = new List<string>();

        public event Action Initialized;
        public event Action<StorePurchaseResult> PurchaseCompleted;
        public event Action<string> PurchaseFailed;

        public bool IsInitialized { get; protected set; }

        public abstract string StoreName { get; }
        public abstract void Initialize();
        public abstract void BuyProduct(string productId);

        public virtual string GetPrice(string productId)
        {
            return string.Empty;
        }

        protected bool IsRegisteredProduct(string productId)
        {
            return productIds.Contains(productId);
        }

        protected void RaiseInitialized()
        {
            IsInitialized = true;
            Initialized?.Invoke();
        }

        protected void RaisePurchaseCompleted(StorePurchaseResult result)
        {
            PurchaseCompleted?.Invoke(result);
        }

        protected void RaisePurchaseFailed(string message)
        {
            PurchaseFailed?.Invoke(message);
        }
    }
}
