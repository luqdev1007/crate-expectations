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
    public sealed class CargoHandoff : IDisposable
    {
        private readonly ICargoCatalog _catalog;
        private readonly InspectionDefinition _inspection;
        private readonly IEventBus _bus;

        private readonly CancellationTokenSource _cts = new();

        public CargoHandoff(ICargoCatalog catalog, InspectionDefinition inspection, IEventBus bus)
        {
            _catalog = catalog;
            _inspection = inspection;
            _bus = bus;

            _bus.Subscribe<CargoInspected>(OnCargoInspected);
        }

        public void Dispose()
        {
            _bus.Unsubscribe<CargoInspected>(OnCargoInspected);

            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnCargoInspected(CargoInspected inspected)
        {
            if (inspected.Cargo != null) 
                TakeAwayAsync(inspected.Cargo, _cts.Token).Forget();
        }

        private async UniTaskVoid TakeAwayAsync(CargoBox cargo, CancellationToken token)
        {
            bool cancelled = await UniTask
                .Delay(TimeSpan.FromSeconds(_inspection.CargoHandoffSeconds), cancellationToken: token)
                .SuppressCancellationThrow();

            if (cancelled) 
                return;

            if (cargo != null) 
                _catalog.Despawn(cargo);
        }
    }
}
