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

        public bool IsBusy { get; private set; }

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
                Debug.LogError($"[Сохранение] Записать не вышло: {exception.Message}");
                _bus.Publish(new GameStateFailed(wasSaving: true, "Сохранить не вышло"));
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

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

        public UniTask<bool> HasSaveAsync() => _saves.ExistsAsync(_slot.Key);
    }
}
