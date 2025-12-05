using ShopAPI.Models;

namespace ShopAPI.Services;

public interface IProductService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task<Product> UpdateProductAsync(int id, CreateProductRequest request);
    Task<bool> DeleteProductAsync(int id);
}

