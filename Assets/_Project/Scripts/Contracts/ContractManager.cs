using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Core.Events;
using CrateExpectations.Contracts.Events;
using CrateExpectations.Economy;
using CrateExpectations.Inspection.Events;
using CrateExpectations.Inventory;
using UnityEngine;

namespace CrateExpectations.Contracts
{
    /// <summary>
    /// Оркестратор игрового цикла: держит взятый заказ, слушает досмотры и превращает
    /// вердикт в деньги и прогресс. Единственное место, где встречаются груз, инспекция
    /// и кошелёк, - и именно поэтому ни один из них не знает про остальных.
    ///
    /// <para>Обычный C#-класс. Подписка живёт ровно столько же, сколько объект: заводится
    /// в конструкторе, снимается в <see cref="Dispose"/>, - а время жизни объекта держит
    /// DI-контейнер. Так подписку невозможно забыть снять.</para>
    /// </summary>
    public sealed class ContractManager : IContractManager, IDisposable
    {
        private readonly ContractCatalogDefinition _catalog;
        private readonly PayoutCalculator _calculator;
        private readonly IEconomyService _economy;
        private readonly ICargoInventory _inventory;
        private readonly IEventBus _bus;

        /// <summary>Заказы, листки которых уже сняли с доски. Пополняется, но не чистится</summary>
        private readonly HashSet<string> _taken = new();

        private readonly List<ContractDefinition> _onBoard = new();

        private ContractProgress _active;

        public ContractManager(
            ContractCatalogDefinition catalog,
            PayoutCalculator calculator,
            IEconomyService economy,
            ICargoInventory inventory,
            IEventBus bus)
        {
            _catalog = catalog;
            _calculator = calculator;
            _economy = economy;
            _inventory = inventory;
            _bus = bus;

            _bus.Subscribe<CargoInspected>(OnCargoInspected);
        }

        /// <inheritdoc />
        public ContractProgress Active => _active;

        /// <inheritdoc />
        public IReadOnlyList<ContractDefinition> Available
        {
            get
            {
                _onBoard.Clear();

                if (_catalog == null)
                    return _onBoard;

                IReadOnlyList<ContractDefinition> all = _catalog.Contracts;

                for (int i = 0; i < all.Count; i++)
                    if (all[i] != null && !IsTaken(all[i]))
                        _onBoard.Add(all[i]);

                return _onBoard;
            }
        }

        /// <inheritdoc />
        public void Dispose() => _bus.Unsubscribe<CargoInspected>(OnCargoInspected);

        public bool IsTaken(ContractDefinition contract) =>
            contract != null && _taken.Contains(contract.name);

        /// <summary>
        /// Заказ берётся по одному. Несколько параллельных означали бы, что на каждый сданный
        /// ящик нужно решать, в чей зачёт он идёт, - вопрос дизайна, на который MVP отвечать
        /// не обязан, а цена ошибки в нём выше, чем польза
        /// </summary>
        public bool CanAccept(ContractDefinition contract) =>
            contract != null && contract.IsPlayable && !_active.IsActive && !IsTaken(contract);

        /// <inheritdoc />
        public bool Accept(ContractDefinition contract)
        {
            if (!CanAccept(contract))
                return false;

            _taken.Add(contract.name);

            _active = new ContractProgress(contract);
            _bus.Publish(new ContractAccepted(_active));

            return true;
        }

        public ContractSnapshot Capture()
        {
            var taken = new string[_taken.Count];
            _taken.CopyTo(taken);

            return new ContractSnapshot
            {
                ContractId = _active.IsActive ? _active.Contract.name : string.Empty,
                Delivered = _active.Delivered,
                Seized = _active.Seized,
                TakenIds = taken,
            };
        }

