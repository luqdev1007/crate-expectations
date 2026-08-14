using CrateExpectations.Core.Events;
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

        [Tooltip("Маршрут обхода. Пусто - стражник стоит на посту: маршрут привязан " +
                 "к месту на доке, а не к типу NPC")]
        [SerializeField] private PatrolRoute _patrolRoute;

        private readonly StateMachine _machine = new();

        private GuardBrain _brain;
        private GuardHoldPostState _holdPost;
        private GuardPatrolState _patrol;
        private GuardStaggerState _stagger;

        // Необязательный сосед: снимут реакцию с префаба - стражник просто перестанет
        // вздрагивать. Ссылка берётся один раз в Awake, а не GetComponent в кадре
        private GuardHitReaction _hitReaction;

        private IEventBus _bus;

        /// <summary>Чем стражник занят - для отладки и будущего UI</summary>
        public GuardIntent Intent { get; private set; }

        /// <inheritdoc />
        public NavMeshAgent Agent { get; private set; }

        /// <inheritdoc />
        public Animator Animator { get; private set; }

        /// <inheritdoc />
        public GuardMovementDefinition Movement => _movement;

        /// <inheritdoc />
        public PatrolRoute Route => _patrolRoute;

        /// <summary>
        /// Шина событий. Стражник пока в неё ничего не пишет и ни на что не подписан -
        /// ссылка взята сейчас, чтобы проверить сам путь доставки зависимостей
        /// до нескольких экземпляров в сцене
        /// </summary>
        [Inject]
        public void Construct(IEventBus bus) => _bus = bus;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Animator = GetComponent<Animator>();
            _hitReaction = GetComponent<GuardHitReaction>();

            if (_movement == null)
            {
                Debug.LogError($"Стражнику '{name}' не назначен GuardMovementDefinition - " +
                               "обходить маршрут будет нечем.", this);
                enabled = false;
                return;
            }

            _brain = new GuardBrain();
            _holdPost = new GuardHoldPostState(this);
            _patrol = new GuardPatrolState(this);
            _stagger = new GuardStaggerState(this);
        }

        private void Update()
        {
            // Намерение пересчитывается каждый кадр, а не один раз на старте: маршрут
            // могут снять или выдать по ходу смены, и стражник обязан это заметить сам.
            // Аллокаций тут нет - GuardContext это структура
            Intent = _brain.Decide(new GuardContext(HasRoute, IsStaggered));

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

        private IState StateFor(GuardIntent intent) => intent switch
        {
            GuardIntent.Stagger => _stagger,
            GuardIntent.Patrol => _patrol,
            _ => _holdPost,
        };
    }
}
