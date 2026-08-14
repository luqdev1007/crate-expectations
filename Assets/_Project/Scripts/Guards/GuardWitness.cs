using CrateExpectations.Combat;
using CrateExpectations.Combat.Events;
using CrateExpectations.Core.Events;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Органы чувств стражника: заметил ли он, что рядом бьют его товарища.
    /// Геометрию считает <see cref="PerceptionCheck"/>, физику - этот компонент,
    /// а решение, что делать с замеченным, по-прежнему принимает
    /// <see cref="GuardBrain"/>: свидетельство только взводит уже существующий
    /// <see cref="GuardAI.IsAggro"/> и новых намерений не заводит.
    /// <para>
    /// <b>Восприятие событийное, а не покадровое.</b> Проверка идёт РОВНО ОДИН РАЗ,
    /// в момент чужого попадания, - а не каждый кадр по всем стражникам порта.
    /// Разница не в стиле: постоянный тик означал бы N стражников × 60 кадров
    /// проверок ради события, которое случается раз в минуту.
    /// </para>
    /// <para>
    /// <b>Отдельный компонент от <see cref="GuardAlertResponder"/></b>, хотя оба
    /// кончаются одной строчкой: этот отвечает на вопрос «заметил ли сам»,
    /// тот - «докричались ли до него». Первый смотрит и слушает, второй просто
    /// стоит в радиусе, и снять с префаба их надо уметь по отдельности.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(GuardAI))]
    public sealed class GuardWitness : MonoBehaviour
    {
        [Tooltip("Дальности, конус, рост и маска препятствий. Один ассет на всех стражников")]
        [SerializeField] private GuardPerceptionDefinition _perception;

        private GuardAI _guard;
        private IEventBus _bus;

        [Inject]
        public void Construct(IEventBus bus) => _bus = bus;

        private void Awake()
        {
            _guard = GetComponent<GuardAI>();

            if (_perception != null)
                return;

            Debug.LogError($"Свидетелю '{name}' не назначен GuardPerceptionDefinition - " +
                           "замечать чужую драку будет нечем.", this);
            enabled = false;
        }

        /// <summary>
        /// Подписка в <c>Start</c>, а не в <c>OnEnable</c>, - тот же приём, что
        /// у <c>GuardAttackFeedback</c> и <c>BalanceView</c>: инъекция стражникам
        /// раздаётся из <c>RegisterBuildCallback</c> скоупа, то есть в чужом
        /// <c>Awake</c>, и в <c>OnEnable</c> шины может ещё не быть
        /// </summary>
        private void Start() => _bus?.Subscribe<DamageTaken>(OnDamageTaken);

        private void OnDestroy() => _bus?.Unsubscribe<DamageTaken>(OnDamageTaken);

        /// <summary>
        /// Отбираем чужие попадания по стражникам. Признака «кто ударил» в событии нет
        /// и не заводится (фаза D §4.1), но он и не нужен: стражники друг друга не бьют -
        /// маска их удара это только слой <c>Player</c>, - поэтому любое попадание
        /// ПО СТРАЖНИКУ нанесено игроком
        /// </summary>
        private void OnDamageTaken(DamageTaken damage)
        {
            // Уже взведён - считать нечего. Проверка первой: она самая дешёвая,
            // а в разгар драки событий идёт больше всего именно тогда, когда
            // все окрестные стражники уже сбежались
            if (_guard.IsAggro)
                return;

            HealthComponent victim = damage.Victim;

            if (victim == null)
                return;

            // Свой урон сюда не относится: его стражник замечает собственной шкурой,
            // прямой подпиской на своё здоровье. Сравнение по объекту, а не по имени -
            // имена в сцене никто не обязывался держать уникальными
            if (victim.gameObject == gameObject)
                return;

            // Жертва - стражник? Проверка по типу, а не по имени и не по тегу:
            // GuardAI и есть маркер, заводить второй незачем. Игрока сюда пропускать
            // нельзя - бьёт его сам стражник, и это было бы «свидетельство»
            // собственного удара
            if (victim.GetComponent<GuardAI>() == null)
                return;

            if (Notices(victim.transform.position))
                _guard.RaiseAggro();
        }

        /// <summary>
        /// Слух - первым, и это не про справедливость, а про цену: он стоит одного
        /// сравнения дистанции, а зрение - угла и, если тот сошёлся, ещё и рейкаста.
        /// Гонять дорогую проверку раньше дешёвой смысла нет
        /// </summary>
        private bool Notices(Vector3 victimPosition)
        {
            // Звук идёт от тела, поэтому дистанция меряется между корнями. Стены
            // не проверяются ВОВСЕ - слух сквозь них по решению дизайна, а не по забывчивости
            if (PerceptionCheck.CanHear(transform.position, victimPosition, _perception.HearingRadius))
                return true;

            Vector3 eye = transform.position + Vector3.up * _perception.EyeHeight;

            // Целимся в глаза жертвы, а не в её корень: луч в ноги идёт впритирку
            // к палубе и цепляется за столешницу досмотра. Высота та же самая -
            // жертва такой же стражник, из того же префаба
            Vector3 target = victimPosition + Vector3.up * _perception.EyeHeight;

            if (!PerceptionCheck.CanSee(
                    eye, transform.forward, target,
                    _perception.VisionRange, _perception.VisionFovFullDegrees))
                return false;

            // И только теперь физика. Прямая видимость - единственное, чего чистая
            // геометрия знать не может
            return !Physics.Linecast(
                eye, target, _perception.SightBlockers, QueryTriggerInteraction.Ignore);
        }
    }
}
