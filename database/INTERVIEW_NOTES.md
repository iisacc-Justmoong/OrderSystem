# Database Interview Notes

## 기본 설명

이 데이터베이스는 온라인 주문 및 소매 관리 시스템을 가정하여 설계했다. 핵심 목표는 상품 관리, 주문 관리, 재고 관리, 그리고 매출 분석을 서로 분리하면서도 데이터 정합성을 유지하는 것이다.

먼저 상품은 `Products` 테이블에서 관리한다. 여기에는 SKU, 상품명, 가격, 판매 여부 등의 마스터 데이터를 저장한다.

고객은 `Customers` 테이블에서 관리한다.

고객이 주문을 생성하면 `Orders` 테이블에 주문 헤더가 생성되고, 주문에 포함된 실제 상품 목록은 `OrderItems`에 저장된다.

```text
Order
|-- OrderItem
|-- OrderItem
|-- OrderItem
```

하나의 주문에는 여러 상품이 포함될 수 있기 때문에 Orders와 OrderItems를 1:N 관계로 설계했다. `Orders`는 주문 번호, 고객, 현재 상태, 총액 같은 헤더 정보를 저장하고, `OrderItems`는 상품, 수량, 주문 당시 단가, 줄 합계를 저장한다.

## 가격 스냅샷

주문 당시 가격을 보존하기 위해 `Products.Price`만 직접 참조하지 않고 OrderItems에 UnitPrice를 별도로 저장했다.

예를 들어 오늘 티셔츠 가격이 29,000원이었는데 다음 달에 35,000원으로 변경되더라도 과거 주문 금액은 변경되면 안 되기 때문이다. 따라서 `Products.Price`는 현재 판매 가격이고, `OrderItems.UnitPrice`는 주문 당시 확정 가격이다.

## 재고 설계

재고는 `Inventory` 테이블에서 현재 수량만 관리한다.

하지만 현재 수량만 저장하면 왜 그 수량이 되었는지 추적할 수 없기 때문에 `InventoryTransactions` 테이블을 별도로 두었다.

```text
+100 Initial Stock
-2 Order
-1 Order
+2 Cancel
```

이렇게 하면 현재 재고는 `Inventory`에서 빠르게 조회하고, 재고 변화의 원인은 `InventoryTransactions`에서 추적할 수 있다. 면접에서 왜 `InventoryTransactions`를 만들었는지 질문을 받으면, 재고 수량만 저장하면 원인을 추적할 수 없기 때문이라고 설명할 수 있다.

## 상태 이력

`Orders` 테이블에는 현재 상태만 저장한다.

하지만 주문 상태가 어떻게 변경되었는지는 `OrderStatusHistories`에 별도로 저장한다.

```text
Pending
-> Confirmed
-> Preparing
-> Shipped
-> Delivered
```

이 구조를 통해 주문이 언제 배송 준비로 바뀌었는지, 언제 배송되었는지, 취소가 있었다면 어떤 상태에서 취소되었는지를 추적할 수 있다.

## 트랜잭션 정합성

주문 생성은 트랜잭션으로 처리한다.

주문 생성 시 다음 작업을 하나의 트랜잭션으로 처리한다.

1. `Orders` 생성
2. `OrderItems` 생성
3. `Inventory` 차감
4. `InventoryTransactions` 기록
5. `OrderStatusHistories` 기록

중간에 하나라도 실패하면 전체를 롤백하도록 설계했다. 예를 들어 주문은 생성되었는데 재고 차감이 실패하거나, 재고는 차감되었는데 주문 항목 저장이 실패하면 데이터가 서로 모순된다. 따라서 주문 생성과 취소는 모두 트랜잭션 안에서 처리해야 한다.

## 인덱스

주문 목록은 고객, 상태, 날짜 기준으로 자주 조회되므로 `Orders` 테이블의 CustomerId, Status, CreatedAt 컬럼에 인덱스를 생성했다.

또한 `OrderItems`는 주문 상세 조회 시 자주 사용되므로 `OrderId` 인덱스를 추가했다. 재고 이력은 상품별 추적이 중요하므로 `InventoryTransactions.ProductId`에도 인덱스를 두었다.

## 1분 답변

이 DB는 상품, 고객, 주문, 재고를 중심으로 설계했다. `Orders`와 `OrderItems`를 분리하여 주문 헤더와 상세 항목을 관리했고, 주문 당시 가격을 보존하기 위해 `OrderItems`에 `UnitPrice`를 저장했다. 재고는 현재 수량을 관리하는 `Inventory`와 변경 이력을 관리하는 `InventoryTransactions`로 분리했다. 또한 주문 상태 변경 추적을 위해 `OrderStatusHistories`를 두었다. 주문 생성과 취소는 모두 DB 트랜잭션으로 처리하여 주문 데이터와 재고 데이터의 정합성을 보장하도록 설계했다.
