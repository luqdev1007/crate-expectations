using Cysharp.Threading.Tasks;

namespace CrateExpectations.Core.Services
{
    /// <summary>Абстракция сохранения. Локально сейчас, готова под облако Steam</summary>
    public interface ISaveService
    {
        UniTask SaveAsync<T>(string key, T data);
        UniTask<T> LoadAsync<T>(string key);
        UniTask<bool> ExistsAsync(string key);
    }
}
