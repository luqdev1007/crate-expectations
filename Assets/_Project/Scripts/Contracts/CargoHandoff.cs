using System;
using System.Threading;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Core.Events;
using CrateExpectations.Inspection;
using CrateExpectations.Inspection.Events;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Contracts
{
    /// <summary>
    /// Досмотренный груз увозят с причала: принятый - на корабль, изъятый - на склад порта.
    /// Пока этого не делал никто, ящик оставался стоять в зоне, и инспектор принимался
    /// осматривать его снова.
    ///
    /// <para>Отдельный класс, а не строчка в <see cref="ContractManager"/>: тот считает
    /// деньги и прогресс и про Addressables знать не должен. И не в модуле досмотра -
    /// <c>Inspection</c> сознательно не решает, что станет с ящиком, а только объявляет
    /// вердикт (см. <c>InspectorAI.CloseCase</c>).</para>
    ///
    /// <para>Груз уезжает не сразу: игрок должен успеть увидеть вердикт над тем самым
    /// ящиком, о котором он вынесен.</para>
    /// </summary>
    public sealed class CargoHandoff : IDisposable
    {
        private readonly ICargoCatalog _catalog;
        private readonly InspectionDefinition _inspection;
        private readonly IEventBus _bus;

        // Общий токен на все отложенные вывозы: со scope контейнера сворачиваются разом
        private readonly CancellationTokenSource _cts = new();

        public CargoHandoff(ICargoCatalog catalog, InspectionDefinition inspection, IEventBus bus)
        {
            _catalog = catalog;
            _inspection = inspection;
            _bus = bus;

            _bus.Subscribe<CargoInspected>(OnCargoInspected);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _bus.Unsubscribe<CargoInspected>(OnCargoInspected);

            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnCargoInspected(CargoInspected inspected)
        {
            if (inspected.Cargo != null) TakeAwayAsync(inspected.Cargo, _cts.Token).Forget();
        }

        private async UniTaskVoid TakeAwayAsync(CargoBox cargo, CancellationToken token)
        {
            bool cancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(_inspection.CargoHandoffSeconds), cancellationToken: token)
                .SuppressCancellationThrow();

            // Сцену закрыли, пока груз ждал отправки, - это не ошибка
            if (cancelled) return;

            // Ящик мог не дожить до отправки: смена закончилась, каталог всё отпустил.
            // Despawn это переживает, но лишний раз его дёргать незачем
            if (cargo != null) _catalog.Despawn(cargo);
        }
    }
}
