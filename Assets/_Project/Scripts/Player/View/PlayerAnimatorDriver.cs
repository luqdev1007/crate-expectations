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
    /// гуманоидный скелет с TPS-клипами, у вьюмодели - Generic-риг из одних рук с клипами,
    /// авторенными сразу под первое лицо. Друг о друге они не знают, синхронными их держит
    /// то, что источник состояния один: аниматор ничего не решает сам и лишь отыгрывает то,
    /// что ему сказали.
    /// </para>
    /// <para>
    /// Анимаций доставания и убирания в проекте нет, поэтому их роль играет кроссфейд между
    /// обычной стойкой и боевой: длительность перехода в контроллере подобрана под
    /// <c>DrawDuration</c> из ассета оружия.
    /// </para>
    /// <para>
    /// На взмахе аниматоры могут разойтись, и это единственное их расхождение. Физическое
    /// тело клип взмаха играет всегда: его дугу видно в тени на земле, и по ней же будут
    /// читать намерение игрока NPC. Вьюмодель клип получает тогда, когда выключен тумблер
    /// процедурной дуги в <see cref="SwingDefinition"/>, - это и есть переключатель A/B
    /// между клипом и <see cref="ViewModelSwing"/>. Решает это тот, кто раздаёт триггеры,
    /// а не сами графы.
    /// </para>
    /// </summary>
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        private const string SlashAlternateName = "SlashAlternate";

        private static readonly int IsArmedId = Animator.StringToHash("IsArmed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");
        private static readonly int SlashAlternateId = Animator.StringToHash(SlashAlternateName);

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

            // Клип у каждого аниматора свой, потому что скелеты разные: у тела TPS-клип из
            // пака Kevin Iglesias, у вьюмодели - клип из Arms.fbx, авторенный под первое лицо.
            // Длины у них не совпадают, а подчинить обе одному AttackDuration надо -
            // значит и множитель скорости у каждого свой
            [Tooltip("Тот же клип взмаха, что стоит в стейте Attack контроллера ЭТОГО " +
                     "аниматора. Нужен только чтобы узнать его длину и подогнать её под " +
                     "тайминг из ассета оружия")]
            public AnimationClip AttackClip;

            /// <summary>
            /// Есть ли у контроллера параметр чередования. Разрешаем один раз в
            /// <c>Awake</c>: контроллер тела про чередование не знает, а запись
            /// несуществующего параметра - это варнинг в консоль на каждый удар
            /// </summary>
            [NonSerialized] public bool SupportsSlashAlternate;
        }

        [Tooltip("Чьи состояния отыгрываем")]
        [SerializeField] private PlayerWeaponController _weapon;

        [Tooltip("Кому отыгрывать: физическое тело и вьюмодель. Порядок не важен - " +
                 "различие между ними задаёт флаг, а не место в списке")]
        [SerializeField] private AnimatorTarget[] _animators;

        // Каким из двух клипов взмаха отыграть следующий удар. Флаг живёт здесь, а не в
        // машине состояний: чередование - это разнообразие картинки, а не правило боя.
        // Машина про то, что ударов нарисовано два, знать не должна
        private bool _slashAlternate;

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

            float duration = Mathf.Max(_weapon.Weapon.AttackDuration, 0.01f);

            for (int i = 0; i < _animators.Length; i++)
            {
                Animator animator = _animators[i].Animator;

                if (animator == null)
                    continue;

                // Тело двигает Rigidbody-контроллер, вьюмодель прибита к камере. Root motion
                // добавил бы к этому ещё и смещение из клипа - игрок поехал бы сам по себе,
                // мимо своей физики, а руки уползли бы из кадра
                animator.applyRootMotion = false;

                _animators[i].SupportsSlashAlternate = HasParameter(animator, SlashAlternateName);

                ApplyAttackSpeed(_animators[i], duration);
            }
        }

        /// <summary>
        /// Темп взмаха задаёт ассет оружия, а не длина клипа: стейты взмаха умножают свою
        /// скорость на параметр <c>AttackSpeed</c>, и здесь считается ровно тот множитель,
        /// который уложит клип в <c>AttackDuration</c>. Поменяли число в ассете - взмах
        /// стал быстрее, клип трогать не нужно.
        /// <para>
        /// Считается на каждый аниматор отдельно: клипы у тела и у вьюмодели разной длины,
        /// и один множитель на двоих уложил бы в тайминг только одного из них.
        /// </para>
        /// </summary>
        private void ApplyAttackSpeed(AnimatorTarget target, float duration)
        {
            if (target.AttackClip == null)
            {
                Debug.LogWarning($"Аниматору '{target.Animator.name}' игрока '{name}' не назначен " +
                                 "клип взмаха - взмах пойдёт в темпе клипа, а не в темпе ассета оружия.", this);
                return;
            }

            target.Animator.SetFloat(AttackSpeedId, target.AttackClip.length / duration);
        }

        /// <summary>
        /// Есть ли у контроллера такой параметр. Единственный способ спросить - перебрать
        /// список: <see cref="Animator"/> проверки по имени не даёт, а запись в
        /// несуществующий параметр он молча не глотает
        /// </summary>
        private static bool HasParameter(Animator animator, string parameterName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].name == parameterName)
                    return true;

            return false;
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
            SwingDefinition swing = _weapon.Weapon.Swing;
            bool procedural = swing != null && swing.UseProceduralSwing;

            foreach (AnimatorTarget target in _animators)
            {
                if (target.Animator == null)
                    continue;

                if (target.SwingDrawnProcedurally && procedural)
                    continue;

                // Флаг обязан лечь раньше триггера: граф читает оба в одном вычислении
                // перехода, и выставленный следом флаг достался бы уже следующему удару
                if (target.SupportsSlashAlternate)
                    target.Animator.SetBool(SlashAlternateId, _slashAlternate);

                target.Animator.SetTrigger(AttackId);
            }

            // Переключаем после отправки, а не до: иначе самый первый удар за сессию
            // уходил бы во второй клип, а первый доставался бы только со второго нажатия
            _slashAlternate = !_slashAlternate;
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
            state == WeaponState.Attacking;
    }
}
