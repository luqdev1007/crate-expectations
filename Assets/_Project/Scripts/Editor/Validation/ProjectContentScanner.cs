using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Inspection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using Object = UnityEngine.Object;

namespace CrateExpectations.EditorTools.Validation
{
    public static class ProjectContentScanner
    {
        public static ContentCatalog Scan()
        {
            return new ContentCatalog(
                FindAll<ContractDefinition>(),
                FindAll<ContractCatalogDefinition>(),
                FindAll<CargoTypeDefinition>(),
                FindAll<DisguiseRecipe>(),
                FindAll<DisguiseStationDefinition>(),
                FindAll<PortRegulationsDefinition>(),
                FindAll<InspectorProfile>(),
                FindAll<CargoManifestDefinition>(),
                ScanAddressables(),
                FindAll<CargoRegistryDefinition>());
        }

        public static List<T> FindAll<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var found = new List<T>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset != null) 
                    found.Add(asset);
            }

            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            return found;
        }

        private static Dictionary<string, Object> ScanAddressables()
        {
            var entries = new Dictionary<string, Object>();

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.SettingsExists
                ? AddressableAssetSettingsDefaultObject.Settings
                : null;

            if (settings == null) 
                return entries;

            var all = new List<AddressableAssetEntry>();
            settings.GetAllAssets(all, includeSubObjects: false);

            for (int i = 0; i < all.Count; i++)
            {
                AddressableAssetEntry entry = all[i];

                if (entry == null || string.IsNullOrEmpty(entry.address)) 
                    continue;

                entries[entry.address] = AssetDatabase.LoadAssetAtPath<Object>(entry.AssetPath);
            }

            return entries;
        }
    }
}
