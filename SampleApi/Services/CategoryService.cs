using SampleApi.DTOs;
using SampleApi.Models;

namespace SampleApi.Services;

public class CategoryService : ICategoryService
{
    private readonly List<Category> _categories = new();

    public Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        return Task.FromResult(_categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description }));
    }

    public Task<CategoryDto?> GetByIdAsync(int id)
    {
        var c = _categories.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(c == null ? null : new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description });
    }

    public Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var c = new Category { Id = _categories.Count + 1, Name = dto.Name, Description = dto.Description };
        _categories.Add(c);
        return Task.FromResult(new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description });
    }
}
