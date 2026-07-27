using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;

namespace CrateExpectations.EditorTools.Validation
{
    public sealed class AddressableKeysCheck : IContentCheck
    {
        public string Title => "Ключи Addressables";

        public void Run(ContentCatalog catalog, List<ContentIssue> issues)
        {
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
