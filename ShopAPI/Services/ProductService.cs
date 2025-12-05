using Microsoft.EntityFrameworkCore;
using ShopAPI.Models;

namespace ShopAPI.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Color = request.Color,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            ImageGenerationStatus = GenerationStatus.Pending
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Створено товар: {ProductName} (ID: {ProductId})", product.Name, product.Id);

        return product;
    }

    public async Task<Product> UpdateProductAsync(int id, CreateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Товар з ID {id} не знайдено");

        product.Name = request.Name;
        product.Color = request.Color;
        product.Description = request.Description;

        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return true;
    }
}

