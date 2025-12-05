using System.IO;

namespace ShopAPI.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly string _storagePath;
    private readonly string _baseUrl;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(IConfiguration configuration, ILogger<ImageStorageService> logger)
    {
        var basePath = Directory.GetCurrentDirectory();
        _storagePath = Path.Combine(basePath, "wwwroot", "images");
        _baseUrl = configuration["ImageStorage:BaseUrl"] ?? "http://localhost:5000/images";
        _logger = logger;

        // Створюємо папку якщо не існує
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Створено папку для зображень: {Path}", _storagePath);
        }
    }

    public async Task<string> SaveImageAsync(byte[] imageData, string filename)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, filename);
            await File.WriteAllBytesAsync(filePath, imageData);
            
            _logger.LogInformation("Зображення збережено: {FilePath}", filePath);
            
            return GetImageUrl(filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при збереженні зображення {Filename}", filename);
            throw;
        }
    }

    public string GetImageUrl(string filename)
    {
        return $"{_baseUrl}/{filename}";
    }
}

