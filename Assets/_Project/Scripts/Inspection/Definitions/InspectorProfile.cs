using UnityEngine;

namespace CrateExpectations.Inspection
{
    [CreateAssetMenu(
        fileName = "InspectorProfile",
        menuName = "CrateExpectations/Inspection/Inspector Profile")]
    public sealed class InspectorProfile : ScriptableObject
    {
        [Tooltip("Имя инспектора")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Инспектор";

        [Tooltip("Как он себя ведёт - заметка для дизайнера")]
        [field: SerializeField, TextArea(2, 4)] public string Description { get; private set; } = string.Empty;

        [Tooltip("Голос инспектора: реплики осмотра и вердикта")]
        [field: SerializeField] public InspectorLinesDefinition Lines { get; private set; }

        [Header("Что проверяет")]
        [Tooltip("Набор выполняемых проверок. Чего здесь нет, того он не заметит никогда")]
        [field: SerializeField] public ClueChecks Checks { get; private set; } = ClueChecks.All;

        [Header("Веса улик")]
        [Tooltip("Заявлено то, что везти нельзя")]
        [field: SerializeField] public float DeclaredContrabandWeight { get; private set; } = 100f;

        [Tooltip("Внутри не то, что заявлено")]
        [field: SerializeField] public float ContentMismatchWeight { get; private set; } = 70f;

        [Tooltip("Окраска не та, что положена заявленному содержимому")]
        [field: SerializeField] public float PaintMismatchWeight { get; private set; } = 25f;

        [Tooltip("Обязательной пломбы нет")]
        [field: SerializeField] public float MissingStampWeight { get; private set; } = 20f;

        [Tooltip("Пломба не та")]
        [field: SerializeField] public float WrongStampWeight { get; private set; } = 30f;

        [Tooltip("Обязательная окраска не нанесена вовсе")]
        [field: SerializeField] public float IncompleteDisguiseWeight { get; private set; } = 20f;

        [Header("Решение")]
        [Tooltip("С какого уровня подозрения груз задерживается")]
        [field: SerializeField] public float SuspicionThreshold { get; private set; } = 40f;

        [Tooltip("Шанс проморгать отдельную улику, 0..1")]
        [Range(0f, 1f)]
        [field: SerializeField] public float OverlookChance { get; private set; }

        public InspectionPolicy Policy => new(
            Checks,
            new ClueWeights(
                DeclaredContrabandWeight,
                ContentMismatchWeight,
                PaintMismatchWeight,
                MissingStampWeight,
                WrongStampWeight,
                IncompleteDisguiseWeight),
            SuspicionThreshold,
            OverlookChance);
    }
}
