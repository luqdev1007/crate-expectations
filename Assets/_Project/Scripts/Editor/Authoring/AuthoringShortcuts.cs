using UnityEditor;
using UnityEngine;

namespace CrateExpectations.EditorTools
{
    /// <summary>
    /// Общий механизм пунктов меню "выдели мне вот тот ассет". Нужен потому, что ассеты
    /// подбора кадра правят в play mode: лезть в этот момент по папкам проекта неудобно,
    /// а сами ассеты - обычные ScriptableObject, поэтому правки слайдеров переживают
    /// выход из play mode.
    /// <para>
    /// Ассет ищется по типу, а не по пути: переложат папку - пункт меню продолжит работать.
    /// </para>
    /// </summary>
    internal static class AuthoringShortcuts
    {
        [MenuItem("Tools/Crate Expectations/Кадрирование вьюмодели")]
        private static void SelectFraming() =>
            Select("t:ViewModelFramingDefinition", "ViewModelFramingDefinition");

        [MenuItem("Tools/Crate Expectations/Посадка оружия")]
        private static void SelectWeapon() => Select("t:WeaponDefinition", "WeaponDefinition");

        [MenuItem("Tools/Crate Expectations/Дуга взмаха")]
        private static void SelectSwing() => Select("t:SwingDefinition", "SwingDefinition");

        /// <summary>
        /// Выделяет первый найденный ассет типа <paramref name="typeFilter"/>.
        /// Ассетов каждого типа в проекте пока по одному; станет больше - пункт меню
        /// превратится в подменю, а искать по-прежнему будет по типу
        /// </summary>
        private static void Select(string typeFilter, string humanName)
        {
            string[] guids = AssetDatabase.FindAssets(typeFilter);

            if (guids.Length == 0)
            {
                Debug.LogWarning($"Ассет {humanName} в проекте не найден.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