        /// <summary>
        /// Заказ из сохранения. Имя разрешается по каталогу - если такого заказа в каталоге
        /// больше нет (переименовали, выкинули из смены), игрок остаётся без активного заказа
        /// и может взять новый. Это хуже, чем ничего не потерять, но лучше, чем упасть
        /// на загрузке чужого сейва
        /// </summary>
        public void Restore(in ContractSnapshot snapshot)
        {
            RestoreTaken(snapshot.TakenIds);

            ContractDefinition contract = FindById(snapshot.ContractId);

            if (contract == null)
            {
                if (!string.IsNullOrEmpty(snapshot.ContractId))
                {
                    Debug.LogWarning(
                        $"[Контракты] В сохранении заказ '{snapshot.ContractId}', " +
                        "а в каталоге такого нет. Игрок остался без активного заказа");
                }

                _active = default;

                return;
            }

            // Активный заказ по определению уже снят с доски, даже если в списке его почему-то нет
            _taken.Add(contract.name);

            _active = new ContractProgress(contract, snapshot.Delivered, snapshot.Seized);
        }

        private void RestoreTaken(string[] takenIds)
        {
            _taken.Clear();

            if (takenIds == null)
                return;

            for (int i = 0; i < takenIds.Length; i++)
                if (!string.IsNullOrEmpty(takenIds[i]))
                    _taken.Add(takenIds[i]);
        }

        /// <summary>Ищем по всему каталогу, а не по доске: активный заказ уже снят с неё</summary>
        private ContractDefinition FindById(string contractId)
        {
            if (string.IsNullOrEmpty(contractId) || _catalog == null)
                return null;

            IReadOnlyList<ContractDefinition> all = _catalog.Contracts;

            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].name == contractId)
                    return all[i];

            return null;
        }

        /// <summary>
        /// Груз прошёл досмотр. Дальше решается только одно: считается ли эта сдача
        /// в текущий заказ, - и если да, во что она обходится
        /// </summary>
        private void OnCargoInspected(CargoInspected inspected)
        {
            if (inspected.Cargo == null) 
                return;

            // Ящик, чья судьба уже решена, второй раз не считается. Инспектор смотрит всё,
            // что попадает в зону, - в том числе груз, который он только что пропустил
            // и который никто не унёс; платить за него дважды заказчик не станет
            int id = inspected.Cargo.GetInstanceID(); // fix

            if (_inventory.TryGet(id, out CargoRecord record) && !record.IsOnDock)
                return;

            bool seized = inspected.Verdict.IsBust;

            // Реестр обновляется всегда: ящик прошёл досмотр независимо от того,
            // засчитает его заказчик или нет
            _inventory.Settle(id, seized ? CargoStanding.Seized : CargoStanding.Delivered);

            if (!_active.IsActive) 
                return;

            if (!MatchesActiveContract(inspected.Cargo))
                return;

            // Перевод вердикта на язык денег: улики и пороги дальше не идут
            var delivery = new DeliveryReport(
                seized ? DeliveryOutcome.Seized : DeliveryOutcome.Cleared,
                spotless: inspected.Verdict.Clues.Count == 0);

            _economy.Apply(_calculator.Calculate(_active.Contract.Terms, delivery));

            _active = seized ? _active.WithSeizure() : _active.WithDelivery();
            _bus.Publish(new ContractProgressed(_active, seized));

            if (_active.IsComplete) 
                Close(completed: true);
            else if (_active.IsFailed) 
                Close(completed: false);
        }

        /// <summary>
        /// Сдали не то, что заказывали. Такое бывает: на доке лежит и посторонний груз,
        /// а инспектор смотрит всё подряд. Заказчику до этого дела нет - ни платы,
        /// ни штрафа, ни прогресса; ломаться тут нечему
        /// </summary>
        private bool MatchesActiveContract(CargoBox cargo)
        {
            if (cargo == null) 
                return false;

            CargoTypeDefinition truth = cargo.Identity.TrueType;

            if (truth == _active.Contract.Cargo) 
                return true;

            Debug.Log(
                $"[Контракты] Сдан груз не по заказу: внутри {Name(truth)}, " +
                $"а заказан {Name(_active.Contract.Cargo)}. Заказчик это не считает", cargo);
            return false;
        }

        private static string Name(CargoTypeDefinition type) =>
            type != null ? type.DisplayName : "неизвестно что";

        private void Close(bool completed)
        {
            ContractProgress finished = _active;
            _active = default;

            if (completed) 
                _bus.Publish(new ContractCompleted(finished));
            else 
                _bus.Publish(new ContractFailed(finished));
        }
    }
}
