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

        private float _recoveryTimer;

        /// <summary>
        /// Стражник ещё не оправился от удара. Это спрашивает <see cref="GuardAI"/>,
        /// собирая снимок для <see cref="GuardBrain"/>: пока флаг взведён, обход
        /// не продолжается
        /// </summary>
        public bool IsStaggered => _recoveryTimer > 0f;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _health = GetComponent<HealthComponent>();

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
