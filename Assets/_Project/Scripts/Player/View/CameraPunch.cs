using CrateExpectations.Combat;
using CrateExpectations.Player.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Тычок камеры на попадании - аддитивный слой поверх Cinemachine, а не присваивание
    /// углов.
    /// <para>
    /// Это принципиально. Углами камеру задаёт связка "мышь -> Cinemachine", и любое
    /// присваивание поверх неё либо будет затёрто в тот же кадр, либо затрёт ввод игрока
    /// и уведёт прицел. Расширение вклинивается в конце конвейера камеры и КЛАДЁТ СВОЙ
    /// ДОВОРОТ В <c>OrientationCorrection</c> - поле, которое для того и заведено:
    /// прицел остаётся ровно там, куда его привёл игрок, а сверху лежит затухающая добавка.
    /// </para>
    /// <para>
    /// Направление тычка задано в ассете отдачи против дуги удара: клинок идёт вниз-влево,
    /// камера отдаёт вверх-вправо. Совпади они по знаку - удар читался бы как второй замах.
    /// </para>
    /// </summary>
    public sealed class CameraPunch : CinemachineExtension
    {
        [Tooltip("Чьи попадания слушаем")]
        [SerializeField] private PlayerMeleeAttack _attack;

        [Tooltip("Откуда берутся амплитуда, длительность и кривая затухания")]
        [SerializeField] private PlayerWeaponController _weapon;

        private ImpactFeedbackDefinition _feedback;
        private float _elapsed;
        private float _duration;

        // Множитель приёма запоминается вместе с остальным: к моменту, когда тычок
        // доигрывает, удар мог уже смениться следующим, и сила поехала бы посреди отдачи
        private float _scale = 1f;

        private bool _punching;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_attack == null || _weapon == null)
            {
                Debug.LogError($"Тычку камеры '{name}' не назначен удар или оружие.", this);
                enabled = false;
                return;
            }

            _attack.Hit += OnHit;
        }

        private void OnDisable()
        {
            if (_attack != null)
                _attack.Hit -= OnHit;

            _punching = false;
        }

        private void OnHit(HitInfo hit, Collider collider)
        {
            ImpactFeedbackDefinition feedback = _weapon.Weapon == null ? null : _weapon.Weapon.Feedback;

            if (feedback == null || feedback.CameraPunchDuration <= 0f)
                return;

            // Второе попадание тем же взмахом перезапускает тычок с нуля, а не складывается
            // с текущим: складывать - значит удваивать амплитуду на каждой лишней цели
            // и получать рывок тем сильнее, чем гуще стояли ящики
            AttackDefinition attack = _weapon.CurrentAttack;

            _feedback = feedback;
            _scale = attack == null ? 1f : attack.CameraPunchMultiplier;
            _duration = feedback.CameraPunchDuration;
            _elapsed = 0f;
            _punching = true;
        }

        /// <summary>
        /// Время копим скалированное, а не реальное: тычок обязан замереть вместе со всем
        /// кадром на время заморозки. Реальное время прокрутило бы его прямо во время стопа,
        /// и к моменту разморозки от удара не осталось бы ничего
        /// </summary>
        private void Update()
        {
            if (!_punching)
                return;

            _elapsed += UnityEngine.Time.deltaTime;

            if (_elapsed >= _duration)
                _punching = false;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState state, float deltaTime)
        {
            // Только в самом конце конвейера: доворот должен лечь поверх всего,
            // что камера уже решила про своё положение
            if (stage != CinemachineCore.Stage.Finalize || !_punching || _feedback == null)
                return;

            float t = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
            float falloff = _feedback.EvaluateCameraPunch(t);
            Vector2 angles = _feedback.CameraPunchAngles * (falloff * _scale);

            state.OrientationCorrection *= Quaternion.Euler(angles.x, angles.y, 0f);
        }
    }
}
