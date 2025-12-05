using ShopAPI.Models;

namespace ShopAPI.Services;

public interface IComfyUIService
{
    Task<string> QueuePromptAsync(string positivePrompt, string negativePrompt = "");
    Task<GenerateImageResponse?> CheckGenerationStatusAsync(string promptId);
    Task<byte[]?> DownloadImageAsync(string filename, string subfolder);
}

