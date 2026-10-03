using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pos.Application.UseCases;
using Pos.Domain.Entities;
using Pos.Domain.ValueObjects;
using Pos.Infrastructure.Data;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Pos.Infrastructure.Tests.UseCases;

public class RebuildTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ITestOutputHelper _output;

    public RebuildTests(ITestOutputHelper output)
    {
        _output = output;
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Create schema using a temp context
        using var setupCtx = MakeCtx();
        setupCtx.Database.EnsureCreated();
    }

    private PosDbContext MakeCtx() =>
        new PosDbContext(new DbContextOptionsBuilder<PosDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task RebuildBalances_Recalculates_Quantity_And_AvgCost()
    {
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Seed movements using one context
        using (var seedCtx = MakeCtx())
        {
            var m1 = new StockMovement(branchId, productId, StockMovementType.Opening, new Quantity(10m), new Money(10m));
            var m2 = new StockMovement(branchId, productId, StockMovementType.Adjustment, new Quantity(5m), new Money(16m));
            var m3 = new StockMovement(branchId, productId, StockMovementType.Sale, new Quantity(-3m), new Money(12m));

            m1.SetOccurredAt(now.AddSeconds(-2));
            m2.SetOccurredAt(now.AddSeconds(-1));
            m3.SetOccurredAt(now);

            await seedCtx.StockMovements.AddRangeAsync(m1, m2, m3);
            await seedCtx.SaveChangesAsync();
        }

        // Run rebuild using a fresh context (no stale change tracker)
        using (var svcCtx = MakeCtx())
        {
            var stockRepo = new StockRepository(svcCtx);
            var auditRepo = new Repository<AuditLog>(svcCtx);
            var service = new StockService(svcCtx, stockRepo, auditRepo);
            await service.RebuildBalancesAsync(branchId, productId);
        }

        // Verify raw DB
        using var rawCmd = _connection.CreateCommand();
        rawCmd.CommandText = "SELECT Quantity, AvgCost FROM StockBalances";
        using var reader = rawCmd.ExecuteReader();
        while (reader.Read())
            _output.WriteLine($"RAW DB: Qty={reader.GetValue(0)} AvgCost={reader.GetValue(1)}");

        // Verify via fresh context
        using var verifyCtx = MakeCtx();
        var balance = await verifyCtx.StockBalances.FirstOrDefaultAsync(b => b.ProductId == productId);
        balance.ShouldNotBeNull();
        _output.WriteLine($"Balance: Qty={balance!.Quantity.Value} AvgCost={balance.AvgCost.Amount}");

        balance.Quantity.Value.ShouldBe(12m); // 10 + 5 - 3
        // Opening: 10 @ $10 → avg = $10.00   (total value = $100)
        // +5 @ $16          → (10×$10 + 5×$16)/15 = (100 + 80)/15 = $180/15 = $12.00
        // -3 outbound       → avg unchanged = $12.00
        balance.AvgCost.Amount.ShouldBe(12m);
    }
}
