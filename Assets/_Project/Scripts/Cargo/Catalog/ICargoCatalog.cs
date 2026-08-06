using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    /// <summary>
    /// Каталог груза: отдаёт типы груза и создаёт ящики по ключу контента.
    /// Игровой код знает только этот интерфейс, а где лежит контент (Addressables, бандлы, ресурсы)
    /// и как он освобождается, его не касается
    /// </summary>
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
