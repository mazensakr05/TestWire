using SampleApi.Models;
using SampleApi.DTOs;

public class ProductService : IProductService
{
    private readonly List<Product> _products = new();

    public Task<List<Product>> GetAllAsync() =>
        Task.FromResult(_products);

    public Task<Product?> GetByIdAsync(int id) =>
        Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<Product> CreateAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = _products.Count + 1,
            Name = dto.Name,
            Price = dto.Price
        };
        _products.Add(product);
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return Task.FromResult<Product?>(null);
        product.Name = dto.Name;
        product.Price = dto.Price;
        return Task.FromResult<Product?>(product);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return Task.FromResult(false);
        _products.Remove(product);
        return Task.FromResult(true);
    }
}