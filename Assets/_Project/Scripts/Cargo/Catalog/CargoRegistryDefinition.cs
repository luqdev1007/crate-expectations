using System;
using UnityEngine;

namespace CrateExpectations.Cargo.Catalog
{
    [CreateAssetMenu(
        fileName = "CargoRegistry",
        menuName = "CrateExpectations/Cargo/Cargo Registry")]
    public sealed class CargoRegistryDefinition : ScriptableObject
    {
        /// <summary>Тип груза и ключ, по которому его отдаёт каталог</summary>
        [Serializable]
        public struct CargoEntry
        {
            [Tooltip("Ключ контента: тот же, что в манифесте дока")]
            public string Key;

            [Tooltip("Тип груза, который пдо этим ключом лежит")]
            public CargoTypeDefinition Type;
        }

        [Tooltip("Все типы груза, которые могут оказаться в сохранении")]
        [SerializeField] private CargoEntry[] _cargo = Array.Empty<CargoEntry>();

        [Tooltip("Все варианты окраски (опознаются по имени ассета)")]
        [SerializeField] private PaintDefinition[] _paints = Array.Empty<PaintDefinition>();

        [Tooltip("Все виды пломб (опознаются по имени ассета)")]
        [SerializeField] private StampDefinition[] _stamps = Array.Empty<StampDefinition>();

        /// <summary>Ключ кнтента для типа груза. Если пусто, то значит тип в реестре не заведён</summary>
        public string KeyOf(CargoTypeDefinition type)
        {
            if (type == null)
                return string.Empty;

            for (int i = 0; i < _cargo.Length; i++)
                if (_cargo[i].Type == type) 
                    return _cargo[i].Key;

            Debug.LogWarning("[Реестр груза] Тип \"{type.name}\" не заведён в реестре, ящик с ним не попадёт в сохранение", this);

            return string.Empty;
        }

        /// <summary>Тип груза по ключу контента</summary>
        public CargoTypeDefinition CargoByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) 
                return null;

            for (int i = 0; i < _cargo.Length; i++)
                if (_cargo[i].Key == key) 
                    return _cargo[i].Type;

            return null;
        }

        /// <summary>Окраска по имени ассета, если null, то ящик не крашен</summary>
        public PaintDefinition PaintById(string id) => ById(_paints, id);

        /// <summary>Пломба по имени ассета, если null, то пломбы нет</summary>
        public StampDefinition StampById(string id) => ById(_stamps, id);

        /// <summary>Идентификатор ассета для файла сохранения, если пусто, то ссылки нет</summary>
        public static string IdOf(ScriptableObject asset) => asset != null ? asset.name : string.Empty;

        private static T ById<T>(T[] assets, string id) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(id)) 
                return null;

            for (int i = 0; i < assets.Length; i++)
                if (assets[i] != null && assets[i].name == id) 
                    return assets[i];

            return null;
        }
    }
}
