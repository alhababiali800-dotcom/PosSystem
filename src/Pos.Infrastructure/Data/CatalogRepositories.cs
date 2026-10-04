using Microsoft.EntityFrameworkCore;
using Pos.Application.Interfaces;
using Pos.Domain.Entities;

namespace Pos.Infrastructure.Data;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(PosDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Categories.AnyAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var lowered = name.ToLower();
        return _context.Categories.AnyAsync(
            c => !c.IsDeleted && c.Name.ToLower() == lowered, cancellationToken);
    }
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(PosDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
}