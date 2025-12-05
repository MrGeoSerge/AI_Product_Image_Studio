using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopAPI.Models;
using ShopAPI.Services;

namespace ShopAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageGenerationController : ControllerBase
{
    private readonly IComfyUIService _comfyUIService;
    private readonly IProductService _productService;
    private readonly IImageStorageService _imageStorage;
        private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImageGenerationController> _logger;

    public ImageGenerationController(
        IComfyUIService comfyUIService,
        IProductService productService,
        IImageStorageService imageStorage,
        IServiceScopeFactory scopeFactory,
        ILogger<ImageGenerationController> logger)
    {
        _comfyUIService = comfyUIService;
        _productService = productService;
        _imageStorage = imageStorage;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateImageResponse>> GenerateImage(GenerateImageRequest request)
    {
        try
        {
            // Формуємо промпт для генерації (generic, без тону шкіри)
            var positivePrompt = BuildProductPrompt(request);
            var negativePrompt = "bad quality, blurry, distorted, low resolution, watermark, text";

            // Відправляємо запит до ComfyUI
            var promptId = await _comfyUIService.QueuePromptAsync(positivePrompt, negativePrompt);

            _logger.LogInformation("Запущено генерацію зображення. Prompt ID: {PromptId}", promptId);

            // Запускаємо фонову задачу для перевірки статусу
            _ = Task.Run(async () => await ProcessImageGenerationAsync(promptId, request));

            return Ok(new GenerateImageResponse
            {
                JobId = promptId,
                Status = GenerationStatus.Processing
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при генерації зображення");
            return StatusCode(500, new GenerateImageResponse
            {
                Status = GenerationStatus.Failed,
                Error = ex.Message
            });
        }
    }

    [HttpPost("generate-for-product/{productId}")]
    public async Task<ActionResult<GenerateImageResponse>> GenerateImageForProduct(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null)
            return NotFound();

        // Оновлюємо статус товару на Processing
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var dbProduct = await context.Products.FindAsync(productId);
            if (dbProduct != null)
            {
                dbProduct.ImageGenerationStatus = GenerationStatus.Processing;
                await context.SaveChangesAsync();
            }
        }

        // Підготуємо базовий запит
        var request = new GenerateImageRequest
        {
            Name = product.Name,
            Color = product.Color,
            Description = product.Description
        };

        // Опис різних тонів шкіри для генерації
        var skinTones = new Dictionary<string, string>
        {
            { "light", "light skin tone" },
            { "medium", "medium skin tone" },
            { "dark", "dark skin tone" }
        };

        var negativePrompt = "bad quality, blurry, distorted, low resolution, watermark, text";

        foreach (var kvp in skinTones)
        {
            var toneKey = kvp.Key;
            var toneDescription = kvp.Value;

            // Формуємо промпт з урахуванням тону шкіри та людини
            var positivePrompt = BuildProductPrompt(request, toneDescription);
            var promptId = await _comfyUIService.QueuePromptAsync(positivePrompt, negativePrompt);

            _logger.LogInformation(
                "Запущено генерацію зображення для товару {ProductId}, тон шкіри {Tone}. Prompt ID: {PromptId}",
                productId, toneKey, promptId);

            // Для кожного тону шкіри запускаємо окрему фонову задачу
            _ = Task.Run(async () => await ProcessImageGenerationAsync(promptId, request, productId, toneKey));
        }

        return Ok(new GenerateImageResponse
        {
            JobId = Guid.NewGuid().ToString(), // умовний jobId для всієї пачки
            Status = GenerationStatus.Processing
        });
    }

    [HttpGet("status/{jobId}")]
    public async Task<ActionResult<GenerateImageResponse>> GetGenerationStatus(string jobId)
    {
        var status = await _comfyUIService.CheckGenerationStatusAsync(jobId);
        return Ok(status);
    }

    private string BuildProductPrompt(GenerateImageRequest request, string? skinToneDescription = null)
    {
        // Формуємо детальний промпт для генерації зображення товару на людині
        var skinTonePart = string.IsNullOrWhiteSpace(skinToneDescription)
            ? "natural skin tone"
            : skinToneDescription;

        var prompt =
            $"fashion product photo of a person wearing {request.Name}, clothing color: {request.Color}, " +
            $"{request.Description}, full body shot, {skinTonePart}, " +
            "studio photography, soft professional lighting, clean neutral background, high quality, " +
            "sharp focus, realistic skin, detailed fabric, commercial e-commerce photo, 8k resolution";

        return prompt;
    }

    private async Task ProcessImageGenerationAsync(
        string promptId,
        GenerateImageRequest request,
        int? productId = null,
        string? skinToneKey = null)
    {
        try
        {
            _logger.LogInformation("Почато обробку генерації зображення для Prompt ID: {PromptId}, Product ID: {ProductId}", promptId, productId);

            // Чекаємо завершення генерації (перевіряємо кожні 3 секунди)
            // На CPU генерація одного зображення може займати 3–4 хвилини, тому
            // збільшуємо таймаут, щоб встигли всі три тони шкіри.
            var maxAttempts = 300; // ~15 хвилин максимум (для CPU режиму)
            var attempt = 0;
            bool imageProcessed = false; // Прапорець, щоб не обробляти зображення повторно

            while (attempt < maxAttempts && !imageProcessed)
            {
                await Task.Delay(3000); // Чекаємо 3 секунди
                attempt++;

                try
                {
                    // Перевіряємо історію ComfyUI
                    var status = await _comfyUIService.CheckGenerationStatusAsync(promptId);
                    
                    if (status != null && status.Status == GenerationStatus.Completed && !string.IsNullOrEmpty(status.ImageUrl))
                    {
                        if (imageProcessed)
                        {
                            // Вже обробили, виходимо
                            return;
                        }

                        _logger.LogInformation("Генерація завершена для Prompt ID: {PromptId}", promptId);
                        
                        // Парсимо URL для отримання filename та subfolder
                        var imageUrl = status.ImageUrl;
                        var uri = new Uri(imageUrl);
                        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                        var imageFilename = queryParams.TryGetValue("filename", out var fn) ? fn.ToString() : null;
                        var imageSubfolder = queryParams.TryGetValue("subfolder", out var sf) ? sf.ToString() : "";
                        
                        _logger.LogInformation("Розпарсено з URL: filename={Filename}, subfolder={Subfolder}", 
                            imageFilename, imageSubfolder);
                        
                        if (!string.IsNullOrEmpty(imageFilename))
                        {
                            // Завантажуємо зображення з ComfyUI
                            _logger.LogInformation("Завантаження зображення: {Filename}, Subfolder: {Subfolder}", 
                                imageFilename, imageSubfolder);
                            
                            var imageData = await _comfyUIService.DownloadImageAsync(imageFilename, imageSubfolder);
                            
                            if (imageData != null && imageData.Length > 0)
                            {
                                // Генеруємо унікальне ім'я файлу
                                var uniqueFilename = $"{Guid.NewGuid()}_{imageFilename}";
                                
                                // Зберігаємо локально
                                var localImageUrl = await _imageStorage.SaveImageAsync(imageData, uniqueFilename);
                                
                                _logger.LogInformation("Зображення збережено локально: {Url}", localImageUrl);
                                
                        // Оновлюємо товар, якщо вказано productId (використовуємо новий scope)
                                if (productId.HasValue)
                                {
                                    using var scope = _scopeFactory.CreateScope();
                                    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                                    var product = await context.Products.FindAsync(productId.Value);
                                    
                                    if (product != null)
                                    {
                                        // Присвоюємо URL відповідному тону шкіри
                                        if (!string.IsNullOrEmpty(skinToneKey))
                                        {
                                            switch (skinToneKey)
                                            {
                                                case "light":
                                                    product.ImageUrlLight = localImageUrl;
                                                    break;
                                                case "medium":
                                                    product.ImageUrlMedium = localImageUrl;
                                                    break;
                                                case "dark":
                                                    product.ImageUrlDark = localImageUrl;
                                                    break;
                                            }

                                            // За замовчуванням робимо середній тон основним, якщо ще не задано
                                            if (string.IsNullOrEmpty(product.ImageUrl) &&
                                                (skinToneKey == "medium" || skinToneKey == "light" || skinToneKey == "dark"))
                                            {
                                                product.ImageUrl = localImageUrl;
                                            }
                                        }
                                        else
                                        {
                                            // Старий режим: одна картинка
                                            product.ImageUrl = localImageUrl;
                                        }

                                        product.ImageGenerationStatus = GenerationStatus.Completed;
                                        await context.SaveChangesAsync();
                                        _logger.LogInformation(
                                            "Товар {ProductId} оновлено з URL зображення (тон {Tone}): {Url}",
                                            productId, skinToneKey ?? "default", localImageUrl);
                                        imageProcessed = true; // Позначаємо як оброблене
                                        return; // Виходимо з циклу
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Товар {ProductId} не знайдено для оновлення", productId);
                                    }
                                }
                                else
                                {
                                    imageProcessed = true;
                                    return;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Не вдалося завантажити зображення з ComfyUI");
                            }
                        }
                    }
                    
                    if (status != null && status.Status == GenerationStatus.Failed)
                    {
                        _logger.LogError("Генерація зображення не вдалася для Prompt ID: {PromptId}. Помилка: {Error}", 
                            promptId, status.Error);
                        
                        // Оновлюємо статус товару на Failed
                        if (productId.HasValue)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                            var product = await context.Products.FindAsync(productId.Value);
                            if (product != null)
                            {
                                product.ImageGenerationStatus = GenerationStatus.Failed;
                                await context.SaveChangesAsync();
                                _logger.LogInformation("Статус товару {ProductId} оновлено на Failed", productId);
                            }
                        }
                        
                        return;
                    }

                    // Логуємо прогрес кожні 10 спроб (30 секунд)
                    if (attempt % 10 == 0)
                    {
                        _logger.LogInformation("Генерація в процесі... (спроба {Attempt}/{MaxAttempts}, минуло ~{Seconds} секунд)", 
                            attempt, maxAttempts, attempt * 3);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Помилка при перевірці статусу генерації");
                }
            }

            if (!imageProcessed)
            {
                _logger.LogWarning("Генерація зображення не завершена в межах таймауту. Prompt ID: {PromptId}", promptId);
                
                // Оновлюємо статус товару на Failed, якщо таймаут
                if (productId.HasValue)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                    var product = await context.Products.FindAsync(productId.Value);
                    if (product != null)
                    {
                        product.ImageGenerationStatus = GenerationStatus.Failed;
                        await context.SaveChangesAsync();
                        _logger.LogWarning("Статус товару {ProductId} оновлено на Failed через таймаут", productId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці генерації зображення. Prompt ID: {PromptId}", promptId);
        }
    }
}

