# Role & Working Rules
- Small steps, one stage at a time.
- Commit after every task.
- Read CLAUDE.md, docs/PROGRESS.md, docs/DECISIONS.md at start. Update PROGRESS.md at end.
- Use only MIT / Apache-2.0 / BSD / built-in .NET libraries.
- Clean-room design. No business logic in code-behind or XAML.

# Structure
Dependencies point inward: Desktop → Application → Domain; Infrastructure → Application → Domain. Domain references nothing.

# Core Rules
- `Money`: value object (decimal), stored as integer minor units.
- `Quantity`: decimal (3 places), stored as integer thousandths.
- IDs: client-side `Guid`, with BranchId, CreatedAtUtc, UpdatedAtUtc, CreatedByUserId, IsDeleted, RowVersion (long).
- Stock: append-only `StockMovement`.
- Atomic sale: Sale, lines, payments, movements, balance updates, outbox in one transaction.
- Permissions: string keys checked in Application layer.
- SQLite WAL mode.
