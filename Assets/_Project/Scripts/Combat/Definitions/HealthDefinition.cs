using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Сколько у существа здоровья. Ассет, а не поле на префабе, по той же причине,
    /// что и у <see cref="AttackDefinition"/>: живучесть - предмет баланса, и править
    /// её надо в одном месте, глядя на соседние числа, а не по префабам поодиночке
    /// </summary>
    [CreateAssetMenu(
        fileName = "HealthDefinition",
        menuName = "CrateExpectations/Combat/Health Definition")]
    public sealed class HealthDefinition : ScriptableObject
    {
        [Tooltip("Полное здоровье. С уроном приёмов соотносится напрямую: сабля " +
                 "снимает 1 за лёгкий удар и 2 за тяжёлый")]
        [field: SerializeField][Min(1f)] public float MaxHp { get; private set; } = 100f;
    }
}
