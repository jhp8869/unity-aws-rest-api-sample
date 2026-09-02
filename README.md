# Unity 모바일 게임 REST API 샘플

상용 Unity 모바일 RPG 프로젝트에서 사용했던 REST API 구조를 포트폴리오용으로 재구성한 샘플입니다.

이 저장소는 Unity 클라이언트가 AWS Lambda 기반 백엔드와 REST API로 통신하는 흐름을 보여줍니다. 특히 서버 권한 기반 구매 처리, 플레이어 데이터 검증, 결제 영수증 검증, 인벤토리 동기화 구조를 중심으로 구성했습니다.

## 주요 기능

- UnityWebRequest 기반 REST API 클라이언트
- 공통 API 응답 포맷 처리
- `{ retCode, body, error }` 형태의 응답 래퍼 구조
- `body`를 JSON 문자열로 한 번 더 감싸는 기존 프로젝트 스타일 반영
- Cognito JWT 인증 헤더 처리
- 지수 백오프 방식의 재시도 처리
- 순차 실행 API Command Queue
- AWS Lambda 핸들러 구조
- Ajv 기반 요청 데이터 검증
- DynamoDB 기반 계정/플레이어 데이터 접근 패턴
- S3 기반 마스터 데이터 로딩 패턴
- 서버 권한 기반 재화 구매 검증 및 인벤토리 동기화
- 외부 provider 로그인 후 Cognito 세션 발급 흐름
- Google Play 결제 영수증 검증 샘플
- One Store 결제 영수증 검증 샘플
- Unity IAP pending order를 서버 검증 후 confirm하는 흐름
- One Store SDK 구매 흐름 샘플

## 폴더 구조

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

## API 흐름

### 1. 로그인

Unity 클라이언트가 provider authorization code를 서버로 전달합니다. 서버는 provider 사용자 식별자를 내부 플레이어 식별자로 변환하고, Cognito를 통해 로그인 세션을 발급합니다.

관련 파일:

- `Unity/Runtime/Network/LoginApi.cs`
- `Lambda/handlers/loginWithProvider.mjs`
- `Lambda/shared/authService.mjs`

### 2. 일반 재화 구매

클라이언트는 구매할 상점 ID와 상품 ID 목록을 서버로 전달합니다. 서버는 플레이어 계정 상태, 선택 서버, 보유 재화, 상점 데이터, 아이템 데이터를 검증한 뒤 인벤토리를 갱신하고 변경 결과를 반환합니다.

관련 파일:

- `Unity/Runtime/Network/PurchaseItemApi.cs`
- `Unity/Runtime/Network/PurchaseItemCommand.cs`
- `Lambda/handlers/purchaseItem.mjs`
- `Lambda/shared/inventoryService.mjs`

### 3. Google Play 결제 검증

Unity IAP에서 발생한 pending order의 영수증을 서버로 전송합니다. 서버는 Google Play Developer API로 영수증을 검증한 뒤, 검증된 상품에 대해서만 보상을 지급합니다. 클라이언트는 서버 검증이 성공한 후 Unity IAP pending order를 confirm합니다.

관련 파일:

- `Unity/Runtime/Network/UnityIapGooglePurchaseService.cs`
- `Unity/Runtime/Network/ValidatePurchaseApi.cs`
- `Unity/Runtime/Network/ValidatePurchaseCommand.cs`
- `Lambda/handlers/validateGooglePurchase.mjs`
- `Lambda/shared/storeVerification.mjs`

### 4. One Store 결제 검증

One Store SDK에서 반환한 구매 정보를 서버로 전송합니다. 서버는 One Store API를 통해 결제 정보를 검증 및 consume 처리한 뒤, 검증된 상품에 대해서만 보상을 지급합니다.

관련 파일:

- `Unity/Runtime/Network/OneStorePurchaseService.cs`
- `Unity/Runtime/Network/ValidatePurchaseApi.cs`
- `Lambda/handlers/validateOneStorePurchase.mjs`
- `Lambda/shared/storeVerification.mjs`

## 설계 포인트

### 서버 권한 기반 처리

구매 가능 여부와 보상 지급은 클라이언트가 아니라 서버에서 최종 판단합니다. 클라이언트는 요청과 결과 동기화만 담당하고, 실제 재화 차감과 아이템 지급은 Lambda에서 처리합니다.

### 공통 응답 포맷

기존 프로젝트에서 사용하던 `{ retCode, body, error }` 형식의 응답 포맷을 유지했습니다. Unity 클라이언트는 `retCode`를 먼저 확인하고, 성공일 때만 `body`를 실제 응답 모델로 역직렬화합니다.

### 순차 API 큐

Unity 클라이언트에는 API Command Queue를 두어 중요한 API 요청이 순차적으로 처리되도록 구성했습니다. 구매, 결제 검증, 인벤토리 동기화처럼 순서가 중요한 작업을 안정적으로 처리하기 위한 구조입니다.

### 스토어 결제 검증

Google Play와 One Store 결제는 클라이언트 결과만 신뢰하지 않고 서버에서 영수증을 다시 검증합니다. 검증이 끝난 뒤에만 보상을 지급하며, Unity IAP의 pending order는 서버 검증 성공 후 confirm하는 흐름을 사용합니다.

## 보안 및 공개 범위

이 저장소는 포트폴리오 공개를 위해 재작성된 샘플입니다. 실제 운영 프로젝트의 민감 정보는 포함하지 않았습니다.

제거 또는 샘플화한 항목:

- 실제 AWS 계정 정보
- AWS Access Key / Secret Key
- API Gateway 실제 엔드포인트
- Cognito User Pool ID / App Client ID
- DynamoDB 실제 테이블명
- S3 실제 버킷명
- Google Play 서비스 계정 키
- One Store client secret
- 실제 상품 ID 및 라이브 서비스 데이터

서버에서 필요한 값은 환경변수나 Secret Manager를 통해 주입하는 것을 전제로 작성했습니다.

## 안내

이 저장소는 전체 실행 가능한 Unity 프로젝트나 Lambda 배포 패키지가 아니라, 구조와 설계 흐름을 보여주기 위한 코드 샘플입니다.
