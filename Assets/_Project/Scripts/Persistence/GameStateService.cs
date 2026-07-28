using System;
using System.Threading;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Core.Events;
using CrateExpectations.Core.Services;
using CrateExpectations.Economy;
using CrateExpectations.Inventory;
using CrateExpectations.Persistence.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Persistence
{
    /// <summary>
    /// Координатор сохранения. Спрашивает у каждой системы её снимок, складывает их в один
    /// <see cref="GameSnapshot"/> и отдаёт <see cref="ISaveService"/>; при загрузке раздаёт
    /// снимки обратно.
    ///
    /// <para><b>Во внутренности систем не лезет.</b> Кошелёк сам знает, что у него баланс,
    /// менеджер заказов - что у него имя контракта и два счётчика, груз - что у него ящики.
    /// Добавить систему в сохранение значит добавить сюда две строки, а не разобраться,
    /// как эта система устроена.</para>
    ///
    /// <para>Где лежит файл - не знает никто из них, включая этот класс: за ключом слота
    /// стоит <see cref="ISaveService"/>, и подменить диск на облако Steam можно, не тронув
    /// ни строчки здесь.</para>
    /// </summary>
    public sealed class GameStateService : IGameStateService
    {
        private readonly ISaveService _saves;
        private readonly SaveSlotDefinition _slot;
        private readonly IEconomyService _economy;
        private readonly IContractManager _contracts;
        private readonly ICargoInventory _inventory;
        private readonly CargoSceneKeeper _cargo;
        private readonly IEventBus _bus;

        public GameStateService(
            ISaveService saves,
            SaveSlotDefinition slot,
            IEconomyService economy,
            IContractManager contracts,
            ICargoInventory inventory,
            CargoSceneKeeper cargo,
            IEventBus bus)
        {
            _saves = saves;
            _slot = slot;
            _economy = economy;
            _contracts = contracts;
            _inventory = inventory;
            _cargo = cargo;
            _bus = bus;
        }

        /// <inheritdoc />
        public bool IsBusy { get; private set; }

        /// <inheritdoc />
        public async UniTask<bool> SaveAsync(CancellationToken ct = default)
        {
            if (IsBusy) 
                return false;

            IsBusy = true;

            try
            {
                var snapshot = new GameSnapshot
                {
                    Version = GameSnapshot.CurrentVersion,
                    Economy = _economy.Capture(),
                    Contract = _contracts.Capture(),
                    Cargo = _cargo.Capture(),
                };

                await _saves.SaveAsync(_slot.Key, snapshot);

                _bus.Publish(new GameSaved(_slot.DisplayName));

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Диск может быть недоступен, облако - отвалиться. Игру это ронять не должно:
                // не сохранились - сказали об этом и играем дальше
                Debug.LogError($"[Сохранение] Записать не вышло: {exception.Message}");
                _bus.Publish(new GameStateFailed(wasSaving: true, "Сохранить не вышло"));
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <inheritdoc />
        public async UniTask<bool> LoadAsync(CancellationToken ct = default)
        {
            if (IsBusy) 
                return false;

            IsBusy = true;

            try
            {
                var snapshot = await _saves.LoadAsync<GameSnapshot>(_slot.Key);

                if (snapshot == null)
                {
                    _bus.Publish(new GameStateFailed(wasSaving: false, "Сохранения нет"));
                    return false;
                }

                // Чужая версия читается только до конца понятого места, а понятого места нет.
                // Лучше отказаться целиком, чем восстановить половину мира
                if (!snapshot.IsReadable)
                {
                    Debug.LogWarning(
                        $"[Сохранение] Версия файла {snapshot.Version}, игра понимает " +
                        $"{GameSnapshot.CurrentVersion}. Загрузка отменена");
                    _bus.Publish(new GameStateFailed(
                        wasSaving: false, $"Сейв версии {snapshot.Version} эта версия игры не читает"));

                    return false;
                }

                _economy.Restore(snapshot.Economy);
                _contracts.Restore(snapshot.Contract);

                // Реестр очищается до пересоздания груза: новые ящики объявятся в шину сами
                // и встанут на учёт, а старые записи к тому моменту уже никого не касаются
                _inventory.Clear();
                await _cargo.RestoreAsync(snapshot.Cargo, ct);

                _bus.Publish(new GameLoaded(_slot.DisplayName));
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Сохранение] Прочитать не вышло: {exception.Message}");
                _bus.Publish(new GameStateFailed(wasSaving: false, "Загрузить не вышло"));
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <inheritdoc />
        public UniTask<bool> HasSaveAsync() => _saves.ExistsAsync(_slot.Key);
    }
}
