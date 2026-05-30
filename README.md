# Retail Order System

소매 주문 처리 업무 흐름을 검증하기 위한 ASP.NET Core Web API 프로젝트다. 쇼핑몰 화면보다 주문 생성, 재고 차감, 상태 전이, 결제 장부, 취소 시 재고 복구 같은 업무 데이터 정합성에 초점을 둔다.

## 기술 구성

- ASP.NET Core Web API
- Avalonia desktop shell
- Entity Framework Core
- SQL Server setup scripts
- SQLite
- Swagger
- xUnit 기반 서비스 및 REST API 통합 테스트

빌드 산출물과 중간 산출물은 저장소 루트의 `build/` 아래에 생성되도록 `Directory.Build.props`에서 고정한다.
프로젝트의 대상 프레임워크는 `net8.0`이다. 다만 로컬 `.dotnet` 경로에 .NET 9 런타임만 설치된 환경에서도 `build/bin/Debug/net8.0/`의 앱 호스트를 직접 실행할 수 있도록 런타임 롤포워드는 `Major`로 둔다.

## 핵심 도메인

- `Product`: SKU, 이름, 설명, 현재 가격, 판매 여부를 가진 상품 마스터다.
- `Customer`: 주문을 생성하는 고객이다.
- `Inventory`: 상품별 현재 재고 수량이다.
- `InventoryTransaction`: 입고, 주문 차감, 취소 복구 같은 재고 변동 이력이다.
- `Order`: 고객, 상태, 총액, 생성/수정 시각을 가진 주문 단위다.
- `OrderItem`: 주문 시점의 상품, 수량, 단가, 줄 합계를 보존하는 주문 행이다.
- `OrderStatusHistory`: 주문 상태가 언제, 어떤 상태에서 어떤 상태로 바뀌었는지 남기는 이력이다.
- `Payment`: 주문과 분리된 결제 방법, 결제 상태, 결제 금액 장부다.

`OrderItem.UnitPrice`는 현재 상품 가격이 아니라 주문 생성 시점의 가격 스냅샷이다. 상품 가격이 이후 변경되어도 과거 주문 금액은 바뀌지 않는다.

## 주문 규칙

주문 생성은 고객 존재 여부, 상품 활성 상태, 재고 수량을 검증한 뒤 처리한다. 주문 생성, 주문 항목 저장, 재고 차감, 재고 이력 저장, 주문 상태 이력 저장은 하나의 DB 트랜잭션 안에서 실행한다.

상태 전이는 다음 흐름만 허용한다.

```text
Pending -> Confirmed -> Preparing -> Shipped -> Delivered
```

취소는 `Pending`, `Confirmed` 상태에서만 허용한다. 취소가 성공하면 주문 상태를 `Cancelled`로 변경하고 주문 항목 수량만큼 재고를 복구한다. 이때 `InventoryTransactions`에는 양수 복구 이력을, `OrderStatusHistories`에는 취소 상태 전이를 남긴다.

재고 차감은 `Quantity >= 주문수량` 조건을 포함한 업데이트로 처리한다. 동시에 두 주문이 들어와도 재고 부족 시 업데이트 행 수가 0이 되므로 과판매를 막을 수 있다.

## 주요 API

- `GET /api/products`: 상품 목록 조회
- `POST /api/products`: 상품 및 초기 재고 등록
- `GET /api/products/{id}`: 상품 단건 조회
- `PUT /api/products/{id}`: 상품 정보 수정
- `POST /api/customers`: 고객 등록
- `GET /api/customers/{id}`: 고객 단건 조회
- `GET /api/inventory`: 재고 목록 조회
- `GET /api/inventory/{productId}`: 상품별 재고 조회
- `POST /api/inventory/{productId}/adjust`: 수동 재고 조정
- `GET /api/orders`: 주문 목록 조회, `status`, `from`, `to` 쿼리 지원
- `GET /api/orders/{id}`: 주문 단건 조회
- `POST /api/orders`: 주문 생성
- `PATCH /api/orders/{id}/status`: 주문 상태 변경
- `POST /api/orders/{id}/cancel`: 주문 취소
- `GET /api/orders/{id}/payments`: 주문 결제 목록 조회
- `POST /api/orders/{id}/payments`: 주문 결제 장부 기록
- `GET /api/reports/daily-sales?from=2026-05-01&to=2026-05-29`: 일별 매출 조회

SQL Server 기준 DB 재현 스크립트는 [database/README.md](database/README.md)에 정리되어 있다. `01_schema.sql`, `02_sample-data.sql`, `03_procedures.sql`, `04_reports.sql` 순서로 실행하면 테이블, 시연 데이터, 저장 프로시저, 리포트 쿼리를 확인할 수 있다. 면접 설명용 답변은 [database/INTERVIEW_NOTES.md](database/INTERVIEW_NOTES.md)에 정리했다. 애플리케이션은 로컬 실행 편의를 위해 SQLite를 사용하지만, SQL 산출물은 면접 시연용 SQL Server 기준으로 둔다.

REST API 세부 계약은 [Docs/rest-api.md](Docs/rest-api.md)에 정리되어 있다. 서버 실행 후 Swagger UI는 `/swagger`에서 확인한다.

Avalonia GUI 준비 내용은 [Docs/gui.md](Docs/gui.md)에 정리되어 있다. `OrderSystem.Desktop`은 REST API와 연결할 데스크톱 셸, 메인 윈도우, ViewModel 구조를 포함한다.
솔루션의 첫 프로젝트는 `OrderSystem.Desktop`으로 배치해 기본 실행 진입점 후보가 GUI 앱이 되도록 했다.

## 주문 생성 예시

```json
{
  "customerId": 1,
  "items": [
    {
      "productId": 10,
      "quantity": 2
    },
    {
      "productId": 15,
      "quantity": 1
    }
  ]
}
```

서버는 클라이언트가 보낸 가격을 사용하지 않고 DB에 저장된 상품 가격으로 `UnitPrice`, `LineTotal`, `TotalAmount`를 계산한다.

## 검증 명령

로컬 PATH에 .NET SDK가 있다면 다음 명령을 사용한다.

```bash
dotnet restore OrderSystem.sln
dotnet test OrderSystem.sln
dotnet build OrderSystem.sln
```

현재 개발 환경처럼 Rider 번들 SDK만 사용 가능한 경우에는 해당 `dotnet` 실행 파일을 직접 지정한다.

```bash
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet restore OrderSystem.sln
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet test OrderSystem.sln
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet build OrderSystem.sln
```

`/Users/ymy/.dotnet`에 .NET 9 런타임만 있는 상태에서 기존 `net8.0` 앱 호스트가 exit code 150으로 종료되면, 위 Rider 번들 SDK로 다시 빌드한다. 재빌드된 `runtimeconfig.json`에는 `rollForward: Major`가 포함되어 .NET 9 호스트에서도 로컬 실행이 가능하다.
