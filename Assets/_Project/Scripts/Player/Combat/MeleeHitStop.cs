using CrateExpectations.Combat;
using CrateExpectations.Core.Timing;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Player.Combat
{
    /// <summary>
    /// Просит заморозку на попадании. Вся его работа - перевести событие удара в один
    /// вызов сервиса; <c>Time.timeScale</c> он не трогает и трогать не должен -
    /// см. <see cref="IHitStopService"/>.
    /// <para>
    /// Отдельный компонент, а не строчка внутри <see cref="PlayerMeleeAttack"/>: детекция
    /// не обязана знать, что от её событий вообще что-то происходит с временем. Снимите
    /// этот компонент - удары продолжат считаться, просто без задержки.
    /// </para>
    /// </summary>
    public sealed class MeleeHitStop : MonoBehaviour
    {
        [Tooltip("Чьи попадания слушаем")]
        [SerializeField] private PlayerMeleeAttack _attack;

        [Tooltip("Откуда берётся длительность заморозки")]
        [SerializeField] private PlayerWeaponController _weapon;

        private IHitStopService _hitStop;

        [Inject]
        public void Construct(IHitStopService hitStop) => _hitStop = hitStop;

        private void Awake()
        {
            if (_attack == null || _weapon == null)
            {
                Debug.LogError($"Заморозке '{name}' не назначен удар или оружие.", this);
                enabled = false;
            }
        }

        private void OnEnable() => _attack.Hit += OnHit;

        private void OnDisable() => _attack.Hit -= OnHit;

        private void OnHit(HitInfo hit, Collider collider)
        {
            ImpactFeedbackDefinition feedback = _weapon.Weapon == null ? null : _weapon.Weapon.Feedback;

            if (feedback == null || _hitStop == null)
                return;

            // Несколько целей одним взмахом дадут несколько вызовов. Складывать их
            // не надо, и сервис этого и не делает - он лишь двигает конец заморозки
            _hitStop.Freeze(feedback.HitStopDuration, feedback.HitStopScale);
        }
    }
}
