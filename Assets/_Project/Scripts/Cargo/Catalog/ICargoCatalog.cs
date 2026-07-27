using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    public interface ICargoCatalog
    {
        /// <summary>Загрузить описание типа груза по ключу контента</summary>
        UniTask<CargoTypeDefinition> LoadTypeAsync(string cargoTypeKey, CancellationToken ct = default);

        /// <summary>Создать ящик нужного типа в мире</summary>
        UniTask<CargoBox> SpawnAsync(
            string cargoTypeKey, Vector3 position, Quaternion rotation, CancellationToken ct = default);

        /// <summary>Убрать ящик из мира и вернуть его ресурсы</summary>
        void Despawn(CargoBox box);
    }
}
