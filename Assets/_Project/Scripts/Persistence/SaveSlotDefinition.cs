using UnityEngine;

namespace CrateExpectations.Persistence
{
    /// <summary>
    /// Куда игра сохраняется. Ключ живёт в ассете, а не строкой по коду: слотов со временем
    /// станет больше одного, и переименование ключа не должно быть правкой в C#.
    ///
    /// <para>Что именно стоит за ключом - файл, ключ в облаке Steam или строка в БД -
    /// не знает ни этот ассет, ни игровой код: это забота <c>ISaveService</c>.</para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "SaveSlot",
        menuName = "CrateExpectations/Persistence/Save Slot")]
    public sealed class SaveSlotDefinition : ScriptableObject
    {
        [Tooltip("Ключ слота. Для локального сохранения станет именем файла")]
        [field: SerializeField] public string Key { get; private set; } = "crate-expectations-save";

        [Tooltip("Как слот называется для игрока")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Быстрое сохранение";
    }
}
