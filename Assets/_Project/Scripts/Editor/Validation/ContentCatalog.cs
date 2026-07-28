using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Inspection;
using Object = UnityEngine.Object;

namespace CrateExpectations.EditorTools.Validation
{
    /// <summary>
    /// Снимок контента проекта: все ассеты, которые интересны проверкам, плюс карта адресов
    /// Addressables. Обычный контейнер без единого обращения к <c>AssetDatabase</c> -
    /// собирает его <see cref="ProjectContentScanner"/>, а тест наполняет вручную
    /// и гоняет проверки без папки <c>Assets</c> вообще
    /// </summary>
    public sealed class ContentCatalog
    {
        private static readonly IReadOnlyDictionary<string, Object> NoEntries =
            new Dictionary<string, Object>();

        public ContentCatalog(
            IReadOnlyList<ContractDefinition> contracts = null,
            IReadOnlyList<ContractCatalogDefinition> contractCatalogs = null,
            IReadOnlyList<CargoTypeDefinition> cargoTypes = null,
            IReadOnlyList<DisguiseRecipe> recipes = null,
            IReadOnlyList<DisguiseStationDefinition> stations = null,
            IReadOnlyList<PortRegulationsDefinition> regulations = null,
            IReadOnlyList<InspectorProfile> inspectorProfiles = null,
            IReadOnlyList<CargoManifestDefinition> manifests = null,
            IReadOnlyDictionary<string, Object> addressableEntries = null,
            IReadOnlyList<CargoRegistryDefinition> registries = null)
        {
            Registries = registries ?? Array.Empty<CargoRegistryDefinition>();
            Contracts = contracts ?? Array.Empty<ContractDefinition>();
            ContractCatalogs = contractCatalogs ?? Array.Empty<ContractCatalogDefinition>();
            CargoTypes = cargoTypes ?? Array.Empty<CargoTypeDefinition>();
            Recipes = recipes ?? Array.Empty<DisguiseRecipe>();
            Stations = stations ?? Array.Empty<DisguiseStationDefinition>();
            Regulations = regulations ?? Array.Empty<PortRegulationsDefinition>();
            InspectorProfiles = inspectorProfiles ?? Array.Empty<InspectorProfile>();
            Manifests = manifests ?? Array.Empty<CargoManifestDefinition>();
            AddressableEntries = addressableEntries ?? NoEntries;
        }

        public IReadOnlyList<ContractDefinition> Contracts { get; }

        public IReadOnlyList<ContractCatalogDefinition> ContractCatalogs { get; }

        public IReadOnlyList<CargoTypeDefinition> CargoTypes { get; }

        /// <summary>Все рецепты проекта, включая те, что не стоят ни на одной станции</summary>
        public IReadOnlyList<DisguiseRecipe> Recipes { get; }

        public IReadOnlyList<DisguiseStationDefinition> Stations { get; }

        public IReadOnlyList<PortRegulationsDefinition> Regulations { get; }

        public IReadOnlyList<InspectorProfile> InspectorProfiles { get; }

        public IReadOnlyList<CargoManifestDefinition> Manifests { get; }

        /// <summary>Адрес Addressables → ассет, который под ним лежит</summary>
        public IReadOnlyDictionary<string, Object> AddressableEntries { get; }

        /// <summary>Реестры контента груза. Через них ключ манифеста превращается в тип</summary>
        public IReadOnlyList<CargoRegistryDefinition> Registries { get; }

        /// <summary>Заведён ли такой адрес в группах</summary>
        public bool HasAddress(string key) =>
            !string.IsNullOrEmpty(key) && AddressableEntries.ContainsKey(key);

        /// <summary>
        /// Тип груза под этим ключом контента. Так манифест дока связан с типами.
        ///
        /// <para>Спрашиваем реестр, а не Addressables: в рантайме тип берётся именно оттуда,
        /// и проверка обязана смотреть туда же, куда посмотрит игра.</para>
        /// </summary>
        public CargoTypeDefinition CargoTypeAt(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            for (int i = 0; i < Registries.Count; i++)
            {
                CargoTypeDefinition type = Registries[i] != null
                    ? Registries[i].CargoByKey(key)
                    : null;

                if (type != null) return type;
            }

            return null;
        }

        /// <summary>
        /// Рецепты, до которых игрок реально может дотянуться: те, что стоят на станциях.
        /// Рецепт-ассет сам по себе в игре не существует - его негде применить
        /// </summary>
        public List<DisguiseRecipe> ReachableRecipes()
        {
            var reachable = new List<DisguiseRecipe>(Stations.Count);

            for (int i = 0; i < Stations.Count; i++)
            {
                DisguiseRecipe recipe = Stations[i] != null ? Stations[i].Recipe : null;
                if (recipe != null && !reachable.Contains(recipe)) reachable.Add(recipe);
            }

            return reachable;
        }
    }
}
