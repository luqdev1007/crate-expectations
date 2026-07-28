using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;

namespace CrateExpectations.EditorTools.Validation
{
    /// <summary>
    /// Ключи Addressables, которых нет в группах: ошибка тихая и коварная - тип груза выглядит
    /// заполненным, а ящик просто не появляется на доке
    /// <para>
    /// Через Addressables едут только префабы, поэтому проверяется только
    /// <see cref="CargoTypeDefinition.PrefabKey"/>: ключи манифеста - это ключи реестра груза,
    /// и за них отвечает <see cref="DockSupplyCheck"/>
    /// </para>
    /// </summary>
    public sealed class AddressableKeysCheck : IContentCheck
    {
        /// <inheritdoc />
        public string Title => "Ключи Addressables";

        /// <inheritdoc />
        public void Run(ContentCatalog catalog, List<ContentIssue> issues)
        {
            // Пустая карта адресов означает, что Addressables не настроены вовсе:
            // ругаться на каждый ключ в такой ситуации - только шуметь
            if (catalog.AddressableEntries.Count == 0)
                return;

            for (int i = 0; i < catalog.CargoTypes.Count; i++)
            {
                CargoTypeDefinition type = catalog.CargoTypes[i];

                if (type == null || string.IsNullOrEmpty(type.PrefabKey)) 
                    continue;

                if (!catalog.HasAddress(type.PrefabKey))
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"Тип груза \"{type.DisplayName}\": ключа префаба \"{type.PrefabKey}\" " +
                        "нет ни в одной группе Addressables - ящик не создастся",
                        type));
                }
            }

            for (int i = 0; i < catalog.Manifests.Count; i++)
            {
                CargoManifestDefinition manifest = catalog.Manifests[i];

                if (manifest == null) 
                    continue;

                string[] keys = manifest.CargoKeys;

                for (int k = 0; k < keys.Length; k++)
                {
                    if (string.IsNullOrEmpty(keys[k]))
                    {
                        issues.Add(ContentIssue.Error(
                            Title, $"Манифест \"{manifest.name}\": пустой ключ в позиции {k}.", manifest));
                    }
                }
            }
        }
    }
}
