using CrateExpectations.Combat;
using CrateExpectations.Player.Combat;
using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Отдача вьюмодели на попадании: короткий толчок рук назад и возврат по кривой.
    /// Ещё один <see cref="IViewModelLayer"/> - слоистость <see cref="ViewModelRig"/>
    /// для того и сделана, чтобы новый вид движения добавлялся компонентом, без правок
    /// в риге и в соседних слоях.
    /// <para>
    /// Толчок идёт строго назад, вдоль оси взгляда, и не крутит модель. Поворот здесь
    /// был бы лишним: дугу уже рисует клип взмаха, и второй поворот поверх неё сбивает
    /// её форму. Смещение же читается как упор в цель - именно то, чего не хватает
    /// удару, который проходит сквозь ящик без сопротивления.
    /// </para>
    /// </summary>
    public sealed class ViewModelImpact : MonoBehaviour, IViewModelLayer
    {
        [Tooltip("Чьи попадания слушаем")]
        [SerializeField] private PlayerMeleeAttack _attack;

        [Tooltip("Откуда берутся глубина отдачи, длительность и кривая возврата")]
        [SerializeField] private PlayerWeaponController _weapon;

        private ImpactFeedbackDefinition _feedback;
        private float _elapsed;
        private float _duration;
        private bool _kicking;

        private void Awake()
        {
            if (_attack == null || _weapon == null)
            {
                Debug.LogError($"Отдаче вьюмодели '{name}' не назначен удар или оружие.", this);
                enabled = false;
            }
        }

        private void OnEnable() => _attack.Hit += OnHit;

        private void OnDisable() => _attack.Hit -= OnHit;

        private void OnHit(HitInfo hit, Collider collider)
        {
            ImpactFeedbackDefinition feedback = _weapon.Weapon == null ? null : _weapon.Weapon.Feedback;

            if (feedback == null || feedback.ViewModelKickDuration <= 0f)
                return;

            _feedback = feedback;
            _duration = feedback.ViewModelKickDuration;
            _elapsed = 0f;
            _kicking = true;
        }

        /// <summary>
        /// Время копим здесь, а не в <see cref="Evaluate"/>: слой опрашивает риг из
        /// своего <c>LateUpdate</c>, и класть накопление состояния в опрос значит
        /// завязать результат на то, сколько раз за кадр слой спросили
        /// </summary>
        private void Update()
        {
            if (!_kicking)
                return;

            _elapsed += UnityEngine.Time.deltaTime;

            if (_elapsed >= _duration)
                _kicking = false;
        }

        /// <inheritdoc />
        public ViewModelOffset Evaluate()
        {
            // Слой остаётся в списке рига, даже когда молчит: порядок наложения задаётся
            // порядком компонентов, и выпадение одного из них сдвинуло бы остальные
            if (!_kicking || _feedback == null)
                return ViewModelOffset.None;

            float t = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
            float falloff = _feedback.EvaluateViewModelKick(t);

            // Пивот нулевой: слой не вращает, а только сдвигает, и точка вращения ему
            // не нужна. Минус по Z - назад от объектива
            return new ViewModelOffset(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(0f, 0f, -_feedback.ViewModelKick * falloff));
        }
    }
}
