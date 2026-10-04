using Pos.Application.Interfaces;
using Pos.Domain.Entities;

namespace Pos.Application.UseCases;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        IUnitOfWork unitOfWork)
    {
        _products = products;
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        => _products.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => _products.GetByCategoryAsync(categoryId, cancellationToken);

    public async Task<Product> AddProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        product.Name = (product.Name ?? string.Empty).Trim();
        if (product.Name.Length == 0)
            throw new ArgumentException("Product name is required.", nameof(product));

        if (product.UnitId == Guid.Empty)
            throw new ArgumentException("Product unit is required.", nameof(product));

        if (!await _categories.ExistsAsync(product.CategoryId, cancellationToken))
            throw new InvalidOperationException("The selected category does not exist.");

        await _products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product;
    }
}