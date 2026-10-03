using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pos.Application.UseCases;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Data;

public class DocumentNumberGenerator : IDocumentNumberGenerator
{
    private readonly PosDbContext _context;

    public DocumentNumberGenerator(PosDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateDocumentNumberAsync(Guid branchId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
        
        var counter = await _context.SequenceCounters
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.DeviceId == deviceId && c.YearMonth == yearMonth, cancellationToken);
            
        if (counter == null)
        {
            counter = new SequenceCounter
            {
                BranchId = branchId,
                DeviceId = deviceId,
                YearMonth = yearMonth,
                LastSequence = 0
            };
            await _context.SequenceCounters.AddAsync(counter, cancellationToken);
        }

        counter.LastSequence++;
        
        // For simplicity, we just format the IDs directly (we'd map GUIDs to branch/device codes in real life)
        // Format: {BranchCode}-{DeviceCode}-{yyyyMM}-{seq}
        // Assuming we look up branch/device code:
        var branch = await _context.Branches.FindAsync(new object[] { branchId }, cancellationToken);
        var device = await _context.Devices.FindAsync(new object[] { deviceId }, cancellationToken);
        
        var branchCode = branch?.Code ?? "BR";
        var deviceCode = device?.Code ?? "DEV";

        return $"{branchCode}-{deviceCode}-{yearMonth}-{counter.LastSequence:D4}";
    }
}
