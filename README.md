# Unity Mobile Game REST API Sample

A portfolio sample based on a commercial Unity mobile RPG project.

This repository demonstrates how a Unity client communicates with an AWS Lambda backend through REST APIs, including authentication, request handling, response parsing, and server-authoritative purchase validation.

## Features

- UnityWebRequest-based REST API client
- Common API response envelope handling
- Cognito JWT authentication header handling
- Request retry and sequential API queue
- AWS Lambda handler structure
- Request validation with Ajv
- DynamoDB / S3 based player data access pattern
- Server-authoritative purchase validation and inventory sync

## Structure

```text
Unity/
  Runtime/
    Network/
      UnityRestClient.cs
      ApiRequest.cs
      ApiResponse.cs
      ApiError.cs
      SessionStore.cs
      ApiRequestQueue.cs
      PurchaseItemApi.cs

Lambda/
  handlers/
    purchaseItem.mjs

  shared/
    apiResponse.mjs
    playerRepository.mjs
    inventoryService.mjs
