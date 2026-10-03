using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pos.Application.UseCases;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Data;

public class StockRepository : IStockRepository
{
    private readonly PosDbContext _context;

    public StockRepository(PosDbContext context)
    {
        _context = context;
    }

    public async Task<StockBalance?> GetBalanceAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.StockBalances
            .FirstOrDefaultAsync(b => b.BranchId == branchId && b.ProductId == productId, cancellationToken);
    }

    public async Task AddBalanceAsync(StockBalance balance, CancellationToken cancellationToken = default)
    {
        await _context.StockBalances.AddAsync(balance, cancellationToken);
    }

    public void UpdateBalance(StockBalance balance)
    {
        _context.StockBalances.Update(balance);
    }

    public async Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await _context.StockMovements.AddAsync(movement, cancellationToken);
    }

    public async Task<System.Collections.Generic.IReadOnlyList<StockMovement>> GetMovementsAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.StockMovements
            .Where(m => m.BranchId == branchId && m.ProductId == productId)
            .OrderBy(m => m.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }
}
