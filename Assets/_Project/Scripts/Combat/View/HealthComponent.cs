using System;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Здоровье на сцене: тонкая обёртка над <see cref="HealthState"/>. Вся арифметика
    /// живёт в POCO-ядре, здесь только приём удара и оповещение соседей - тот же расклад,
    /// что у <c>GuardAI</c> с <c>GuardBrain</c>.
    /// <para>
    /// Реализует <see cref="IDamageable"/>, поэтому попадания приходят сюда сами:
    /// <c>PlayerMeleeAttack</c> ищет получателя через <c>GetComponentInParent</c>,
    /// и компонент на корне ловит удары по любому дочернему коллайдеру. Ничего
    /// специально подключать не требуется.
    /// </para>
    /// <para>
    /// События здесь ЛОКАЛЬНЫЕ, C#-шные, и это не дубликат шины: они для соседних
    /// компонентов того же объекта (хитреакт стражника, смерть), которым нужна прямая
    /// ссылка и нулевая задержка. Для систем без ссылки на этот объект - будущих
    /// свидетелей и статистики - то же самое уходит в <c>IEventBus</c> отдельно
    /// </para>
    /// </summary>
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [Tooltip("Сколько здоровья. Ассет общий на тип существа, не на экземпляр")]
        [SerializeField] private HealthDefinition _definition;

        private HealthState _health;

        /// <summary>
        /// Получен урон. Несёт и результат, и сам удар: реакция выбирается по
        /// <see cref="HitInfo.Tier"/>, а не по тому, сколько сняли
        /// </summary>
        public event Action<DamageResult, HitInfo> Damaged;

        /// <summary>Здоровье кончилось. Поднимается ровно один раз за жизнь</summary>
        public event Action Died;

        /// <summary>Сколько здоровья осталось</summary>
        public float CurrentHp => _health == null ? 0f : _health.CurrentHp;

        /// <summary>Полное здоровье</summary>
        public float MaxHp => _health == null ? 0f : _health.MaxHp;

        /// <summary>Здоровье кончилось</summary>
        public bool IsDead => _health != null && _health.IsDead;

        private void Awake()
        {
            if (_definition == null)
            {
                Debug.LogError($"Здоровью '{name}' не назначен HealthDefinition - " +
                               "существо будет неуязвимым.", this);
                enabled = false;
                return;
            }

            _health = new HealthState(_definition.MaxHp);
        }

        /// <inheritdoc />
        public void TakeHit(HitInfo hit)
        {
            // Компонент выключен проверкой ссылок - удар просто не считается.
            // Молчаливая неуязвимость лучше, чем NullReferenceException на каждый взмах:
            // об отсутствии ассета уже сказано ошибкой в Awake, повторять её незачем
            if (_health == null)
                return;

            // По мёртвому не бьют - и наружу об этом не сообщают тоже. Иначе добивающий
            // взмах, задевший труп повторно, поднял бы хитреакт у уже упавшего тела
            if (_health.IsDead)
                return;

            DamageResult result = _health.ApplyDamage(hit.Damage, hit.Tier);

            Damaged?.Invoke(result, hit);

            if (result.Died)
                Died?.Invoke();
        }
    }
}
