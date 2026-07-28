using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CrateExpectations.Cargo.Catalog
{
    /// <summary>
    /// Реализация каталога на Addressables. Каждый созданный экземпляр учитывается, чтобы при
    /// уничтожении владельца (scope контейнера) всё было освобождено: Addressables держит
    /// контент, пока его явно не отпустят.
    ///
    /// <para><b>Что через Addressables идёт, а что нет.</b> Через бандлы едет только то, что
    /// рисуется, - префабы ящиков. Типы груза приходят из
    /// <see cref="CargoRegistryDefinition"/>, то есть из сборки игры, и существуют в
    /// единственном экземпляре.</para>
    ///
    /// <para>Разделение не косметическое. Ассет, на который ссылается адресуемый префаб,
    /// Unity кладёт <b>копию</b> в бандл, и эта копия - другой объект, чем оригинал в сборке.
    /// Всё, что сравнивает определения по ссылке - "сдали ли груз по заказу", "та ли краска,
    /// что требует регламент", "какой ящик писать в сохранение", - на такой копии молча
    /// отвечает "нет". В редакторе этого не видно: там Addressables отдаёт тот же самый
    /// ассет, и баг живёт до первого билда.</para>
    /// </summary>
    public sealed class AddressableCargoCatalog : ICargoCatalog, IDisposable
    {
        private readonly CargoRegistryDefinition _registry;
        private readonly List<GameObject> _instances = new();

        private bool _disposed;

        public AddressableCargoCatalog(CargoRegistryDefinition registry) => _registry = registry;

        /// <inheritdoc />
        public UniTask<CargoTypeDefinition> LoadTypeAsync(
            string cargoTypeKey, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(cargoTypeKey))
                throw new ArgumentException("Пустой ключ типа груза.", nameof(cargoTypeKey));

            // Ждать нечего: тип уже в памяти. Метод остаётся асинхронным, потому что это
            // контракт каталога, а не свойство текущей реализации, - реализация, которая
            // будет тянуть типы с сервера, встанет сюда без правок у вызывающих
            return UniTask.FromResult(_registry.CargoByKey(cargoTypeKey));
        }

        /// <inheritdoc />
        public async UniTask<CargoBox> SpawnAsync(
            string cargoTypeKey, Vector3 position, Quaternion rotation, CancellationToken ct = default)
        {
            CargoTypeDefinition type = await LoadTypeAsync(cargoTypeKey, ct);
            ThrowIfDisposed();

            if (type == null)
            {
                Debug.LogError($"Тип груза по ключу '{cargoTypeKey}' не заведён в реестре.");
                return null;
            }

            if (string.IsNullOrEmpty(type.PrefabKey))
            {
                Debug.LogError($"У типа груза по ключу '{cargoTypeKey}' не задан PrefabKey.");
                return null;
            }

            GameObject instance = await Addressables
                .InstantiateAsync(type.PrefabKey, position, rotation)
                .ToUniTask(cancellationToken: ct);

            if (instance == null) 
                return null;

            // Экземпляр уже создан: если запрос отменили ровно сейчас, его всё равно нужно вернуть,
            // иначе Addressables останется с висящей ссылкой
            if (_disposed)
            {
                Addressables.ReleaseInstance(instance);
                return null;
            }

            _instances.Add(instance);

            if (!instance.TryGetComponent(out CargoBox box))
            {
                Debug.LogError($"Префаб '{type.PrefabKey}' - не ящик груза: нет CargoBox.", instance);
                return null;
            }

            // Истина ящика приходит из каталога: префаб-вариант отвечает за внешний вид,
            // а за то, что внутри, - тип груза, по ключу которого его и запросили
            box.AssignIdentity(type);

            return box;
        }

        /// <inheritdoc />
        public void Despawn(CargoBox box)
        {
            if (box == null) 
                return;

            GameObject instance = box.gameObject;

            if (_instances.Remove(instance)) 
                Addressables.ReleaseInstance(instance);
            else 
                UnityEngine.Object.Destroy(instance);
        }

        /// <summary>Освободить все экземпляры и хендлы. Зовётся контейнером вместе со scope</summary>
        public void Dispose()
        {
            if (_disposed) 
                return;

            _disposed = true;

            for (int i = 0; i < _instances.Count; i++)
                if (_instances[i] != null) 
                    Addressables.ReleaseInstance(_instances[i]);

            _instances.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) 
                throw new ObjectDisposedException(nameof(AddressableCargoCatalog));
        }
    }
}
