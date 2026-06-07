namespace SampleApi.DTOs;

public class ProductDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public CategoryDto Category { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public InventoryDto Inventory { get; set; } = new();
}

public class InventoryDto
{
    public int StockCount { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
}
