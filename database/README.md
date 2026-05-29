# Database Setup

This database is designed for a retail online order management system on Microsoft SQL Server.

## Execution Order

Run the scripts in this order:

1. `01_schema.sql`
2. `02_sample-data.sql`
3. `03_procedures.sql`
4. Use `04_reports.sql` for reporting queries

The scripts are numbered so the database can be recreated from an empty SQL Server database without guessing the setup sequence.

Interview explanation notes are available in `INTERVIEW_NOTES.md`.

## Main Concepts

- Product and inventory management
- Customer order management
- Order item snapshot pricing
- Inventory transaction history
- Order status history
- Payment ledger records
- Transactional stored procedures
- Sales and inventory reporting queries

## Demo Flow

1. Run `01_schema.sql` to create tables, constraints, and indexes.
2. Run `02_sample-data.sql` to seed customers, products, inventory, and initial stock history.
3. Run `03_procedures.sql` to create stored procedures for inventory adjustment, order creation, and order cancellation.
4. Execute `CreateOrder` with an `OrderItemRequest` table-valued parameter to create an order through SQL.
5. Run queries from `04_reports.sql` to inspect sales, best-selling products, inventory, and customer order history.
6. Use `INTERVIEW_NOTES.md` to explain the design in an interview.
