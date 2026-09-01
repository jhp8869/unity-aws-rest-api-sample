# Unity Mobile Game REST API Sample

A portfolio sample based on a commercial Unity mobile RPG project.

This repository demonstrates how a Unity client communicates with an AWS Lambda backend through REST APIs. The sample focuses on a server-authoritative purchase flow: the client sends a purchase request, the backend validates player data and shop rules, then returns item changes for the client to synchronize locally.

## Features

- UnityWebRequest-based REST API client
- Common API response envelope handling
- Original-style `{ retCode, body, error }` response format where `body` is a JSON string
- Cognito JWT authentication header handling
- Retry with exponential backoff
- Sequential API command queue
- AWS Lambda handler structure
- Request validation with Ajv
- DynamoDB and S3 based player data access pattern
- Server-authoritative purchase validation and inventory sync
- Provider login and Cognito session issuing
- Google Play and One Store receipt validation samples
- Unity IAP Google Play purchase flow sample
- One Store SDK purchase flow sample
- Server validation before confirming pending Unity IAP orders

## Structure

```text
Unity/
  Runtime/
    Network/
      ApiError.cs
      ApiResponse.cs
      ApiRequestQueue.cs
      AuthType.cs
      IApiRequest.cs
      ISessionStore.cs
      LoginApi.cs
      PurchaseItemApi.cs
      PurchaseItemCommand.cs
      StorePurchaseService.cs
      StoreReceiptModels.cs
      UnityIapGooglePurchaseService.cs
      OneStorePurchaseService.cs
      ValidatePurchaseApi.cs
      ValidatePurchaseCommand.cs
      SessionStore.cs
      UnityRestClient.cs

Lambda/
  handlers/
    loginWithProvider.mjs
    purchaseItem.mjs
    validateGooglePurchase.mjs
    validateOneStorePurchase.mjs

  shared/
    apiResponse.mjs
    authService.mjs
    iapRewardService.mjs
    inventoryService.mjs
    logger.mjs
    purchaseLimitService.mjs
    repositories.mjs
    storeVerification.mjs
```

## Background

The original project used a REST API backend for a live Unity mobile game. This sample was rewritten for portfolio purposes with all production dependencies, service URLs, table names, bucket names, product IDs, and credentials removed.

## Notes

This is not a full production project. The goal is to show the client-server communication structure and backend validation flow used in a mobile game environment.
