using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pos.Application.Interfaces;
using Pos.Application.UseCases;
using Pos.Domain.Entities;
using Pos.Domain.ValueObjects;
using Pos.Infrastructure.Data;
using Shouldly;
using Xunit;
using Microsoft.Data.Sqlite;

namespace Pos.Infrastructure.Tests.UseCases;

public class AtomicSaleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PosDbContext _context;
    
    public AtomicSaleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new PosDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CompleteSale_SimulatedFailure_LeavesDatabaseUnchanged_T3()
    {
        var branchId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed initial stock
        _context.StockBalances.Add(new StockBalance(branchId, productId));
        var initialStock = _context.StockBalances.Local.First();
        initialStock.Apply(new StockMovement(branchId, productId, StockMovementType.Opening, new Quantity(10m), new Money(5m)));
        await _context.SaveChangesAsync();

        var docGen = new DocumentNumberGenerator(_context);
        var saleRepo = new Repository<Sale>(_context);
        var stockRepo = new StockRepository(_context);
        var auditRepo = new Repository<AuditLog>(_context);
        var outboxRepo = new Repository<OutboxMessage>(_context);
        
        var returnRepo = new Repository<SaleReturn>(_context);
        var saleLineRepo = new Repository<SaleLine>(_context);
        
        var service = new SaleService(_context, docGen, saleRepo, stockRepo, auditRepo, outboxRepo, returnRepo, saleLineRepo);

        var sale = new Sale(Guid.NewGuid());
        sale.AddLine(productId, new Quantity(3m), new Money(10m), new TaxRate("NoTax", 0m, false));
        sale.CalculateTotals(Currency.Default);
        sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Cash, new Money(30m)));

        // We simulate a failure during SaveChanges by using a throwing UnitOfWork wrapper
        var failingUow = new FailingUnitOfWork(_context);
        var failingService = new SaleService(failingUow, docGen, saleRepo, stockRepo, auditRepo, outboxRepo, returnRepo, saleLineRepo);

        await Should.ThrowAsync<Exception>(() => failingService.CompleteSaleAsync(sale, branchId, deviceId, userId));

        // Create a fresh context to verify the database state
        var options = new DbContextOptionsBuilder<PosDbContext>().UseSqlite(_connection).Options;
        using var verifyContext = new PosDbContext(options);

        // Stock should still be 10, not 7
        var balance = await verifyContext.StockBalances.FirstOrDefaultAsync(b => b.ProductId == productId);
        balance!.Quantity.Value.ShouldBe(10m);

        // No sale should exist
        var saleExists = await verifyContext.Sales.AnyAsync();
        saleExists.ShouldBeFalse();

        // Document number should not have been consumed
        var seq = await verifyContext.SequenceCounters.FirstOrDefaultAsync();
        // Since the previous transaction failed, the sequence counter in DB should either be null (if it was created in the failed transaction)
        if (seq != null)
        {
            seq.LastSequence.ShouldBe(0); // If it was seeded earlier
        }
        else
        {
            seq.ShouldBeNull();
        }
    }

    [Fact]
    public async Task CompleteSale_Succeeds_WritesOutbox_And_Sequence_And_Audit()
    {
        var branchId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed initial stock
        _context.StockBalances.Add(new StockBalance(branchId, productId));
        var initialStock = _context.StockBalances.Local.First();
        initialStock.Apply(new StockMovement(branchId, productId, StockMovementType.Opening, new Quantity(10m), new Money(5m)));
        await _context.SaveChangesAsync();

        var docGen = new DocumentNumberGenerator(_context);
        var service = new SaleService(_context, docGen, new Repository<Sale>(_context), new StockRepository(_context), new Repository<AuditLog>(_context), new Repository<OutboxMessage>(_context), new Repository<SaleReturn>(_context), new Repository<SaleLine>(_context));

        var sale = new Sale(Guid.NewGuid());
        sale.AddLine(productId, new Quantity(3m), new Money(10m), new TaxRate("NoTax", 0m, false));
        sale.CalculateTotals(Currency.Default);
        sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Cash, new Money(30m)));

        var docNumber = await service.CompleteSaleAsync(sale, branchId, deviceId, userId);

        var options = new DbContextOptionsBuilder<PosDbContext>().UseSqlite(_connection).Options;
        using var verifyContext = new PosDbContext(options);

        // Stock should be 7
        var balance2 = await verifyContext.StockBalances.FirstOrDefaultAsync();
        balance2!.Quantity.Value.ShouldBe(7m);

        // Sale should exist
        var savedSale = await verifyContext.Sales.FirstOrDefaultAsync();
        savedSale.ShouldNotBeNull();
        
        // Outbox should exist
        var outbox = await verifyContext.OutboxMessages.FirstOrDefaultAsync();
        outbox.ShouldNotBeNull();
        outbox!.EntityType.ShouldBe("Sale");

        // Audit log should exist
        var audit = await verifyContext.AuditLogs.FirstOrDefaultAsync();
        audit.ShouldNotBeNull();

        // Sequence should be 1
        var seq2 = await verifyContext.SequenceCounters.FirstOrDefaultAsync();
        seq2!.LastSequence.ShouldBe(1);
    }
    
    [Fact]
    public async Task StaleUpdate_Throws_DbUpdateConcurrencyException()
    {
        var branch = new Branch { Code = "B1", Name = "Branch 1" };
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();
        
        // Simulate a second context reading the same entity
        var options = new DbContextOptionsBuilder<PosDbContext>().UseSqlite(_connection).Options;
        using var context2 = new PosDbContext(options);
        var branch2 = await context2.Branches.FirstAsync();
        
        // Modify in context 1 and save
        branch.Name = "Updated 1";
        await _context.SaveChangesAsync();
        
        // Modify in context 2 and try to save
        branch2.Name = "Updated 2";
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
    }

    private class FailingUnitOfWork : IUnitOfWork
    {
        private readonly PosDbContext _inner;
        public FailingUnitOfWork(PosDbContext inner) => _inner = inner;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new Exception("Simulated DB failure");
        }
    }
}
