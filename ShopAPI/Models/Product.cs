namespace ShopAPI.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Legacy single image URL (kept for backward compatibility)
    public string? ImageUrl { get; set; }

    // New: separate images for different skin tones
    public string? ImageUrlLight { get; set; }
    public string? ImageUrlMedium { get; set; }
    public string? ImageUrlDark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public GenerationStatus ImageGenerationStatus { get; set; } = GenerationStatus.Pending;
}

public enum GenerationStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class GenerateImageRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class GenerateImageResponse
{
    public string JobId { get; set; } = string.Empty;
    public GenerationStatus Status { get; set; }
    public string? ImageUrl { get; set; }
    public string? Error { get; set; }
}

