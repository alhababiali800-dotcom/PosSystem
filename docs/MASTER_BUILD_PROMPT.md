# MASTER BUILD PROMPT: Multi-Branch Offline-First POS (C# / WPF)

> Paste "SECTION 9: START MESSAGE" into Claude Code as the first message, with this file saved at `docs/MASTER_BUILD_PROMPT.md` in an empty Git repo.
> Work is split into stages. Each stage is self-contained. Finish and commit one stage before starting the next.

---

## 1. YOUR ROLE AND WORKING RULES

You are a senior .NET engineer building the foundation of a retail POS for a client with several branches and a central warehouse. Follow these rules in every session:

1. **Small steps.** One stage at a time. Within a stage, one task at a time: write the test first, then the code, then run `dotnet test`. Never leave the build red at the end of a task.
2. **Commit after every task** with a clear message (`stage1: add Money value object + tests`).
3. **Keep memory outside the chat.** At the start of every session read `CLAUDE.md`, `docs/PROGRESS.md`, `docs/DECISIONS.md`. At the end of every task update `docs/PROGRESS.md` (done / next / blockers). Context may be cut at any time; the files must always let a fresh session continue.
4. **Never invent requirements.** If something is ambiguous, pick the default listed in Section 3, record it in `docs/DECISIONS.md`, and continue. Ask the human only when blocked.
5. **Licensing.** Use only MIT / Apache-2.0 / BSD / built-in .NET libraries. No commercial UI suites (no Telerik, DevExpress, Syncfusion), no AGPL/GPL/SSPL. Avoid FluentAssertions (commercial since v8): use xUnit asserts or Shouldly. Avoid QuestPDF. Record every NuGet package and its license in `THIRD-PARTY.md`.
6. **Clean-room.** Do not copy code, schemas or field names from FreePOS, VOODO ERP or Posnic. Design from the specification here.
7. **Keep files small** (under ~300 lines). No business logic in code-behind or in XAML.
8. **Verify the toolchain first:** run `dotnet --list-sdks`. Use the newest LTS SDK that supports WPF. If WPF cannot be built in the current environment (not Windows), build and test everything except `Pos.Desktop`, and say so in PROGRESS.md.

## 2. PRODUCT SUMMARY

- Windows desktop POS per branch, **fully usable with no internet**.
- A central server (later stage) holds the main warehouse, shared catalog/prices/users, reports, and receives branch data via sync.
- Roles: Owner, Admin, Branch Manager, Cashier, Warehouse Keeper. Roles/permissions are data, not hard-coded.
- UI: Arabic first (RTL), English supported. All strings in resource files.

## 3. DEFAULTS (until the client answers; all configurable, never hard-coded)

| Topic | Default |
|---|---|
| Currency | Configurable code + symbol + minor-unit digits (default 2) |
| Tax | Configurable named rates; price-inclusive flag per setting; default off |
| Languages | ar (RTL) and en |
| Stock costing | Weighted average cost |
| Negative stock | Disallowed by default; setting per branch |
| Price lists | Retail and Wholesale; selected by customer type |
| Payments | Cash, Card (manual reference), Bank transfer, QR (manual reference), Credit (customer debt); mixed payments allowed |
| Discounts | Line and invoice level; max % per role; above max needs manager PIN |
| Invoice edit | Completed sales are immutable; corrections via return |
| Time | Stored UTC; branch time zone in settings |

## 4. SOLUTION STRUCTURE

```
Pos.sln
src/
  Pos.Domain            entities, value objects, domain rules (no external deps)
  Pos.Application       use cases, service interfaces, permissions, DTOs
  Pos.Infrastructure    EF Core + SQLite, migrations, repositories, unit of work, outbox, backup, hashing
  Pos.Hardware          receipt rendering, ESC/POS encoder, raw printer, cash drawer, barcode input helper
  Pos.Sync.Contracts    DTOs shared with the future server
  Pos.Desktop           WPF (MVVM), views, view models, localization
  Pos.Server            ASP.NET Core + PostgreSQL (LATER STAGE, do not create yet)
tests/
  Pos.Domain.Tests  Pos.Application.Tests  Pos.Infrastructure.Tests  Pos.Hardware.Tests
CLAUDE.md  THIRD-PARTY.md        (repo ROOT. Claude Code auto-loads a root CLAUDE.md)
docs/  PROGRESS.md  DECISIONS.md  MASTER_BUILD_PROMPT.md
```

