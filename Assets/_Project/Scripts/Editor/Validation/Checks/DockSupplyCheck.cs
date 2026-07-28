using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;

namespace CrateExpectations.EditorTools.Validation
{
    /// <summary>
    /// Манифест дока против заказов: хватит ли на локации ящиков нужного типа, чтобы заказ вообще
    /// можно было закрыть (ровно эта дыра всплыла в Фазе 4 - заказ на два ящика рома при манифесте
    /// ровно на два: один потерял на досмотре, и заказ уже не сдать)
    /// </summary>
    public sealed class DockSupplyCheck : IContentCheck
    {
        private readonly Dictionary<CargoTypeDefinition, int> _supply = new();

        /// <inheritdoc />
        public string Title => "Груз на доке";

        /// <inheritdoc />
        public void Run(ContentCatalog catalog, List<ContentIssue> issues)
        {
            if (catalog.Manifests.Count == 0 || catalog.Contracts.Count == 0) 
                return;

            for (int m = 0; m < catalog.Manifests.Count; m++)
            {
                CargoManifestDefinition manifest = catalog.Manifests[m];

                if (manifest == null)
                    continue;

                CountSupply(catalog, manifest, issues);

                for (int c = 0; c < catalog.Contracts.Count; c++)
                    CheckContract(catalog.Contracts[c], manifest, issues);
            }
        }

        private void CountSupply(
            ContentCatalog catalog, CargoManifestDefinition manifest, List<ContentIssue> issues)
        {
            _supply.Clear();

            string[] keys = manifest.CargoKeys;
            for (int i = 0; i < keys.Length; i++)
            {
                CargoTypeDefinition type = catalog.CargoTypeAt(keys[i]);
                if (type == null)
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Манифест \"{manifest.name}\": по ключу \"{keys[i]}\" нет типа груза " +
                        "в реестре груза - ящик не появится",
                        manifest));
                    continue;
                }

                _supply.TryGetValue(type, out int count);
                _supply[type] = count + 1;
            }
        }

        private void CheckContract(
            ContractDefinition contract, CargoManifestDefinition manifest, List<ContentIssue> issues)
        {
            if (contract == null || contract.Cargo == null) 
                return;

            _supply.TryGetValue(contract.Cargo, out int available);

            if (available < contract.Crates)
            {
                issues.Add(ContentIssue.Error(
                    Title,
                    $"Заказ \"{contract.DisplayName}\" требует {contract.Crates} ящ. " +
                    $"\"{contract.Cargo.DisplayName}\", а манифест \"{manifest.name}\" " +
                    $"выставляет {available}. Закрыть заказ нечем",
                    contract));

                return;
            }

            // Изъятый ящик уезжает со склада порта и обратно не возвращается: запас должен
            // покрывать и те, что заказчик готов простить
            int comfortable = contract.Crates + contract.AllowedSeizures;

            if (available < comfortable)
            {
                issues.Add(ContentIssue.Warning(
                    Title,
                    $"Заказ \"{contract.DisplayName}\": ящиков \"{contract.Cargo.DisplayName}\" " +
                    $"на доке ровно {available} при {contract.Crates} нужных и " +
                    $"{contract.AllowedSeizures} прощаемых изъятиях - права на ошибку нет",
                    contract));
            }
        }
    }
}
