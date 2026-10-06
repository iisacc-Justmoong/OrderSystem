# Retail Order System

ASP . NET  Core Web  API  project to verify the sleeve order processing workflow. It focuses on order data integrity such as order creation, inventory deduction, status transition, payment ledger, and inventory restoration upon cancellation, rather than the shopping mall screen.

<a id="기술-구성"></a>

## Technical configuration

- ASP.NET Core Web API
- Avalonia desktop shell
- Entity Framework Core
- SQL Server setup scripts
- SQLite
- Swagger
- xUnit Base services and REST API integration tests

Build artifacts and intermediate artifacts are fixed to be created under `build/` at the repository root in `Directory.Build.props`. The target framework of the project is `net8.0`. However, runtime forwarders are set to `Major` so that the app host of `build/bin/Debug/net8.0/` can be executed directly even in environments where only . NET 9 runtime is installed at the local `.dotnet` path.

<a id="핵심-도메인"></a>

## Core domain

- `Product`: Product master with SKU, name, description, current price, and whether it is sold.
- `Customer`: Customer who creates orders.
- `Inventory`: Current inventory quantity per product.
- `InventoryTransaction`: Inventory change history such as receipt, order deduction, and cancellation recovery.
- `Order`: Order unit with customer, status, total amount, and creation/modification time.
- `OrderItem`: Order line that preserves product, quantity, unit price, and sub-total at the time of order.
- `OrderStatusHistory`: History that records when and from what status the order status changed to what status.
- `Payment`: Ledger of payment methods, payment status, and payment amount separate from the order.

`OrderItem.UnitPrice` is a price snapshot at the time of order creation, not the current product price. Even if the product price is changed later, the past order amount does not change.

<a id="주문-규칙"></a>

## Order Rule

Order creation processes after verifying customer existence, product active status, and inventory quantity. Order creation, order item storage, inventory deduction, inventory history storage, and order status history storage are executed within a single DB transaction.

Customer email is stored in lowercase after removing leading and trailing spaces, and duplicate emails are rejected with `409 Conflict`. Product SKU, name, and description are stored after removing leading and trailing spaces, and duplicate SKU is also rejected with `409 Conflict`. This contract is a boundary set to prevent DB unique constraint exceptions from leaking outside API during SQLite development execution and SQL Server evidence scripts.

State transitions allow only the following flows.

```text
Pending -> Confirmed -> Preparing -> Shipped -> Delivered
```

Cancellation is allowed only in `Pending` and `Confirmed` states. If cancellation succeeds, the order status is changed to `Cancelled` and inventory is restored by the quantity of order items. At this time, a positive restoration history is recorded in `InventoryTransactions`, and a cancellation state transition is recorded in `OrderStatusHistories`.

Inventory deduction is processed as an update including the `Quantity >= order quantity` condition. Even if two orders come in, the number of update rows becomes 0 when inventory is insufficient, thus preventing overselling.

<a id="주요-api"></a>

## Key API

- `GET /api/products`: Product list retrieval
- `POST /api/products`: Product and initial inventory registration
- `GET /api/products/{id}`: Single product retrieval
- `PUT /api/products/{id}`: Product information update
- `POST /api/customers`: Customer registration
- `GET /api/customers/{id}`: Single customer retrieval
- `GET /api/inventory`: Inventory list retrieval
- `GET /api/inventory/{productId}`: Inventory retrieval by product
- `POST /api/inventory/{productId}/adjust` : Manual Inventory Adjustment
- `GET /api/orders` : Order List Retrieval, `status`, `from`, `to` Query Support
- `GET /api/orders/{id}` : Single Order Retrieval
- `POST /api/orders` : Order Creation
- `PATCH /api/orders/{id}/status` : Order Status Change
- `POST /api/orders/{id}/cancel` : Order Cancellation
- `GET /api/orders/{id}/payments` : Order Payment List Retrieval
- `POST /api/orders/{id}/payments` : Order Payment Ledger Recording
- `GET /api/reports/daily-sales?from=2026-05-01&to=2026-05-29` : Daily Sales Retrieval

SQL Server-based DB reproduction scripts are organized in [database/README.md](database/README.md). Executing in the order of `01_schema.sql`, `02_sample-data.sql`, `03_procedures.sql`, `04_reports.sql` allows checking tables, demonstration data, stored procedures, and report queries. Interview explanation answers are organized in [database/INTERVIEW_NOTES.md](database/INTERVIEW_NOTES.md). The application uses SQLite for local execution convenience, but SQL outputs are kept as SQL Server-based for interview demonstrations.

REST API detailed contracts are organized in [Docs/rest-api.md](Docs/rest-api.md). After server execution, Swagger UI is checked at `/swagger`.

Avalonia GUI preparation content is organized in [Docs/gui.md](Docs/gui.md). `OrderSystem.Desktop` includes a desktop shell, main window, and ViewModel structure to connect with REST API. The current GUI provides a mini shopping mall flow with a virtual product catalog, shopping cart, checkout, and shopping console logs, rather than a screen to directly inspect the API. The first project of the solution is placed in `OrderSystem.Desktop` so that the default execution entry point candidate becomes the GUI app.

<a id="주문-생성-예시"></a>

## Order creation example

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

The server does not use the price sent by the client but calculates `UnitPrice`, `LineTotal`, and `TotalAmount` using the product price stored in the DB.

<a id="검증-명령"></a>

## Validation command

Use the following command if there is a .NET SDK in the local PATH.

```bash
dotnet restore OrderSystem.sln
dotnet test OrderSystem.sln
dotnet build OrderSystem.sln
```

If only the Rider bundle SDK is available in the current development environment, specify the corresponding `dotnet` executable directly.

```bash
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet restore OrderSystem.sln
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet test OrderSystem.sln
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet build OrderSystem.sln
```

If only the .NET 9 runtime is present in `/Users/ymy/.dotnet` and the existing `net8.0` app host exits with exit code 150, rebuild using the Rider bundle SDK. The rebuilt `runtimeconfig.json` includes `rollForward: Major`, making local execution possible on .NET 9 hosts as well.

## Source layout

Application and library projects live under `src/`; automated test projects live under `tests/`. Build configuration stays at the root, and all build output belongs under `build/`.