Dependencies point inward: Desktop → Application → Domain; Infrastructure → Application → Domain. Domain references nothing.

Allowed packages (verify license when adding): `Microsoft.EntityFrameworkCore.Sqlite`, `CommunityToolkit.Mvvm`, `MaterialDesignThemes`, `Serilog` (+ file sink), `Microsoft.Extensions.*`, `xunit`, `Shouldly`, `NSubstitute` (the only mocking library; do not use Moq), `coverlet.collector` (coverage: `dotnet test --collect:"XPlat Code Coverage"`).

## 5. CORE DESIGN RULES

1. **Money:** `Money` value object (decimal in code). In SQLite store as INTEGER minor units via value converter. Quantities: `decimal` with 3 places, stored as INTEGER thousandths. Never `float`/`double` for money or quantity. Rounding rule documented and tested (default: away from zero at line level, totals are sums of rounded lines).
2. **IDs:** every entity has `Guid Id` generated client-side, plus `BranchId`, `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedByUserId`, `IsDeleted` (soft delete), `RowVersion` (a plain `long`, NOT a database-generated token: EF Core does not auto-generate concurrency tokens on SQLite. Override `SaveChanges` to increment it on every Added/Modified entity, configure it with `IsConcurrencyToken()`, and test that a stale update throws `DbUpdateConcurrencyException`). Human-readable document numbers: `{BranchCode}-{DeviceCode}-{yyyyMM}-{seq}`, sequence per device, gap-free per device.
3. **Stock is a ledger.** Table `StockMovement` (append-only): Id, BranchId, ProductId, Type, QuantityDelta (signed), UnitCost, ReferenceType, ReferenceId, OccurredAtUtc, UserId, Note. Types: `Sale, SaleReturn, Purchase, PurchaseReturn, TransferOut, TransferIn, Adjustment, StocktakeSettlement, Opening`. A `StockBalance` table (BranchId, ProductId, Quantity, AvgCost) is updated **in the same transaction** as the movement, and a `RebuildBalances` function must reproduce it exactly from the ledger (tested). **Costing:** each `SaleLine` stores `UnitCostAtSale`, a snapshot of the average cost at the moment of sale. A `SaleReturn` movement uses that snapshot cost (not the current average), and the balance's average cost is then recomputed by the normal weighted-average formula with the returned quantity at that cost.
4. **Atomic sale.** `CompleteSale` writes Sale, SaleLines, SalePayments, StockMovements, StockBalance updates, AuditLog and an Outbox row in **one DB transaction**. Any failure rolls back everything. Payments must equal total due (integer minor units), otherwise reject. **The `SequenceCounter` increment that allocates the document number is part of this same transaction**, so a rolled-back sale never consumes a number (the sequence stays gap-free).
5. **Immutability.** Completed sales, movements and audit entries are never updated or deleted. Corrections are new rows (returns, reversal movements).
6. **Permissions** are string keys (e.g. `sales.create`, `sales.discount.override`, `price.edit`, `stock.adjust`, `transfer.approve`, `users.manage`, `reports.view`, `shift.close`). Checked in the Application layer for every use case; UI only mirrors them. Branch scope: a user can act only on branches assigned to them.
7. **Audit log:** UserId, DeviceId, BranchId, Action, EntityType, EntityId, BeforeJson, AfterJson, TimestampUtc. Written for every sensitive action (login failures, price/discount override, return, adjustment, user/role change, shift close, backup/restore).
8. **Outbox for sync:** every change that must reach the server writes an `OutboxMessage` (Id, EntityType, EntityId, PayloadJson, CreatedAtUtc, SentAtUtc nullable) in the same transaction. Sync itself is a later stage; only the outbox is built now.
9. **Security:** PBKDF2-HMACSHA256 (≥ 310,000 iterations, per-user salt) via built-in .NET APIs; lockout after 5 failures for 5 minutes; manager PIN stored hashed the same way; no secrets in source or config files (the backup encryption key is derived from a user passphrase, see Stage 6, and is never stored); parameterized access only (EF Core); structured logs without sensitive data.
10. **SQLite:** WAL mode, `PRAGMA foreign_keys=ON`, busy timeout, one DB file per branch device in `%LOCALAPPDATA%\Pos\`.

## 6. LOCAL DATABASE ENTITIES (create with EF Core migrations)

Settings, Branch, Device, User, Role, RolePermission, UserRole, UserBranch, Category, Unit, Product, ProductBarcode, PriceList, ProductPrice, TaxRate, Customer (with CustomerType, CreditLimit), Supplier, Shift (OpenedAt, ClosedAt, OpeningCash, CountedCash, ExpectedCash, Difference), Sale, SaleLine, SalePayment, SaleReturn, SaleReturnLine, StockMovement, StockBalance, Expense, AuditLog, OutboxMessage, SequenceCounter.
Not yet: PurchaseOrder, GoodsReceipt, StockTransfer, Stocktake (added in later stages, but keep `StockMovement` types ready for them).

## 6B. ACCEPTANCE TEST CATALOG

Stages reference these IDs. Implement them as automated tests where marked "auto"; "manual" ones are recorded in `docs/PROGRESS.md` as pending human tasks. IDs marked "later" belong to stages not in this prompt; do not implement them now.

| ID | Scenario | Expected result | Stage | Type |
|---|---|---|---|---|
| T1 | Cashier of branch 1 requests stock or sales of branch 2 | Rejected by the server | later | auto |
| T2 | Cashier tries price edit, user management, stock adjustment, transfer approval | Denied, and the attempt is written to the audit log | 3 | auto |
| T3 | Sell 3 of 10 with a failure injected at every step of saving | Either stock 7 + complete sale, or stock 10 + no sale. Never in between. Document number not consumed on rollback | 2 | auto |
| T4 | 10 sales offline, restart, reconnect | All sales and movements arrive once, none lost, none duplicated | later | auto |
| T5 | Two branches sell 5 and 4 from a stock of 7 | Server detects negative stock and raises an alert; no sale is lost | later | auto |
| T6 | Sales − cost of goods − expenses | Net profit equals a hand-computed sample | later | auto |
| T7 | Transfer: send 10, receive 8 | The difference of 2 appears as a pending variance; both stocks correct | later | auto |
| T8 | Return after 3 days of a discounted sale | Refund equals the original net price; stock increases at the original line cost | 1, 2, 5 | auto |
| T9 | Mixed payment (cash + card) not equal to the total | Save rejected | 1 | auto |
| T10 | Edit a completed sale | Impossible; the return flow is offered instead | 1 | auto |
| T11 | Close a shift with a cash difference | Difference recorded with cashier, manager and audit entry | 5 | auto |
| T12 | Restore a backup on a clean folder | Application works with identical data | 6 | auto |
| T13 | Device clock moved backwards | Sale blocked or a warning logged | later | auto |
| T14 | 100k sales and 50k products | Search and sale respond in under 1 second | later | auto |
| T15 | Arabic RTL receipt on the real thermal printer | Readable text, correct alignment, paper cut | 5 | manual |
| T16 | Wrong password 5 times | Temporary lockout and audit entry | 3 | auto |
| T17 | Input `' OR 1=1 --` into every text field | No effect and no error | 3 | auto |
| T18 | An employee is disabled | Cannot log in, even on an offline device, after the next sync | later | auto |

