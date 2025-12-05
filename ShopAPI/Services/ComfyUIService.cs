using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;
using ShopAPI.Models;

namespace ShopAPI.Services;

public class ComfyUIService : IComfyUIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ComfyUIService> _logger;
    private readonly string _baseUrl;
    private readonly string _wsUrl;
    private readonly int _timeoutSeconds;

    // Workflow template для генерації
    private readonly Dictionary<string, object> _workflowTemplate;

    public ComfyUIService(IConfiguration configuration, ILogger<ComfyUIService> logger)
    {
        _baseUrl = configuration["ComfyUI:BaseUrl"] ?? "http://localhost:8188";
        _wsUrl = configuration["ComfyUI:WebSocketUrl"] ?? "ws://localhost:8188/ws";
        _timeoutSeconds = int.Parse(configuration["ComfyUI:TimeoutSeconds"] ?? "120");
        
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl), Timeout = TimeSpan.FromSeconds(_timeoutSeconds) };
        _logger = logger;

        // Базовий workflow template
        _workflowTemplate = CreateWorkflowTemplate();
    }

    public async Task<string> QueuePromptAsync(string positivePrompt, string negativePrompt = "")
    {
        try
        {
            var workflow = CreateWorkflow(positivePrompt, negativePrompt);
            var clientId = Guid.NewGuid().ToString();
            
            // Перевірка, що workflow не порожній
            if (workflow == null || workflow.Count == 0)
            {
                throw new InvalidOperationException("Workflow is empty or invalid");
            }
            
            // Перевірка, що workflow має SaveImage node (output)
            if (!workflow.ContainsKey("9") || 
                !(workflow["9"] is Dictionary<string, object> saveImageNode) ||
                saveImageNode.GetValueOrDefault("class_type")?.ToString() != "SaveImage")
            {
                _logger.LogError("Workflow missing SaveImage node! Available nodes: {Nodes}", 
                    string.Join(", ", workflow.Keys));
                throw new InvalidOperationException("Workflow must contain SaveImage node (output)");
            }
            
            // Створюємо request об'єкт
            var requestObj = new Dictionary<string, object>
            {
                ["prompt"] = workflow,
                ["client_id"] = clientId
            };
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(requestObj, options);
            
            _logger.LogDebug("Відправляємо workflow до ComfyUI ({NodeCount} nodes)", workflow.Count);
            _logger.LogDebug("Workflow JSON (first 1000 chars): {Json}", 
                json.Length > 1000 ? json.Substring(0, 1000) + "..." : json);
            _logger.LogInformation("Відправка запиту до ComfyUI з промптом: {Prompt}", positivePrompt);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/prompt", content);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ComfyUI повернув помилку: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"ComfyUI error: {response.StatusCode} - {responseContent}");
            }
            
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            var promptId = result?.GetValueOrDefault("prompt_id")?.ToString();
            _logger.LogInformation("ComfyUI прийняв запит, prompt_id: {PromptId}", promptId);

            if (string.IsNullOrEmpty(promptId))
            {
                _logger.LogWarning("ComfyUI не повернув prompt_id. Response: {Response}", responseContent);
                return clientId;
            }
            
            return promptId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці запиту до ComfyUI");
            throw;
        }
    }

    public async Task<GenerateImageResponse?> CheckGenerationStatusAsync(string promptId)
    {
        try
        {
            // ComfyUI history endpoint повертає всі виконані промпти
            // Потрібно знайти наш prompt_id в історії
            var response = await _httpClient.GetAsync("/history");
            
            if (!response.IsSuccessStatusCode)
            {
                return new GenerateImageResponse
                {
                    JobId = promptId,
                    Status = GenerationStatus.Pending
                };
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("History response: {Content}", content);
            
            var history = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
            
            if (history == null)
            {
                _logger.LogWarning("History is null for prompt ID: {PromptId}", promptId);
                return new GenerateImageResponse
                {
                    JobId = promptId,
                    Status = GenerationStatus.Processing
                };
            }

            if (!history.ContainsKey(promptId))
            {
                // Prompt ще не виконано
                _logger.LogDebug("Prompt ID {PromptId} not found in history yet. Available IDs: {Ids}", 
                    promptId, string.Join(", ", history.Keys.Take(5)));
                return new GenerateImageResponse
                {
                    JobId = promptId,
                    Status = GenerationStatus.Processing
                };
            }
            
            _logger.LogInformation("Prompt ID {PromptId} found in history", promptId);

            var promptHistory = history[promptId];
            
            // Перевіряємо чи є outputs (означає що генерація завершена)
            if (promptHistory.TryGetProperty("outputs", out var outputs))
            {
                // Знаходимо SaveImage node (зазвичай це node "9")
                foreach (var output in outputs.EnumerateObject())
                {
                    if (output.Value.TryGetProperty("images", out var images))
                    {
                        var imagesArray = images.EnumerateArray().ToList();
                        if (imagesArray.Count > 0)
                        {
                            var firstImage = imagesArray[0];
                            var filename = firstImage.GetProperty("filename").GetString();
                            var subfolder = firstImage.TryGetProperty("subfolder", out var sf) 
                                ? sf.GetString() ?? "" 
                                : "";
                            
                            if (!string.IsNullOrEmpty(filename))
                            {
                                _logger.LogInformation("Генерація завершена! Файл: {Filename}, Subfolder: {Subfolder}", 
                                    filename, subfolder);
                                
                                // Формуємо повний URL для зображення
                                var imageUrl = string.IsNullOrEmpty(subfolder)
                                    ? $"{_baseUrl}/view?filename={filename}&type=output"
                                    : $"{_baseUrl}/view?filename={filename}&subfolder={subfolder}&type=output";
                                
                                return new GenerateImageResponse
                                {
                                    JobId = promptId,
                                    Status = GenerationStatus.Completed,
                                    ImageUrl = imageUrl
                                };
                            }
                        }
                    }
                }
            }

            return new GenerateImageResponse
            {
                JobId = promptId,
                Status = GenerationStatus.Processing
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці статусу генерації");
            return new GenerateImageResponse
            {
                JobId = promptId,
                Status = GenerationStatus.Failed,
                Error = ex.Message
            };
        }
    }

    public async Task<byte[]?> DownloadImageAsync(string filename, string subfolder)
    {
        try
        {
            // Формуємо URL правильно
            var url = string.IsNullOrEmpty(subfolder)
                ? $"/view?filename={Uri.EscapeDataString(filename)}&type=output"
                : $"/view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder)}&type=output";
            
            _logger.LogDebug("Завантаження зображення з URL: {Url}", url);
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Помилка завантаження зображення: {StatusCode} - {Reason}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }
            
            var imageData = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("Зображення завантажено: {Size} bytes", imageData.Length);
            
            return imageData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при завантаженні зображення {Filename}", filename);
            return null;
        }
    }

    private Dictionary<string, object> CreateWorkflowTemplate()
    {
        // Базовий workflow - буде використовувати модель з ComfyUI
        // Використовуємо realisticVisionV60B1_v51HyperVAE.safetensors з папки checkpoints
        return new Dictionary<string, object>
        {
            ["4"] = new Dictionary<string, object>
            {
                ["class_type"] = "CheckpointLoaderSimple",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["ckpt_name"] = "realisticVisionV60B1_v51HyperVAE.safetensors"
                }
            },
            ["5"] = new Dictionary<string, object>
            {
                ["class_type"] = "EmptyLatentImage",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["batch_size"] = 1,
                    ["height"] = 512,
                    ["width"] = 512
                }
            }
        };
    }

    private Dictionary<string, object> CreateWorkflow(string positivePrompt, string negativePrompt)
    {
        var workflow = new Dictionary<string, object>(_workflowTemplate);

        // Додаємо CLIPTextEncode для позитивного промпту
        workflow["6"] = new Dictionary<string, object>
        {
            ["class_type"] = "CLIPTextEncode",
            ["inputs"] = new Dictionary<string, object>
            {
                ["clip"] = new object[] { "4", 1 },
                ["text"] = positivePrompt
            }
        };

        // Додаємо CLIPTextEncode для негативного промпту
        var negativeText = string.IsNullOrEmpty(negativePrompt) 
            ? "bad quality, blurry, distorted, low resolution" 
            : negativePrompt;
        
        workflow["7"] = new Dictionary<string, object>
        {
            ["class_type"] = "CLIPTextEncode",
            ["inputs"] = new Dictionary<string, object>
            {
                ["clip"] = new object[] { "4", 1 },
                ["text"] = negativeText
            }
        };

        // KSampler
        workflow["3"] = new Dictionary<string, object>
        {
            ["class_type"] = "KSampler",
            ["inputs"] = new Dictionary<string, object>
            {
                ["seed"] = new Random().Next(0, int.MaxValue),
                ["steps"] = 20,
                ["cfg"] = 8.0,
                ["sampler_name"] = "euler",
                ["scheduler"] = "normal",
                ["denoise"] = 1.0,
                ["model"] = new object[] { "4", 0 },
                ["positive"] = new object[] { "6", 0 },
                ["negative"] = new object[] { "7", 0 },
                ["latent_image"] = new object[] { "5", 0 }
            }
        };

        // VAEDecode
        workflow["8"] = new Dictionary<string, object>
        {
            ["class_type"] = "VAEDecode",
            ["inputs"] = new Dictionary<string, object>
            {
                ["samples"] = new object[] { "3", 0 },
                ["vae"] = new object[] { "4", 2 }
            }
        };

        // SaveImage
        workflow["9"] = new Dictionary<string, object>
        {
            ["class_type"] = "SaveImage",
            ["inputs"] = new Dictionary<string, object>
            {
                ["filename_prefix"] = "product",
                ["images"] = new object[] { "8", 0 }
            }
        };

        return workflow;
    }
}

