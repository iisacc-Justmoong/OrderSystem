# REST API 계약

REST API는 클라이언트와 Swagger가 주문 시스템을 조작하는 표준 출입구다. 외부 호출자는 DB를 직접 만지지 않고 HTTP 요청을 보내며, 컨트롤러는 DTO를 검증한 뒤 서비스에 업무 처리를 위임한다.

```text
Client / Swagger
REST API Controller
Service
Entity Framework Core
Database
```

## 엔드포인트

```http
GET    /api/products
POST   /api/products
GET    /api/products/{id}
PUT    /api/products/{id}

POST   /api/customers
GET    /api/customers/{id}

GET    /api/inventory
GET    /api/inventory/{productId}
POST   /api/inventory/{productId}/adjust

POST   /api/orders
GET    /api/orders/{id}
GET    /api/orders
PATCH  /api/orders/{id}/status
POST   /api/orders/{id}/cancel
GET    /api/orders/{id}/payments
POST   /api/orders/{id}/payments

GET    /api/reports/daily-sales
```

## 주문 생성

`POST /api/orders`는 고객 확인, 상품 확인, 재고 조건부 차감, 주문 저장, 주문 항목 저장, 재고 이력 저장, 주문 상태 이력 저장을 하나의 트랜잭션으로 처리한다.

요청 예시는 다음과 같다.

```json
{
  "customerId": 1,
  "items": [
    {
      "productId": 10,
      "quantity": 2
    }
  ]
}
```

응답은 `orderId`, `orderNumber`, `status`, `totalAmount`, `items`를 반환한다. enum은 JSON에서 `"Pending"` 같은 문자열로 표시한다.

## 에러 응답

- `400 Bad Request`: 요청 값이 잘못되었거나 재고가 부족하다.
- `404 Not Found`: 상품, 고객, 주문, 재고 같은 리소스가 없다.
- `409 Conflict`: 현재 상태상 처리할 수 없다. 예를 들어 배송된 주문 취소나 허용되지 않은 상태 전이가 여기에 해당한다.
- `500 Server Error`: 처리하지 못한 서버 내부 오류다.

## Swagger

Swagger 문서는 `/swagger/v1/swagger.json`, Swagger UI는 `/swagger`에서 제공한다. 면접 시에는 Swagger UI에서 상품 등록, 고객 등록, 주문 생성, 재고 조회, 주문 취소를 순서대로 호출하면 주문 처리 백엔드의 핵심 흐름을 빠르게 시연할 수 있다.