## 7. STAGES

### Stage 0: Scaffolding
- Create the solution/projects per Section 4, project references, `.editorconfig`, `.gitignore`, nullable enabled, warnings as errors.
- Write `CLAUDE.md` (a condensed copy of Sections 1, 4, 5), `THIRD-PARTY.md`, `docs/PROGRESS.md`, `docs/DECISIONS.md`.
- One passing smoke test per test project.
- **Done when:** `dotnet build` and `dotnet test` are green; commit.

### Stage 1: Domain core (pure logic, no DB)
- `Money`, `Quantity`, `Currency` settings, rounding rules.
- `Sale` aggregate: add line, line/invoice discount, tax calculation (inclusive/exclusive), payments, totals, state (Draft/Completed), invariants.
- Stock rules: weighted average cost update; negative stock policy.
- `Return` rules: cannot exceed sold quantity minus already returned; refund value follows original line net price.
- Permission catalog and `Role` evaluation including branch scope and discount limit.
- **Tests:** rounding; discount; tax; mixed payment mismatch rejected (T9); completed sale cannot change (T10); return limits and value (T8); weighted average cost; a return uses the original line's `UnitCostAtSale`, not the current average.
- **Done when:** all green; ≥ 90% line coverage of `Pos.Domain` measured with `coverlet.collector`; commit.

