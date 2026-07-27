using Cysharp.Threading.Tasks;

namespace CrateExpectations.Core.Services
{
    public interface ISaveService
    {
        UniTask SaveAsync<T>(string key, T data);
        UniTask<T> LoadAsync<T>(string key);
        UniTask<bool> ExistsAsync(string key);
    }
}
