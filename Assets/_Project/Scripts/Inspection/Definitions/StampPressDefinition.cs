using UnityEngine;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Ритм и материал печати: чем инспектор бьёт по экрану и как быстро.
    /// Весь эффект настраивается одним ассетом - вид сцены знает только, где стоит оснастка
    /// и куда лечь оттиску
    /// </summary>
    [CreateAssetMenu(
        fileName = "StampPress",
        menuName = "CrateExpectations/Inspection/Stamp Press")]
    public sealed class StampPressDefinition : ScriptableObject
    {
        [Header("Оттиски")]
        [Tooltip("Печать, которую инспектор ставит пропущенному грузу")]
        [field: SerializeField] public Sprite Approved { get; private set; }

        [Tooltip("Печать, которую инспектор ставит задержанному грузу")]
        [field: SerializeField] public Sprite Rejected { get; private set; }

        [Header("Ход оснастки")]
        [Tooltip("Насколько оснастка поднята над точкой удара в замахе, м")]
        [field: SerializeField][Min(0f)] public float ReadyLift { get; private set; } = 0.05f;

        [Tooltip("Насколько она поднята, когда её не видно: с этой высоты она входит в кадр, м")]
        [field: SerializeField][Min(0.05f)] public float HiddenLift { get; private set; } = 0.4f;

        [Tooltip("Сколько секунд оснастка въезжает в кадр и замирает в замахе")]
        [field: SerializeField][Min(0f)] public float EnterSeconds { get; private set; } = 0.22f;

        [Tooltip("Сам удар: коротко, иначе печать выглядит вялой")]
        [field: SerializeField][Min(0.01f)] public float StrikeSeconds { get; private set; } = 0.07f;

        [Tooltip("Сколько секунд оснастка стоит прижатой, прежде чем уйти")]
        [field: SerializeField][Min(0f)] public float PressHoldSeconds { get; private set; } = 0.16f;

        [Tooltip("Сколько секунд оснастка уходит из кадра")]
        [field: SerializeField][Min(0.01f)] public float LeaveSeconds { get; private set; } = 0.35f;

        [Header("Оттиск")]
        [Tooltip("Сколько секунд оттиск проступает после удара")]
        [field: SerializeField][Min(0.01f)] public float ImprintFadeInSeconds { get; private set; } = 0.18f;

        [Tooltip("С какого размера оттиск садится на место: чуть крупнее - и удар читается")]
        [field: SerializeField][Min(1f)] public float ImprintPunchScale { get; private set; } = 1.25f;

        [Tooltip("Сколько секунд оттиск держится на экране в полную силу")]
        [field: SerializeField][Min(0f)] public float ImprintHoldSeconds { get; private set; } = 2.2f;

        [Tooltip("Сколько секунд оттиск тает")]
        [field: SerializeField][Min(0.01f)] public float ImprintFadeOutSeconds { get; private set; } = 0.5f;

        /// <summary>Оттиск по исходу досмотра</summary>
        public Sprite StampFor(bool bust) => bust ? Rejected : Approved;
    }
}
