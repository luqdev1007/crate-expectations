using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Contracts;
using CrateExpectations.Inspection;

namespace CrateExpectations.EditorTools.Validation
{
    /// <summary>
    /// Пустые и недозаполненные ассеты: то, что в игре превращается в молчаливый no-op
    /// или в <c>NullReferenceException</c> на третьей минуте плейтеста
    /// </summary>
    public sealed class BrokenReferencesCheck : IContentCheck
    {
        /// <inheritdoc />
        public string Title => "Пустые ссылки";

        /// <inheritdoc />
        public void Run(ContentCatalog catalog, List<ContentIssue> issues)
        {
            CheckContracts(catalog, issues);
            CheckRecipes(catalog, issues);
            CheckStations(catalog, issues);
            CheckProfiles(catalog, issues);
            CheckCargoTypes(catalog, issues);
            CheckContractCatalogs(catalog, issues);
        }

        private void CheckContracts(ContentCatalog catalog, List<ContentIssue> issues)
        {
            for (int i = 0; i < catalog.Contracts.Count; i++)
            {
                ContractDefinition contract = catalog.Contracts[i];

                if (contract == null) 
                    continue;

                if (contract.Cargo == null)
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Заказ \"{contract.DisplayName}\": не задан истинный тип груза - " +
                        "такой заказ не предложат игроку",
                        contract));
                }

                if (contract.Crates < 1)
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Заказ \"{contract.DisplayName}\": ящиков {contract.Crates} - " +
                        "заказ нельзя ни взять, ни закрыть",
                        contract));
                }

                if (string.IsNullOrWhiteSpace(contract.Description))
                {
                    issues.Add(ContentIssue.Warning(
                        Title,
                        $"Заказ \"{contract.DisplayName}\": пустое описание - на доске будет " +
                        "пустая строка",
                        contract));
                }

                if (contract.Penalty == 0 && contract.RewardPerCrate == 0)
                {
                    issues.Add(ContentIssue.Warning(
                        Title,
                        $"Заказ \"{contract.DisplayName}\": ни награды, ни штрафа - " +
                        "сдача ничего не изменит",
                        contract));
                }
            }
        }

        private void CheckRecipes(ContentCatalog catalog, List<ContentIssue> issues)
        {
            for (int i = 0; i < catalog.Recipes.Count; i++)
            {
                DisguiseRecipe recipe = catalog.Recipes[i];

                if (recipe == null) 
                    continue;

                // Ровно та же развилка, по которой DisguiseProcessor отклоняет рецепт
                // с DisguiseRejection.IncompleteRecipe - только здесь она видна до игры
                bool complete = recipe.Action switch
                {
                    DisguiseAction.Paint => recipe.Paint != null,
                    DisguiseAction.Stamp => recipe.Stamp != null,
                    DisguiseAction.Pour => recipe.DeclaredType != null,
                    _ => false,
                };

                if (!complete)
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Рецепт \"{recipe.name}\": действие {recipe.Action}, но цель не задана - " +
                        "станция будет отказывать всегда",
                        recipe));
                }
            }
        }

        private void CheckStations(ContentCatalog catalog, List<ContentIssue> issues)
        {
            for (int i = 0; i < catalog.Stations.Count; i++)
            {
                DisguiseStationDefinition station = catalog.Stations[i];

                if (station == null) 
                    continue;

                if (station.Recipe == null)
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Станция \"{station.name}\": не назначен рецепт - станция ничего не делает",
                        station));
                }
            }
        }

        private void CheckProfiles(ContentCatalog catalog, List<ContentIssue> issues)
        {
            for (int i = 0; i < catalog.InspectorProfiles.Count; i++)
            {
                InspectorProfile profile = catalog.InspectorProfiles[i];

                if (profile == null) 
                    continue;

                if (profile.Lines == null)
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Профиль \"{profile.DisplayName}\": не назначены реплики - " +
                        "инспектору нечем озвучить вердикт",
                        profile));
                }

                if (profile.Checks == ClueChecks.None)
                {
                    issues.Add(ContentIssue.Warning(
                        Title,
                        $"Профиль \"{profile.DisplayName}\": не выполняет ни одной проверки - " +
                        "пропустит вообще всё",
                        profile));
                }
            }
        }

        private void CheckCargoTypes(ContentCatalog catalog, List<ContentIssue> issues)
        {
            for (int i = 0; i < catalog.CargoTypes.Count; i++)
            {
                CargoTypeDefinition type = catalog.CargoTypes[i];

                if (type == null) 
                    continue;

                if (string.IsNullOrEmpty(type.PrefabKey))
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Тип груза \"{type.DisplayName}\": не задан ключ префаба - " +
                        "каталог не сможет создать ящик",
                        type));
                }

                if (type.Icon == null)
                {
                    issues.Add(ContentIssue.Warning(
                        Title,
                        $"Тип груза \"{type.DisplayName}\": не задана иконка - на ящике " +
                        "будет пустая грань, и заявленное содержимое не прочитать",
                        type));
                }
            }
        }

        private void CheckContractCatalogs(ContentCatalog catalog, List<ContentIssue> issues)
        {
            for (int i = 0; i < catalog.ContractCatalogs.Count; i++)
            {
                ContractCatalogDefinition board = catalog.ContractCatalogs[i];

                if (board == null) 
                    continue;

                IReadOnlyList<ContractDefinition> contracts = board.Contracts;

                if (contracts.Count == 0)
                {
                    issues.Add(ContentIssue.Warning(
                        Title, $"Каталог \"{board.name}\" пуст - доска заказов будет пустой", board));
                }

                for (int c = 0; c < contracts.Count; c++)
                {
                    if (contracts[c] == null)
                    {
                        issues.Add(ContentIssue.Error(
                            Title,
                            $"Каталог \"{board.name}\": пустая ссылка на заказ в позиции {c}",
                            board));
                    }
                }
            }
        }
    }
}
