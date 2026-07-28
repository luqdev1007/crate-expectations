using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Как инспектор работает: с какой скоростью ходит, где останавливается и из каких шагов
    /// складывается осмотр. Это процедура и тайминги, общие для всех инспекторов порта.
    /// Строгость живёт в <see cref="InspectorProfile"/>, голос - в
    /// <see cref="InspectorLinesDefinition"/>: три разных вопроса - три разных ассета.
    /// </summary>
    [CreateAssetMenu(
        fileName = "InspectionDefinition",
        menuName = "CrateExpectations/Inspection/Inspection Definition")]
    public sealed class InspectionDefinition : ScriptableObject
    {
        [Header("Перемещение")]
        [field: SerializeField] public float WalkSpeed { get; private set; } = 1.6f;
        [Tooltip("Скорость разворота, град/с")]
        [field: SerializeField] public float TurnSpeed { get; private set; } = 270f;
        [Tooltip("Радиус, в котором точка назначения считается достигнутой, м")]
        [field: SerializeField] public float StopDistance { get; private set; } = 0.12f;

        [Header("Патруль")]
        [field: SerializeField][Min(0f)] public float PatrolSpeed { get; private set; } = 0.45f;
        [field: SerializeField][Min(0f)] public float PatrolDwellSeconds { get; private set; } = 2.5f;

        [Header("Реакция на подход")]
        [field: SerializeField][Min(0f)] public float NoticeRadius { get; private set; } = 4.5f;
        [field: SerializeField] public LayerMask CargoMask { get; private set; } = ~0;
        [field: SerializeField][Min(0f)] public float NoticeHoldSeconds { get; private set; } = 2f;
        [field: SerializeField][Min(0f)] public float NoticeCooldownSeconds { get; private set; } = 8f;

        [Header("Осмотр")]
        [SerializeField] private ExamineStep[] _steps = Array.Empty<ExamineStep>();
        [Tooltip("Пауза после чистого шага, с")]
        [field: SerializeField] public float StepPauseSeconds { get; private set; } = 0.3f;
        [Tooltip("Сколько инспектор задерживается на найденной улике, с")]
        [field: SerializeField] public float ClueReactionSeconds { get; private set; } = 1.8f;

        [Header("Вердикт")]
        [field: SerializeField] public float VerdictDelaySeconds { get; private set; } = 0.8f;
        [field: SerializeField][Min(0f)] public float VerdictHoldSeconds { get; private set; } = 6f;
        [field: SerializeField] public float CargoHandoffSeconds { get; private set; } = 6f;

        [Header("Пузырь реплик")]
        [Tooltip("Сколько плашка над головой проявляется")]
        [field: SerializeField][Min(0f)] public float SpeechFadeInSeconds { get; private set; } = 0.15f;

        [Tooltip("Сколько плашка над головой гаснет")]
        [field: SerializeField][Min(0f)] public float SpeechFadeOutSeconds { get; private set; } = 0.3f;

        [Header("Отладка")]
        [Tooltip("Seed невнимательности инспектора. 0 - случайный при каждом запуске")]
        [field: SerializeField] public int OverlookSeed { get; private set; }

        /// <summary>Шаги осмотра по порядку</summary>
        public IReadOnlyList<ExamineStep> Steps => _steps;
    }
}
