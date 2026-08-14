using CrateExpectations.Combat;
using CrateExpectations.Combat.Events;
using CrateExpectations.Core.Events;
using CrateExpectations.Core.Services;
using CrateExpectations.Core.Timing;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Отдача от удара, полученного игроком: короткая заморозка кадра и искры в точке
    /// касания. То же, что <c>MeleeHitStop</c> и <c>MeleeImpactEffects</c> делают
    /// для ударов игрока, - но с другой стороны.
    /// <para>
    /// <b>Отдельный компонент, а существующие не тронуты ни строкой.</b> Те подписаны
    /// на <c>PlayerMeleeAttack.Hit</c> - событие конкретного компонента конкретного
    /// объекта, - и научить их слышать ещё и стражника значило бы дать им второй
    /// источник и второй набор условий. Здесь источник другой (шина), и правильнее
    /// завести второго слушателя, чем усложнять первого.
    /// </para>
    /// <para>
    /// <b>Один на сцену, а не по одному на стражника.</b> Слушателю шины незачем
    /// размножаться: событие всё равно приходит всем. Плюс <see cref="ImpactEffectPool"/> -
    /// объект сцены, а префаб сценовых ссылок не хранит, так что компонент на
    /// <c>Guard.prefab</c> остался бы с пустым полем.
    /// </para>
    /// </summary>
    public sealed class GuardAttackFeedback : MonoBehaviour
    {
        [Tooltip("Кто выдаёт готовые системы частиц. Тот же пул, что показывает удары игрока")]
        [SerializeField] private ImpactEffectPool _pool;

        [Tooltip("Чем бьют стражники. Нужен не как оружие, а как источник чисел отдачи: " +
                 "длительность и глубина заморозки лежат в его ImpactFeedbackDefinition")]
        [SerializeField] private WeaponDefinition _guardWeapon;

        private IEventBus _bus;
        private IHitStopService _hitStop;
        private IPlayerTarget _player;

        [Inject]
        public void Construct(IEventBus bus, IHitStopService hitStop, IPlayerTarget player)
        {
            _bus = bus;
            _hitStop = hitStop;
            _player = player;
        }

        private void Awake()
        {
            if (_pool != null && _guardWeapon != null)
                return;

            Debug.LogError($"Отдаче удара стражника '{name}' не назначен пул эффектов " +
                           "или оружие - попадание по игроку будет беззвучным и без искр.", this);
            enabled = false;
        }

        /// <summary>
        /// Подписка в <c>Start</c>, а не в <c>OnEnable</c>, - тот же приём, что
        /// у <c>BalanceView</c>: <c>OnEnable</c> у компонента сцены случается раньше,
        /// чем <c>LifetimeScope</c> успевает раздать зависимости, и шины там ещё нет
        /// </summary>
        private void Start() => _bus?.Subscribe<DamageTaken>(OnDamageTaken);

        private void OnDestroy() => _bus?.Unsubscribe<DamageTaken>(OnDamageTaken);

        /// <summary>
        /// Отбираем удары по игроку. Признак «ударил стражник» отдельным полем в событии
        /// НЕ заводится, и это осознанно: <c>Combat</c> знает только, что пришло попадание
        /// с такими-то числами, а «кто ударил» - понятие вне его границы (фаза D §4.1).
        /// Различить хватает того, что стражники друг друга не бьют (маска удара - только
        /// слой <c>Player</c>), поэтому любое попадание ПО ИГРОКУ сейчас нанесено стражником
        /// </summary>
        private void OnDamageTaken(DamageTaken damage)
        {
            if (damage.Victim == null || _player == null || !_player.Exists)
                return;

            if (damage.Victim.transform != _player.Transform)
                return;

            ImpactFeedbackDefinition feedback = _guardWeapon.Feedback;

            if (feedback == null)
                return;

            // Множителя приёма здесь нет, в отличие от игрока: у стражника один удар
            // без вариантов силы, и умножать не на что. Появится вариативность -
            // появится и множитель, ровно там же, где он у игрока
            _hitStop?.Freeze(feedback.HitStopDuration, feedback.HitStopScale);

            // Якорь - сам игрок: он подвижен, и след, оставленный в мировых координатах,
            // повис бы в воздухе на том месте, где игрока задели. То же правило, что
            // разбиралось в фазе D блок 10
            _pool.Play(damage.Hit.Point, damage.Hit.Normal, damage.Victim.transform);
        }
    }
}