### Stage 2: Persistence and atomic sale
- EF Core SQLite context, entity configuration, value converters, first migration, seed (default roles, permissions, settings).
- `IUnitOfWork`, repositories, `SequenceCounter` for document numbers.
- `CompleteSale`, `CompleteReturn`, `AdjustStock`, `RebuildBalances` use cases with the single-transaction guarantee.
- AuditLog and Outbox writing inside the same transaction.
- **Tests (real SQLite file/in-memory):** T3 using a simulated failure injected at each step (nothing partial persists, and the document-number sequence is NOT consumed by the rolled-back sale); stale update throws `DbUpdateConcurrencyException` (RowVersion); ledger vs balance rebuild equality; duplicate document number impossible; outbox row exists for each sale.
- **Done when:** green; commit.

### Stage 3: Authentication and authorization
- Password hashing and verification, lockout, manager PIN approval flow, session object (user, branch, device, permissions).
- Use-case guards using permission keys and branch scope.
- **Tests:** T2 (cashier forbidden actions), T16 (lockout), T17 (injection strings stored/looked up safely), audit entry on every failure/override, disabled user cannot log in.
- **Done when:** green; commit.

### Stage 4: Desktop shell and POS screen
- WPF app with DI host, navigation, localization (ar/en, RTL/LTR switching), theme.
- Screens: Login, Shift open, POS (product search + barcode field that keeps focus, cart grid, qty edit, line/invoice discount with PIN dialog, hold/resume, payment dialog with mixed payments, receipt preview).
- ViewModels use only Application services; ViewModels unit-tested where logic exists.
- **Done when:** app starts, a full sale completes end-to-end against SQLite, and the sale is visible in the DB with its movements. Keyboard-only flow works (F-keys documented).

### Stage 5: Printing, returns, shift close
- Receipt model → rendered as a **bitmap** and sent as ESC/POS raster (this avoids Arabic shaping/codepage problems on thermal printers). Configurable width (58/80 mm), logo, footer. Raw printing to a Windows printer queue; cash-drawer kick command; fake printer implementation for tests.
- Return screen (find original sale, partial return, refund method).
- Shift close: expected vs counted cash, difference recorded with audit (T11).
- **Tests:** receipt layout snapshot for an Arabic receipt (T15 manual check on a real printer is listed in PROGRESS.md as a pending human task); T8, T11.
- **Done when:** tests green; a sale prints through the fake printer producing a deterministic ESC/POS byte stream; return and shift-close flows work end-to-end; commit.

### Stage 6: Backup and restore
- Scheduled and manual backup of the SQLite file (WAL-safe, using SQLite online backup), restore into a fresh folder, integrity check (`PRAGMA integrity_check`) after restore.
- **Encryption and key management:** the key comes from a **user passphrase**, never stored anywhere. Derive a 256-bit key with PBKDF2-HMACSHA256 (≥ 310,000 iterations, random 16-byte salt), encrypt with AES-256-GCM (`System.Security.Cryptography.AesGcm`, random 12-byte nonce). File header: magic bytes, format version, iterations, salt, nonce; the GCM tag is appended. The scheduled backup asks for the passphrase once per session and keeps it only in memory. The UI must warn that a lost passphrase means an unrecoverable backup.
- **Tests:** T12 (backup, delete DB, restore, data identical); wrong passphrase gives a clear error and writes nothing; a tampered file is rejected by the GCM tag; header version is checked.
- **Done when:** tests green; restore on an empty folder yields a working database; commit.

> Stages 7+ (catalog management screens, reports, purchasing, customers/debts, expenses, stocktake, then **Pos.Server + sync + warehouse transfers**) will be given as separate prompts after Stage 6 is accepted. Do not start them.

## 8. REPORTING BACK

At the end of each stage output: what was built, test count and result, decisions taken, open questions for the human, and the exact next stage. Also update `docs/PROGRESS.md`.

---

## 9. START MESSAGE (paste this into Claude Code)

```
Read docs/MASTER_BUILD_PROMPT.md fully. Then:
1. Run `dotnet --list-sdks` and report the SDK you will use.
2. Execute Stage 0 only. Commit when green.
3. Create CLAUDE.md, docs/PROGRESS.md, docs/DECISIONS.md, THIRD-PARTY.md as described.
4. Stop and give me the Stage 0 report. Do not start Stage 1 until I say "continue".
```

To continue each time: `Continue. Read CLAUDE.md and docs/PROGRESS.md, then execute the next stage.`
