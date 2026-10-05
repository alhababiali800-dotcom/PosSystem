using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pos.Application.Interfaces;
using Pos.Domain.Entities;
using Pos.Domain.ValueObjects;

namespace Pos.Infrastructure.Data;

public class PosDbContext : DbContext, IUnitOfWork
{
    // ── DbSets ────────────────────────────────────────────────────────
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleLine> SaleLines => Set<SaleLine>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnLine> SaleReturnLines => Set<SaleReturnLine>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();
    public DbSet<TaxRateRecord> TaxRates => Set<TaxRateRecord>();

    // ── Repository cache (IUnitOfWork) ───────────────────────────────
    private readonly Dictionary<Type, object> _repositories = new();

    public PosDbContext(DbContextOptions<PosDbContext> options) : base(options) { }

    // ── IUnitOfWork.GetRepository<T> ────────────────────────────────
    public IRepository<T> GetRepository<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var cached))
            return (IRepository<T>)cached;

        var repo = new Repository<T>(this);
        _repositories[typeof(T)] = repo;
        return repo;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── SEED DATA ─────────────────────────────────────────────────
        var branchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var unitId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Branch>().HasData(new Branch
        {
            Id = branchId,
            BranchId = branchId,
            Name = "Main Branch",
            CreatedByUserId = adminId,
            CreatedAtUtc = seedDate,
            UpdatedAtUtc = seedDate,
            IsDeleted = false
        });

        modelBuilder.Entity<Unit>().HasData(new Unit
        {
            Id = unitId,
            BranchId = branchId,
            Name = "Piece",
            CreatedByUserId = adminId,
            CreatedAtUtc = seedDate,
            UpdatedAtUtc = seedDate,
            IsDeleted = false
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminId,
            BranchId = branchId,
            Username = "admin",
            PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918",
            CreatedByUserId = adminId,
            CreatedAtUtc = seedDate,
            UpdatedAtUtc = seedDate,
            IsDeleted = false
        });

        // ── Value Converters ──────────────────────────────────────────
        var moneyConverter = new ValueConverter<Money, long>(
            v => (long)Math.Round(v.Amount * 100m, 0, MidpointRounding.AwayFromZero),
            v => new Money(v / 100m)
        );

        var quantityConverter = new ValueConverter<Quantity, long>(
            v => (long)Math.Round(v.Value * 1000m, 0, MidpointRounding.AwayFromZero),
            v => new Quantity(v / 1000m)
        );

        // Apply converters to every mapped Money / Quantity property
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.ClrType.GetProperties())
            {
                if (property.PropertyType == typeof(Money))
                    modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion(moneyConverter);
                else if (property.PropertyType == typeof(Quantity))
                    modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion(quantityConverter);
            }
        }

        // ── Ignore unmapped computed properties ───────────────────────
        modelBuilder.Entity<SaleLine>().Ignore(e => e.TaxRate);

        // ── RowVersion (optimistic concurrency via manual long increment) ──
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.RowVersion))
                    .IsConcurrencyToken();
            }
        }

        // ── Composite / custom keys ───────────────────────────────────
        modelBuilder.Entity<RolePermission>().HasKey(e => new { e.RoleId, e.PermissionKey });
        modelBuilder.Entity<UserRole>().HasKey(e => new { e.UserId, e.RoleId });
        modelBuilder.Entity<UserBranch>().HasKey(e => new { e.UserId, e.BranchId });
        modelBuilder.Entity<SequenceCounter>().HasKey(e => new { e.BranchId, e.DeviceId, e.YearMonth });
        modelBuilder.Entity<StockBalance>().HasKey(e => new { e.BranchId, e.ProductId });
        modelBuilder.Entity<TaxRateRecord>().HasKey(e => e.Name);

        modelBuilder.Entity<SalePayment>().HasKey(e => e.Id);

        // ── Relationships ─────────────────────────────────────────────
        modelBuilder.Entity<Sale>().HasMany(e => e.Lines).WithOne().HasForeignKey("SaleId");
        modelBuilder.Entity<Sale>().HasMany(e => e.Payments).WithOne().HasForeignKey("SaleId");
        modelBuilder.Entity<SaleReturn>().HasMany(e => e.Lines).WithOne().HasForeignKey("SaleReturnId");
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            entry.Entity.RowVersion++;

            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
        }
    }
}

/// <summary>A persisted look-up table row for named tax rates (distinct from the domain TaxRate record).</summary>
public class TaxRateRecord
{
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsInclusive { get; set; }
}