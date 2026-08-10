using System;
using CrateExpectations.Combat;
using CrateExpectations.Player.Combat;
using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Переводит состояния оружия в параметры <see cref="Animator"/>. Единственное место в
    /// игроке, которое вообще знает про аниматор: <see cref="WeaponStateMachine"/> меняет
    /// состояние, а чем его отыграть, решает этот компонент. Снимите его - фехтование
    /// продолжит работать, просто молча
    /// <para>
    /// Аниматоров у игрока два - физическое тело и вьюмодель под камерой, - и оба получают
    /// один и тот же набор вызовов. Графы у них разные и общими быть не могут: у тела
    /// TPS-клипы, снятые камерой из-за спины, у вьюмодели - FPP-пак, авторенный из глаза.
    /// Друг о друге они не знают, синхронными их держит то, что источник состояния один.
    /// </para>
    /// <para>
    /// Приёмов у вьюмодели восемь, у тела один: TPS-пак разных ударов не даёт. Поэтому
    /// номер приёма получает только тот аниматор, чей граф про него знает, а темп -
    /// оба, каждый по длине СВОЕГО клипа. Общий множитель уложил бы в тайминг
    /// только один из них.
    /// </para>
    /// </summary>
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        private const string AttackIndexName = "AttackIndex";
        private const string BlockPhaseName = "BlockPhase";

        private static readonly int IsArmedId = Animator.StringToHash("IsArmed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");
        private static readonly int AttackIndexId = Animator.StringToHash(AttackIndexName);

        // Одно число на три фазы вместо трёх флагов: фазы взаимоисключающие, и двумя
        // одновременно поднятыми флагами выбор стейта достался бы порядку переходов в графе
        private static readonly int BlockPhaseId = Animator.StringToHash(BlockPhaseName);

        /// <summary>
        /// Аниматор и то, чем он отличается от соседа. Пара, а не два параллельных массива:
        /// разъехавшись на один элемент, они молча выдали бы клип не тому скелету
        /// </summary>
        [Serializable]
        private struct AnimatorTarget
        {
            public Animator Animator;

            [Tooltip("Взмах этому аниматору рисует ViewModelSwing. Клип взмаха он получит " +
                     "только тогда, когда процедурная дуга выключена тумблером в ассете - " +
                     "это и есть переключатель A/B между дугой и клипом")]
            public bool SwingDrawnProcedurally;

            [Tooltip("Единственный клип взмаха этого аниматора. Нужен только тем графам, " +
                     "которые НЕ умеют выбирать приём номером: у них один стейт удара " +
                     "на все случаи, и подогнать его под тайминг приёма можно только " +
                     "по этой длине. Граф вьюмодели берёт длину из самого приёма, " +
                     "и поле ему не нужно")]
            public AnimationClip AttackClip;

            /// <summary>
            /// Умеет ли граф выбирать приём номером. Разрешаем один раз в <c>Awake</c>:
            /// у тела такого параметра нет, а запись несуществующего - это варнинг
            /// в консоль на каждый удар
            /// </summary>
            [NonSerialized] public bool SupportsAttackIndex;

            /// <summary>Знает ли граф про блок. У тела клипов блока нет</summary>
            [NonSerialized] public bool SupportsBlock;
        }

        [Tooltip("Чьи состояния отыгрываем")]
        [SerializeField] private PlayerWeaponController _weapon;

        [Tooltip("Кому отыгрывать: физическое тело и вьюмодель. Порядок не важен - " +
                 "различие между ними задаёт флаг, а не место в списке")]
        [SerializeField] private AnimatorTarget[] _animators;

        private void Awake()
        {
            if (_weapon == null)
            {
                Debug.LogError($"Аниматору игрока '{name}' не назначено оружие - отыгрывать нечего.", this);
                enabled = false;
                return;
            }

            if (_animators == null || _animators.Length == 0)
            {
                Debug.LogError($"Аниматору игрока '{name}' не назначен ни один Animator - отыгрывать некому.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < _animators.Length; i++)
            {
                Animator animator = _animators[i].Animator;

                if (animator == null)
                    continue;

                // Тело двигает Rigidbody-контроллер, вьюмодель прибита к камере. Root motion
                // добавил бы к этому ещё и смещение из клипа - игрок поехал бы сам по себе,
                // мимо своей физики, а руки уползли бы из кадра
                animator.applyRootMotion = false;

                _animators[i].SupportsAttackIndex =
                    AnimatorParameters.Has(animator, AttackIndexName);

                _animators[i].SupportsBlock =
                    AnimatorParameters.Has(animator, BlockPhaseName);
            }
        }

        private void OnEnable()
        {
            _weapon.StateChanged += OnWeaponStateChanged;

            // Компонент могли включить посреди боя - подтягиваем стойку под текущее состояние
            SetBool(IsArmedId, IsArmed(_weapon.State));
        }

        private void OnDisable() => _weapon.StateChanged -= OnWeaponStateChanged;

        private void OnWeaponStateChanged(WeaponState state)
        {
            SetBool(IsArmedId, IsArmed(state));
            SetBlockPhase(state);

            if (state == WeaponState.Attacking)
            {
                SetAttackTrigger();
                return;
            }

            // Триггер живёт до первого срабатывания. Не выстрелив, он дождался бы следующего
            // доставания и махнул саблей в тот момент, когда игрок только достал её из-за пояса
            ResetTrigger(AttackId);
        }

        /// <summary>
        /// Взмах достаётся не всем: аниматор, чью дугу рисует <see cref="ViewModelSwing"/>,
        /// клип не получает - иначе поверх процедурной дуги поедет ещё и клиповая.
        /// Выключенный тумблер процедурного замаха возвращает клип на место
        /// </summary>
        private void SetAttackTrigger()
        {
            AttackDefinition attack = _weapon.CurrentAttack;

            if (attack == null)
                return;

            SwingDefinition swing = _weapon.Weapon.Swing;
            bool procedural = swing != null && swing.UseProceduralSwing;
            int index = _weapon.Weapon.Attacks.IndexOf(attack);

            foreach (AnimatorTarget target in _animators)
            {
                if (target.Animator == null)
                    continue;

                if (target.SwingDrawnProcedurally && procedural)
                    continue;

                ApplyAttackSpeed(target, attack);

                // Номер обязан лечь раньше триггера: граф читает оба в одном вычислении
                // перехода, и выставленный следом номер достался бы уже следующему удару
                if (target.SupportsAttackIndex)
                    target.Animator.SetInteger(AttackIndexId, index);

                target.Animator.SetTrigger(AttackId);
            }
        }

        /// <summary>
        /// Темп задаёт <c>Duration</c> приёма, а не длина клипа: множитель скорости стейта -
        /// это ровно то число, которое уложит клип в заказанное время. Поменяли длительность
        /// в ассете приёма - удар стал быстрее, клип трогать не нужно.
        /// <para>
        /// Длина берётся у того клипа, который этот аниматор действительно играет: граф
        /// вьюмодели ставит клип приёма, граф тела - свой единственный. Одно число на двоих
        /// уложило бы в тайминг только одного из них.
        /// </para>
        /// </summary>
        private static void ApplyAttackSpeed(AnimatorTarget target, AttackDefinition attack)
        {
            AnimationClip clip = target.SupportsAttackIndex ? attack.Clip : target.AttackClip;

            if (clip == null || attack.Duration <= 0f)
                return;

            target.Animator.SetFloat(AttackSpeedId, clip.length / attack.Duration);
        }

        /// <summary>
        /// Фаза блока числом: 0 - блока нет, 1 - подъём, 2 - удержание, 3 - опускание.
        /// Пишется только тем графам, которые про блок знают, - у тела такого параметра
        /// нет, и запись несуществующего дала бы варнинг на каждое нажатие
        /// </summary>
        private void SetBlockPhase(WeaponState state)
        {
            int phase = BlockPhase(state);

            foreach (AnimatorTarget target in _animators)
                if (target.Animator != null && target.SupportsBlock)
                    target.Animator.SetInteger(BlockPhaseId, phase);
        }

        private static int BlockPhase(WeaponState state)
        {
            switch (state)
            {
                case WeaponState.BlockRaise: return 1;
                case WeaponState.Blocking: return 2;
                case WeaponState.BlockLower: return 3;
                default: return 0;
            }
        }

        private void SetBool(int id, bool value)
        {
            foreach (AnimatorTarget target in _animators)
                if (target.Animator != null)
                    target.Animator.SetBool(id, value);
        }

        private void ResetTrigger(int id)
        {
            foreach (AnimatorTarget target in _animators)
                if (target.Animator != null)
                    target.Animator.ResetTrigger(id);
        }

        /// <summary>
        /// Боевая стойка держится всё время, пока оружие в игре, включая само доставание и
        /// убирание: именно переход в неё и обратно и читается как "достал" / "убрал"
        /// </summary>
        private static bool IsArmed(WeaponState state) =>
            state == WeaponState.Drawing ||
            state == WeaponState.Ready ||
            state == WeaponState.Attacking ||
            state == WeaponState.BlockRaise ||
            state == WeaponState.Blocking ||
            state == WeaponState.BlockLower;
    }
}
