using CrateExpectations.Combat;
using CrateExpectations.Core.Events;
using CrateExpectations.Core.Services;
using CrateExpectations.Core.StateMachine;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Стражник порта. Сам он почти ничего не делает: спрашивает у <see cref="GuardBrain"/>,
    /// чем заниматься, держит generic <see cref="StateMachine"/> из <c>Core</c> и раздаёт
    /// состояниям то, что им нужно, через <see cref="IGuardStateContext"/>. Поведение живёт
    /// в состояниях, решение - в мозге, числа - в ассетах. Тонкий слой ввода/вывода,
    /// как и положено <see cref="MonoBehaviour"/>.
    /// <para>
    /// Зависимости приходят НЕ через <c>RegisterComponentInHierarchy</c>, как у всех
    /// остальных компонентов сцены, и это не отступление от принятой схемы, а
    /// вынужденное её расширение. Тот способ регистрирует РОВНО ОДИН компонент типа -
    /// первый найденный в иерархии, - и на нём построена вся сцена, потому что до сих
    /// пор каждого такого компонента в ней был ровно один: один игрок, один инспектор,
    /// один взаимодействователь. Стражников будет несколько, и второй с третьим
    /// остались бы без инъекции молча - без ошибки в консоли, просто с пустым полем.
    /// Поэтому <c>GameLifetimeScope</c> обходит их всех и инъецирует каждого поимённо.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public sealed class GuardAI : MonoBehaviour, IGuardStateContext
    {
        [Tooltip("Темп обхода: длина пауз и частота оглядываний. Один ассет на всех стражников")]
        [SerializeField] private GuardMovementDefinition _movement;

        [Tooltip("Боевые числа: радиус удара, скорость погони, фазы замаха. " +
                 "Один ассет на всех стражников")]
        [SerializeField] private GuardCombatDefinition _combat;

        [Tooltip("Маршрут обхода. Пусто - стражник стоит на посту: маршрут привязан " +
                 "к месту на доке, а не к типу NPC")]
        [SerializeField] private PatrolRoute _patrolRoute;

        private readonly StateMachine _machine = new();

        private GuardBrain _brain;
        private GuardHoldPostState _holdPost;
        private GuardPatrolState _patrol;
        private GuardStaggerState _stagger;
        private GuardChaseState _chase;
        private GuardAttackState _attack;

        // Необязательный сосед: снимут реакцию с префаба - стражник просто перестанет
        // вздрагивать. Ссылка берётся один раз в Awake, а не GetComponent в кадре
        private GuardHitReaction _hitReaction;

        // Своё здоровье. Через него приходит агро: бьют - значит есть кого бить в ответ
        private HealthComponent _health;

        private IEventBus _bus;
        private IPlayerTarget _target;

        /// <summary>Чем стражник занят - для отладки и будущего UI</summary>
        public GuardIntent Intent { get; private set; }

        /// <summary>
        /// Стражник взведён и занят игроком. Включается от любого урона по себе,
        /// от чужой драки в поле чувств (<see cref="GuardWitness"/>) и от зова
        /// на помощь (<see cref="GuardAlertResponder"/>), и <b>больше не гаснет</b>:
        /// затухание агро сознательно оставлено за скоупом - сбежать от стражника
        /// сейчас нельзя, и это решение, а не недоделка
        /// </summary>
        public bool IsAggro { get; private set; }

        /// <inheritdoc />
        public NavMeshAgent Agent { get; private set; }

        /// <inheritdoc />
        public Animator Animator { get; private set; }

        /// <inheritdoc />
        public GuardMovementDefinition Movement => _movement;

        /// <inheritdoc />
        public GuardCombatDefinition Combat => _combat;

        /// <inheritdoc />
        public PatrolRoute Route => _patrolRoute;

        /// <inheritdoc />
        public IPlayerTarget Target => _target;

        /// <inheritdoc />
        public GuardMeleeAttack Melee { get; private set; }

        /// <inheritdoc />
        public bool IsAttackCommitted { get; set; }

        /// <inheritdoc />
        public bool IsHyperArmored { get; set; }

        /// <summary>
        /// Шина событий и цель погони. Инъекция доезжает до КАЖДОГО стражника сцены:
        /// <c>GameLifetimeScope.InjectGuards</c> обходит их поимённо, потому что
        /// <c>RegisterComponentInHierarchy</c> зарегистрировал бы ровно одного
        /// </summary>
        [Inject]
        public void Construct(IEventBus bus, IPlayerTarget target)
        {
            _bus = bus;
            _target = target;
        }

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Animator = GetComponent<Animator>();
            _hitReaction = GetComponent<GuardHitReaction>();
            _health = GetComponent<HealthComponent>();
            Melee = GetComponent<GuardMeleeAttack>();

            if (_movement == null)
            {
                Debug.LogError($"Стражнику '{name}' не назначен GuardMovementDefinition - " +
                               "обходить маршрут будет нечем.", this);
                enabled = false;
                return;
            }

            if (_combat == null)
            {
                Debug.LogError($"Стражнику '{name}' не назначен GuardCombatDefinition - " +
                               "гнаться и бить будет нечем.", this);
                enabled = false;
                return;
            }

            _brain = new GuardBrain();
            _holdPost = new GuardHoldPostState(this);
            _patrol = new GuardPatrolState(this);
            _stagger = new GuardStaggerState(this);
            _chase = new GuardChaseState(this);
            _attack = new GuardAttackState(this);
        }

        /// <summary>
        /// Подписка на СВОЁ здоровье - прямой ссылкой, а не через шину: сосед по объекту,
        /// нулевая задержка, и незачем будить всех остальных стражников порта каждым
        /// попаданием по одному
        /// </summary>
        private void OnEnable()
        {
            if (_health != null)
                _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Damaged -= OnDamaged;
        }

        /// <summary>
        /// Любой урон включает агро - и лёгкий, и тяжёлый. Различать тир здесь незачем:
        /// вопрос «заметил ли стражник, что его бьют» на силу удара не отвечает
        /// </summary>
        private void OnDamaged(DamageResult result, HitInfo hit) => RaiseAggro();

        /// <summary>
        /// Взвести агро извне. Единственная точка входа для тех, кто заметил драку
        /// не собственной шкурой: <see cref="GuardWitness"/> - органами чувств,
        /// <see cref="GuardAlertResponder"/> - по зову инспектора. Собственный урон
        /// ходит сюда же, чтобы правило «мёртвый ничего не замечает» жило в одном месте.
        /// <para>
        /// Метод, а не публичный сеттер: снаружи агро можно только <b>взвести</b>.
        /// Снимать его некому - затухания в игре нет, - и открытый сеттер обещал бы
        /// возможность, которой нет
        /// </para>
        /// </summary>
        public void RaiseAggro()
        {
            // Труп не поднимает тревогу. В обычной жизни сюда не доходят - GuardDeath
            // гасит GuardAI целиком, - но свидетельство и зов приходят в компоненты,
            // которые смерть не выключает, и без этой проверки труп «услышал» бы драку
            if (IsDead)
                return;

            IsAggro = true;
        }

        private void Update()
        {
            // Намерение пересчитывается каждый кадр, а не один раз на старте: маршрут
            // могут снять или выдать по ходу смены, и стражник обязан это заметить сам.
            // Аллокаций тут нет - GuardContext это структура
            Intent = _brain.Decide(new GuardContext(
                HasRoute, IsStaggered, IsDead, IsAggro, IsInAttackRange, IsAttackCommitted));

            // ChangeState сам отсекает переход в то же состояние по ссылке,
            // так что сравнивать намерения вручную незачем
            _machine.ChangeState(StateFor(Intent));
            _machine.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Маршрут без единой точки - это не маршрут: стражник ушёл бы в обход,
        /// которого нет, и застыл бы посреди дока без единой строчки в консоли
        /// </summary>
        private bool HasRoute => _patrolRoute != null && _patrolRoute.Points.Count > 0;

        /// <summary>
        /// Не оправился ли от удара. Без компонента реакции - никогда: стражник без
        /// вздрагивания и не оглушается, иначе он замирал бы без единого признака,
        /// почему именно
        /// </summary>
        private bool IsStaggered => _hitReaction != null && _hitReaction.IsStaggered;

        /// <summary>Здоровье кончилось. Без компонента здоровья стражник неубиваем и жив</summary>
        private bool IsDead => _health != null && _health.IsDead;

        /// <summary>
        /// Игрок в пределах удара. Сравнение квадратов расстояния - корень здесь ничего
        /// не решает, а считается каждый кадр у каждого стражника
        /// </summary>
        private bool IsInAttackRange
        {
            get
            {
                if (_target == null || !_target.Exists)
                    return false;

                float range = _combat.AttackRange;

                return (_target.Position - transform.position).sqrMagnitude <= range * range;
            }
        }

        private IState StateFor(GuardIntent intent) => intent switch
        {
            GuardIntent.Stagger => _stagger,
            GuardIntent.Attack => _attack,
            GuardIntent.Chase => _chase,
            GuardIntent.Patrol => _patrol,
            _ => _holdPost,
        };
    }
}
