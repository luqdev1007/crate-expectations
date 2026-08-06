using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CrateExpectations.Core.Services;

namespace CrateExpectations.Platform
{
    /// <summary>Локальное сохранение в JSON. Интерфейс готов под облако Steam</summary>
    public sealed class StubSaveService : ISaveService
    {
        private static string PathFor(string key) =>
            Path.Combine(Application.persistentDataPath, $"{key}.json");

        public async UniTask SaveAsync<T>(string key, T data)
        {
            var json = JsonUtility.ToJson(data, prettyPrint: true);

            await File.WriteAllTextAsync(PathFor(key), json);
        }

        public async UniTask<T> LoadAsync<T>(string key)
        {
            var path = PathFor(key);

            if (File.Exists(path) == false) 
                return default;

            var json = await File.ReadAllTextAsync(path);

            return JsonUtility.FromJson<T>(json);
        }

        public UniTask<bool> ExistsAsync(string key) =>
            UniTask.FromResult(File.Exists(PathFor(key)));
    }
}
