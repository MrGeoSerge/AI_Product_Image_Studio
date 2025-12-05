namespace ShopAPI.Services;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(byte[] imageData, string filename);
    string GetImageUrl(string filename);
}

