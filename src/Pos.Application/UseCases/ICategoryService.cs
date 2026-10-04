using Pos.Domain.Entities;

namespace Pos.Application.UseCases;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
}