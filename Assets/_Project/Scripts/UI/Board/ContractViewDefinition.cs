using UnityEngine;

namespace CrateExpectations.UI
{
    /// <summary>Как листок заказа лежит в руках: поза, тайминги, запаздывание при повороте камеры.</summary>
    [CreateAssetMenu(
        fileName = "ContractView",
        menuName = "CrateExpectations/Contracts/Contract View")]
    public sealed class ContractViewDefinition : ScriptableObject
    {
        [Header("Поза перед камерой")]
        [Tooltip("Куда листок встаёт относительно камеры, когда поднят")]
        [field: SerializeField] public Vector3 HeldPosition { get; private set; } = new(0.16f, -0.10f, 0.42f);

        [Tooltip("Наклон листка в руках")]
        [field: SerializeField] public Vector3 HeldEulerAngles { get; private set; } = new(0f, 180f, -7f);

        [Tooltip("Масштаб листка в руках")]
        [field: SerializeField][Min(0.0001f)] public float HeldScale { get; private set; } = 0.35f;

        [Header("Появление")]
        [Tooltip("Откуда листок выезжает и куда уезжает, относительно поднятой позы")]
        [field: SerializeField] public Vector3 StowedOffset { get; private set; } = new(0.05f, -0.4f, -0.1f);

        [Tooltip("Доворот в убранном положении")]
        [field: SerializeField] public Vector3 StowedEulerOffset { get; private set; } = new(-35f, 0f, 12f);

        [Tooltip("Сколько листок поднимается")]
        [field: SerializeField][Min(0f)] public float RaiseSeconds { get; private set; } = 0.18f;

        [Tooltip("Сколько листок убирается")]
        [field: SerializeField][Min(0f)] public float LowerSeconds { get; private set; } = 0.14f;

        [Header("Запаздывание при повороте камеры")]
        [Tooltip("Больше - листок жёстче держится за камеру. 0 отключает запаздывание")]
        [field: SerializeField][Min(0f)] public float SwayStiffness { get; private set; } = 11f;

        [Tooltip("Насколько сильно листок отстаёт от камеры, градусы")]
        [field: SerializeField][Min(0f)] public float MaxSwayDegrees { get; private set; } = 7f;

        [Header("Слой")]
        [Tooltip("Слой FirstPersonItem: его рисует только наложенная камера, основная не видит")]
        [field: SerializeField] public int FirstPersonLayer { get; private set; }
    }
}
