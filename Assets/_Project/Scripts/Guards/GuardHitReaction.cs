using CrateExpectations.Combat;
using UnityEngine;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Вздрагивание от удара: слушает здоровье, ставит графу нужный клип и держит счётчик
    /// «стражник ещё не оправился». Именно адаптер, а не поведение, - по образцу
    /// <see cref="GuardAnimatorDriver"/>: остановкой тела распоряжается
    /// <see cref="GuardStaggerState"/>, а этот компонент только переводит попадание
    /// в параметры аниматора и в один флаг наружу.
    /// <para>
    /// Разделение проходит по тому, кто что знает. <b>Чем ударили</b> - знает тот, кто
    /// принял удар: тир лежит в <see cref="HitInfo"/>, и добираться до него из состояния
    /// FSM пришлось бы, протаскивая удар через <see cref="GuardContext"/>, который
    /// намеренно не знает про Unity-типы. <b>Что при этом делает тело</b> - знает
    /// состояние: оно и так единственный владелец <c>NavMeshAgent</c>.
    /// </para>
    /// <para>
    /// Компонент необязателен: снимешь его с префаба - стражник перестанет вздрагивать,
    /// но продолжит ходить, получать урон и умирать. Ровно та же развязка, что
    /// у <see cref="GuardAnimatorDriver"/> и <see cref="GuardEquipment"/>
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class GuardHitReaction : MonoBehaviour
    {
        // Хеши считаются один раз на тип: строковый SetTrigger хеширует заново
        // и аллоцирует, а удар прилетает в кадре
        private static readonly int HitTriggerId = Animator.StringToHash("HitTrigger");
        private static readonly int HitIndexId = Animator.StringToHash("HitIndex");

        [Tooltip("Чем отвечать на удар: номера клипов в графе и их длины. " +
                 "Один ассет на всех стражников")]
        [SerializeField] private GuardReactionDefinition _reaction;

        private Animator _animator;
        private HealthComponent _health;

        // Через интерфейс, а не через конкретный GuardAI: реакции нужно ровно одно
        // свойство контекста, и знать, кто именно его держит, ей незачем
        private IGuardStateContext _context;

        private float _recoveryTimer;

        /// <summary>
        /// Стражник ещё не оправился от удара. Это спрашивает <see cref="GuardAI"/>,
        /// собирая снимок для <see cref="GuardBrain"/>: пока флаг взведён, обход
        /// не продолжается
        /// </summary>
        public bool IsStaggered => _recoveryTimer > 0f;

        /// <summary>
        /// Можно ли сбить стражника прямо сейчас. Нельзя ровно в активной фазе его
        /// собственного удара: клинок уже идёт, и остановить его встречным попаданием
        /// значило бы, что размен ударами всегда выигрывает тот, кто нажал позже.
        /// <para>
        /// Гипер-армор проверяется ЗДЕСЬ, а не в <see cref="GuardBrain"/>, и это
        /// не мелочь: мозг отвечает на вопрос «чего стражник хочет», а неуязвимость -
        /// это «что с ним можно сделать». Заведи её флагом в <see cref="GuardContext"/> -
        /// и пришлось бы объяснять, почему намерение зависит от того, бьют ли по нему
        /// прямо сейчас. Здесь же вздрагивание просто не взводится, и до мозга дело
        /// не доходит вовсе
        /// </para>
        /// <para>
        /// Без контекста - можно всегда: стражник без <see cref="GuardAI"/> не атакует,
        /// а значит и защищать ему нечего
        /// </para>
        /// </summary>
        public bool CanBeInterrupted => _context == null || !_context.IsHyperArmored;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _health = GetComponent<HealthComponent>();
            _context = GetComponent<IGuardStateContext>();

            if (_reaction != null)
                return;

            Debug.LogError($"Стражнику '{name}' не назначен GuardReactionDefinition - " +
                           "удары он будет принимать не моргнув.", this);
            enabled = false;
        }

        private void OnEnable() => _health.Damaged += OnDamaged;

        private void OnDisable() => _health.Damaged -= OnDamaged;

        /// <summary>
        /// Счётчик тикает здесь, а не в состоянии FSM, намеренно: состояние живёт ровно
        /// столько, сколько взведён флаг, и считать в нём срок собственной жизни -
        /// значит замкнуть его на себя. Аллокаций нет, ветка одна
        /// </summary>
        private void Update()
        {
            if (_recoveryTimer > 0f)
                _recoveryTimer -= Time.deltaTime;
        }

        private void OnDamaged(DamageResult result, HitInfo hit)
        {
            // Смертельный удар не вздрагивают: следом придёт Died, и тело уйдёт в регдол.
            // Без этой проверки труп успел бы дёрнуться за кадр до падения
            if (result.Died)
                return;

            // Гипер-армор: урон засчитан (здоровье его уже сняло), а реакции нет.
            // Стражник доводит свой удар, и внешне это читается как «он не дрогнул»,
            // а не как «попадание не прошло»
            if (!CanBeInterrupted)
                return;

            _animator.SetInteger(HitIndexId, _reaction.IndexFor(hit.Tier));
            _animator.SetTrigger(HitTriggerId);

            // Не складывается, а взводится заново: серия ударов держит стражника
            // оглушённым столько, сколько длится ПОСЛЕДНИЙ клип, а не сумма всех.
            // Переход графа в это же состояние разрешён (canTransitionToSelf),
            // поэтому картинка начинается заново вместе со счётчиком
            _recoveryTimer = _reaction.RecoveryFor(hit.Tier);
        }
    }
}
