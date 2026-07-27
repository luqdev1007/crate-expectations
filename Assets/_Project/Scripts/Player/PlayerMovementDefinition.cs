using UnityEngine;

namespace CrateExpectations.Player
{
    [CreateAssetMenu(
        fileName = "PlayerMovementDefinition",
        menuName = "CrateExpectations/Player/Movement Definition")]
    public sealed class PlayerMovementDefinition : ScriptableObject
    {
        [Header("Движение")]
        [field: SerializeField] public float MoveSpeed { get; private set; } = 5f;
        [field: SerializeField] public float Acceleration { get; private set; } = 40f;

        [Header("Прыжок")]
        [field: SerializeField] public float JumpHeight { get; private set; } = 1.2f;

        [Header("Обзор")]
        [field: SerializeField] public float LookSensitivity { get; private set; } = 0.1f;
        [field: SerializeField] public float PitchMin { get; private set; } = -85f;
        [field: SerializeField] public float PitchMax { get; private set; } = 85f;

        [Header("Скольжение вдоль стен")]
        [field: SerializeField] public float WallNormalMaxY { get; private set; } = 0.4f;

        [Header("Проверка земли")]
        [field: SerializeField] public float GroundCheckRadius { get; private set; } = 0.3f;
        [field: SerializeField] public float GroundCheckDistance { get; private set; } = 0.25f;
        [field: SerializeField] public LayerMask GroundMask { get; private set; } = ~0;
    }
}
