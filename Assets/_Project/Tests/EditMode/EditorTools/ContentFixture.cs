using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Inspection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrateExpectations.EditorTools.Tests
{
    public sealed class ContentFixture
    {
        private readonly List<Object> _created = new(16);

        public void Dispose()
        {
            for (int i = 0; i < _created.Count; i++)
                if (_created[i] != null) 
                    Object.DestroyImmediate(_created[i]);

            _created.Clear();
        }

        public CargoTypeDefinition CargoType(string displayName, string prefabKey = "Cargo/Prefab")
        {
            var type = Create<CargoTypeDefinition>(displayName);
            var so = new SerializedObject(type);

            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.FindProperty("<PrefabKey>k__BackingField").stringValue = prefabKey;
            so.ApplyModifiedPropertiesWithoutUndo();

            return type;
        }

        public PaintDefinition Paint(string displayName)
        {
            var paint = Create<PaintDefinition>(displayName);
            SetDisplayName(paint, displayName);

            return paint;
        }

        public StampDefinition Stamp(string displayName)
        {
            var stamp = Create<StampDefinition>(displayName);
            SetDisplayName(stamp, displayName);

            return stamp;
        }

        private static void SetDisplayName(Object asset, string displayName)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public DisguiseRecipe PaintRecipe(PaintDefinition paint, PaintDefinition requiredPaint = null)
        {
            var recipe = Create<DisguiseRecipe>($"Recipe_Paint_{Name(paint)}");
            var so = new SerializedObject(recipe);

            so.FindProperty("<Action>k__BackingField").enumValueIndex = (int)DisguiseAction.Paint;
            so.FindProperty("<Paint>k__BackingField").objectReferenceValue = paint;
            so.FindProperty("<RequiredPaint>k__BackingField").objectReferenceValue = requiredPaint;
            so.ApplyModifiedPropertiesWithoutUndo();

            return recipe;
        }

        public DisguiseRecipe StampRecipe(StampDefinition stamp, PaintDefinition requiredPaint = null)
        {
            var recipe = Create<DisguiseRecipe>($"Recipe_Stamp_{Name(stamp)}");
            var so = new SerializedObject(recipe);

            so.FindProperty("<Action>k__BackingField").enumValueIndex = (int)DisguiseAction.Stamp;
            so.FindProperty("<Stamp>k__BackingField").objectReferenceValue = stamp;
            so.FindProperty("<RequiredPaint>k__BackingField").objectReferenceValue = requiredPaint;
            so.ApplyModifiedPropertiesWithoutUndo();

            return recipe;
        }

        public DisguiseRecipe PourRecipe(CargoTypeDefinition declaredType)
        {
            var recipe = Create<DisguiseRecipe>($"Recipe_Pour_{Name(declaredType)}");
            var so = new SerializedObject(recipe);

            so.FindProperty("<Action>k__BackingField").enumValueIndex = (int)DisguiseAction.Pour;
            so.FindProperty("<DeclaredType>k__BackingField").objectReferenceValue = declaredType;
            so.ApplyModifiedPropertiesWithoutUndo();

            return recipe;
        }

        public DisguiseRecipe EmptyRecipe(DisguiseAction action)
        {
            var recipe = Create<DisguiseRecipe>($"Recipe_Empty_{action}");
            var so = new SerializedObject(recipe);

            so.FindProperty("<Action>k__BackingField").enumValueIndex = (int)action;
            so.ApplyModifiedPropertiesWithoutUndo();

            return recipe;
        }

        public DisguiseStationDefinition Station(DisguiseRecipe recipe)
        {
            var station = Create<DisguiseStationDefinition>($"Station_{Name(recipe)}");
            var so = new SerializedObject(station);

            so.FindProperty("<Recipe>k__BackingField").objectReferenceValue = recipe;
            so.ApplyModifiedPropertiesWithoutUndo();

            return station;
        }

        public List<DisguiseStationDefinition> Stations(params DisguiseRecipe[] recipes)
        {
            var stations = new List<DisguiseStationDefinition>(recipes.Length);

            for (int i = 0; i < recipes.Length; i++) 
                stations.Add(Station(recipes[i]));

            return stations;
        }

        public PortRegulationsDefinition Regulations(
            params (CargoTypeDefinition type, PaintDefinition paint, StampDefinition stamp)[] rows)
        {
            var regulations = Create<PortRegulationsDefinition>("PortRegulations");
            var so = new SerializedObject(regulations);

            SerializedProperty list = so.FindProperty("_requirements");
            list.arraySize = rows.Length;

            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty row = list.GetArrayElementAtIndex(i);

                row.FindPropertyRelative("CargoType").objectReferenceValue = rows[i].type;
                row.FindPropertyRelative("Paint").objectReferenceValue = rows[i].paint;
                row.FindPropertyRelative("Stamp").objectReferenceValue = rows[i].stamp;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return regulations;
        }

        public ContractDefinition Contract(
            string displayName,
            CargoTypeDefinition cargo,
            CargoTypeDefinition declaredAs = null,
            int crates = 1,
            int allowedSeizures = 0,
            int reward = 200,
            int penalty = 150,
            int cleanBonus = 50,
            string description = "Описание")
        {
            var contract = Create<ContractDefinition>($"Contract_{displayName}");
            var so = new SerializedObject(contract);

            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.FindProperty("<Description>k__BackingField").stringValue = description;
            so.FindProperty("<Cargo>k__BackingField").objectReferenceValue = cargo;
            so.FindProperty("<DeclaredAs>k__BackingField").objectReferenceValue = declaredAs;
            so.FindProperty("<Crates>k__BackingField").intValue = crates;
            so.FindProperty("<AllowedSeizures>k__BackingField").intValue = allowedSeizures;
            so.FindProperty("<RewardPerCrate>k__BackingField").intValue = reward;
            so.FindProperty("<Penalty>k__BackingField").intValue = penalty;
            so.FindProperty("<CleanBonus>k__BackingField").intValue = cleanBonus;

            so.ApplyModifiedPropertiesWithoutUndo();

            return contract;
        }

        public CargoManifestDefinition Manifest(params string[] keys)
        {
            var manifest = Create<CargoManifestDefinition>("CargoManifest");
            var so = new SerializedObject(manifest);

            SerializedProperty list = so.FindProperty("<CargoKeys>k__BackingField");
            list.arraySize = keys.Length;

            for (int i = 0; i < keys.Length; i++)
                list.GetArrayElementAtIndex(i).stringValue = keys[i];

            so.ApplyModifiedPropertiesWithoutUndo();

            return manifest;
        }

        public InspectorProfile Profile(
            string displayName,
            ClueChecks checks,
            float threshold,
            float paintMismatch = 25f,
            float missingStamp = 20f,
            float wrongStamp = 30f,
            float incompleteDisguise = 20f,
            float declaredContraband = 100f,
            float contentMismatch = 70f)
        {
            var profile = Create<InspectorProfile>($"Profile_{displayName}");
            var lines = Create<InspectorLinesDefinition>($"Lines_{displayName}");

            var so = new SerializedObject(profile);

            so.FindProperty("<DisplayName>k__BackingField").stringValue = displayName;
            so.FindProperty("<Lines>k__BackingField").objectReferenceValue = lines;
            so.FindProperty("<Checks>k__BackingField").intValue = (int)checks;
            so.FindProperty("<SuspicionThreshold>k__BackingField").floatValue = threshold;
            so.FindProperty("<PaintMismatchWeight>k__BackingField").floatValue = paintMismatch;
            so.FindProperty("<MissingStampWeight>k__BackingField").floatValue = missingStamp;
            so.FindProperty("<WrongStampWeight>k__BackingField").floatValue = wrongStamp;
            so.FindProperty("<IncompleteDisguiseWeight>k__BackingField").floatValue = incompleteDisguise;
            so.FindProperty("<DeclaredContrabandWeight>k__BackingField").floatValue = declaredContraband;
            so.FindProperty("<ContentMismatchWeight>k__BackingField").floatValue = contentMismatch;

            so.ApplyModifiedPropertiesWithoutUndo();

            return profile;
        }

        public CargoRegistryDefinition Registry(
            params (string key, CargoTypeDefinition type)[] entries)
        {
            var registry = Create<CargoRegistryDefinition>("CargoRegistry");
            var so = new SerializedObject(registry);

            SerializedProperty list = so.FindProperty("_cargo");

            list.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty row = list.GetArrayElementAtIndex(i);

                row.FindPropertyRelative("Key").stringValue = entries[i].key;
                row.FindPropertyRelative("Type").objectReferenceValue = entries[i].type;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return registry;
        }

        private T Create<T>(string name) where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();

            instance.name = name;
            _created.Add(instance);

            return instance;
        }

        private static string Name(Object asset) => asset != null ? asset.name : "none";
    }
}
