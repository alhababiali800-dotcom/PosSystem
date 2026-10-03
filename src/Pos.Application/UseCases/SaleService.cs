using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Pos.Application.Interfaces;
using Pos.Domain.Entities;
using Pos.Domain.ValueObjects;

namespace Pos.Application.UseCases;

public class SaleService : ISaleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentNumberGenerator _docGenerator;
    private readonly IRepository<Sale> _saleRepo;
    private readonly IStockRepository _stockRepo;
    private readonly IRepository<AuditLog> _auditRepo;
    private readonly IRepository<OutboxMessage> _outboxRepo;
    private readonly IRepository<SaleReturn> _returnRepo;
    private readonly IRepository<SaleLine> _saleLineRepo;

    public SaleService(
        IUnitOfWork unitOfWork,
        IDocumentNumberGenerator docGenerator,
        IRepository<Sale> saleRepo,
        IStockRepository stockRepo,
        IRepository<AuditLog> auditRepo,
        IRepository<OutboxMessage> outboxRepo,
        IRepository<SaleReturn> returnRepo,
        IRepository<SaleLine> saleLineRepo)
    {
        _unitOfWork = unitOfWork;
        _docGenerator = docGenerator;
        _saleRepo = saleRepo;
        _stockRepo = stockRepo;
        _auditRepo = auditRepo;
        _outboxRepo = outboxRepo;
        _returnRepo = returnRepo;
        _saleLineRepo = saleLineRepo;
    }

    public async Task<string> CompleteSaleAsync(Sale sale, Guid branchId, Guid deviceId, Guid userId, CancellationToken cancellationToken = default)
    {
        sale.Complete();
        sale.BranchId = branchId;
        sale.CreatedByUserId = userId;

        var docNumber = await _docGenerator.GenerateDocumentNumberAsync(branchId, deviceId, cancellationToken);
        
        await _saleRepo.AddAsync(sale, cancellationToken);

        foreach (var line in sale.Lines)
        {
            var balance = await _stockRepo.GetBalanceAsync(branchId, line.ProductId, cancellationToken);
            if (balance == null)
            {
                balance = new StockBalance(branchId, line.ProductId);
                await _stockRepo.AddBalanceAsync(balance, cancellationToken);
            }

            line.SetUnitCost(balance.AvgCost);
            var movement = new StockMovement(branchId, line.ProductId, StockMovementType.Sale, new Quantity(-line.Quantity.Value), balance.AvgCost);
            
            balance.Apply(movement);
            
            await _stockRepo.AddMovementAsync(movement, cancellationToken);
            _stockRepo.UpdateBalance(balance);
        }

        var audit = new AuditLog
        {
            UserId = userId,
            DeviceId = deviceId,
            BranchId = branchId,
            Action = "CompleteSale",
            EntityType = "Sale",
            EntityId = sale.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { Id = sale.Id, Doc = docNumber, Total = sale.Total.Amount })
        };
        await _auditRepo.AddAsync(audit, cancellationToken);

        var outbox = new OutboxMessage
        {
            EntityType = "Sale",
            EntityId = sale.Id.ToString(),
            PayloadJson = JsonSerializer.Serialize(new { Id = sale.Id, Doc = docNumber })
        };
        await _outboxRepo.AddAsync(outbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return docNumber;
    }

    public async Task<string> CompleteReturnAsync(SaleReturn saleReturn, Guid branchId, Guid deviceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var docNumber = await _docGenerator.GenerateDocumentNumberAsync(branchId, deviceId, cancellationToken);
        
        await _returnRepo.AddAsync(saleReturn, cancellationToken);
        
        foreach (var line in saleReturn.Lines)
        {
            var saleLine = await _saleLineRepo.GetByIdAsync(line.SaleLineId, cancellationToken);
            if (saleLine == null) throw new InvalidOperationException("SaleLine not found");

            var balance = await _stockRepo.GetBalanceAsync(branchId, saleLine.ProductId, cancellationToken);
            if (balance == null)
            {
                balance = new StockBalance(branchId, saleLine.ProductId);
                await _stockRepo.AddBalanceAsync(balance, cancellationToken);
            }

            var movement = new StockMovement(branchId, saleLine.ProductId, StockMovementType.SaleReturn, line.Quantity, line.ReturnCost);
            balance.Apply(movement);

            await _stockRepo.AddMovementAsync(movement, cancellationToken);
            _stockRepo.UpdateBalance(balance);
        }

        var outbox = new OutboxMessage
        {
            EntityType = "SaleReturn",
            EntityId = saleReturn.Id.ToString(),
            PayloadJson = JsonSerializer.Serialize(new { Id = saleReturn.Id, Doc = docNumber })
        };
        await _outboxRepo.AddAsync(outbox, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return docNumber;
    }
}
