# Progress

## Current Stage: Stage 0 - Scaffolding

### Done
- Created solution and projects.
- Added test projects.
- Set up project references.
- Configured `.gitignore` and `Directory.Build.props`.
- Written `CLAUDE.md`, `THIRD-PARTY.md`, `DECISIONS.md`, `PROGRESS.md`.
- Added smoke tests.
- Stage 1: Added `Money`, `Quantity`, `Currency` value objects.
- Stage 1: Implemented `Sale`, `SaleLine`, `SalePayment` with taxes, discounts, and payments.
- Stage 1: Added `StockMovement`, `StockBalance` with weighted average cost calculation.
- Stage 1: Added `SaleReturn` with return limit checks and cost tracking.
- Stage 1: Added `User`, `Role`, `Permissions` for domain-level authorization logic.
- Stage 2: Added `BaseEntity` with `RowVersion` (concurrency token) and audit fields.
- Stage 2: Added catalog, system, and auth entities (Branch, Device, Product, Customer, Shift, etc.).
- Stage 2: Created `PosDbContext` with Money/Quantity value converters, composite keys, and soft-delete support.
- Stage 2: Added `Repository<T>`, `StockRepository`, `DocumentNumberGenerator`, `IUnitOfWork`.
- Stage 2: Implemented `SaleService.CompleteSaleAsync` (atomic: sale + stock decrease + outbox + audit + doc number).
- Stage 2: Implemented `SaleService.CompleteReturnAsync` (atomic: return + stock increase).
- Stage 2: Implemented `StockService.RebuildBalancesAsync` (replay all movements to recalculate a balance).
- Stage 2: Tests — atomic sale writes outbox + sequence + audit; simulated failure leaves DB unchanged; stale concurrent update throws `DbUpdateConcurrencyException`; rebuild recalculates Qty and AvgCost.

### Next
- Execute Stage 3: Authentication and authorization.

### Blockers
- None.

## Pending Manual Tasks
- T15: Arabic RTL receipt on the real thermal printer (Stage 5).
