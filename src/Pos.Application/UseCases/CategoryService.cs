using Pos.Application.Interfaces;
using Pos.Domain.Entities;

namespace Pos.Application.UseCases;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(ICategoryRepository categories, IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        => _categories.GetAllAsync(cancellationToken);

    public async Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        category.Name = (category.Name ?? string.Empty).Trim();
        if (category.Name.Length == 0)
            throw new ArgumentException("Category name is required.", nameof(category));

        if (await _categories.NameExistsAsync(category.Name, cancellationToken))
            throw new InvalidOperationException($"Category '{category.Name}' already exists.");

        await _categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }
}