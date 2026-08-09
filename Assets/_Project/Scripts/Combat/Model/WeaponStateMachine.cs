using System;
using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Жизненный цикл оружия в руке: убрано - достаёт - готов - бьёт - убирает.
    /// Обычный C#-класс поверх <see cref="StateMachine"/> из Core: про Animator, префабы и
    /// ввод ничего не знает, только отсчитывает время и сообщает, что изменилось.
    /// Все длительности приходят снаружи, поэтому темп фехтования правится в данных,
    /// а не здесь
    /// </summary>
    public sealed class WeaponStateMachine
    {
        private readonly StateMachine _machine = new();

        private readonly IState _sheathed;
        private readonly IState _drawing;
        private readonly IState _ready;
        private readonly WeaponTimedState _attacking;
        private readonly IState _sheathing;

        /// <summary>С какой доли текущего удара его разрешено прервать следующим</summary>
        private float _cancelAfter = 1f;

        /// <summary>Текущее состояние. Меняется только изнутри машины</summary>
        public WeaponState State { get; private set; }

        /// <summary>Видно ли оружие в руке прямо сейчас</summary>
        public bool IsWeaponVisible { get; private set; }

        /// <summary>Сколько текущего удара уже прошло, 0..1. Вне удара - единица</summary>
        public float AttackProgress => State == WeaponState.Attacking ? _attacking.Progress : 1f;

        /// <summary>
        /// Можно ли ударить прямо сейчас. Из стойки - всегда; посреди удара - только
        /// когда тот дошёл до своего окна отмены
        /// </summary>
        public bool CanAttack =>
            State == WeaponState.Ready ||
            (State == WeaponState.Attacking && _attacking.Progress >= _cancelAfter);

        public event Action<WeaponState> StateChanged;

        /// <summary>
        /// Оружие пора показать (<c>true</c>) или спрятать (<c>false</c>). Срабатывает
        /// на середине доставания и убирания, а не на их краях
        /// </summary>
        public event Action<bool> WeaponVisibilityChanged;

        public WeaponStateMachine(WeaponTimings timings)
        {
            _sheathed = new WeaponRestState();
            _ready = new WeaponRestState();

            _drawing = new WeaponTimedState(
                timings.DrawDuration,
                () => SetWeaponVisible(true),
                () => Enter(WeaponState.Ready));

            _sheathing = new WeaponTimedState(
                timings.SheatheDuration,
                () => SetWeaponVisible(false),
                () => Enter(WeaponState.Sheathed));

            // Длительность удара ставится на каждый вход: она своя у каждого приёма
            _attacking = new WeaponTimedState(0f, null, () => Enter(WeaponState.Ready));

            State = WeaponState.Sheathed;
            _machine.ChangeState(_sheathed);
        }

        /// <summary>
        /// Достать или убрать - смотря что сейчас. Посреди доставания, удара и убирания
        /// нажатие проглатывается: очередь команд на этом шаге не нужна
        /// </summary>
        public void ToggleWeapon()
        {
            switch (State)
            {
                case WeaponState.Sheathed:
                    Enter(WeaponState.Drawing);
                    break;

                case WeaponState.Ready:
                    Enter(WeaponState.Sheathing);
                    break;
            }
        }

        /// <summary>
        /// Ударить. Длительность и точка отмены приходят от приёма, а не хранятся здесь:
        /// машина не знает, каким ударом бьют, и знать не должна.
        /// <para>
        /// Удар посреди удара - это не ошибка ввода, а отмена доводки: последние кадры
        /// приёма игрок уже досматривает, а не участвует в них, и заставлять его ждать
        /// их конца значит делать бой вязким. Раньше <paramref name="cancelAfter"/>
        /// нажатие отбрасывается - иначе можно было бы отменить удар до того,
        /// как он попал.
        /// </para>
        /// </summary>
        /// <param name="duration">Сколько длится приём, с</param>
        /// <param name="cancelAfter">С какой доли этого приёма его самого можно прервать</param>
        /// <returns><c>true</c>, если удар начался. <c>false</c> - нажатие не прошло,
        /// и его имеет смысл придержать в буфере</returns>
        public bool Attack(float duration, float cancelAfter)
        {
            if (!CanAttack)
                return false;

            _attacking.Duration = duration;
            _cancelAfter = cancelAfter;

            Enter(WeaponState.Attacking);

            return true;
        }

        public void Tick(float deltaTime) => _machine.Tick(deltaTime);

        private void Enter(WeaponState next)
        {
            // Порядок важен: сначала новое значение и оповещение, потом сам вход в состояние.
            // Иначе слушатель, спросивший State из обработчика, увидел бы предыдущее
            State = next;
            StateChanged?.Invoke(next);

            IState state = Resolve(next);

            // Удар сменяется ударом при отмене доводки - состояние при этом ТО ЖЕ САМОЕ,
            // а обычная смена его бы не тронула: FSM намеренно не входит повторно в то,
            // в чём уже находится. Для боя это не "ничего не изменилось", а новый приём,
            // и без явного перезапуска он доигрывал бы остаток времени предыдущего
            if (ReferenceEquals(_machine.Current, state))
                state.Enter();
            else
                _machine.ChangeState(state);
        }

        private IState Resolve(WeaponState state)
        {
            switch (state)
            {
                case WeaponState.Drawing: return _drawing;
                case WeaponState.Ready: return _ready;
                case WeaponState.Attacking: return _attacking;
                case WeaponState.Sheathing: return _sheathing;
                default: return _sheathed;
            }
        }

        private void SetWeaponVisible(bool visible)
        {
            if (IsWeaponVisible == visible)
                return;

            IsWeaponVisible = visible;
            WeaponVisibilityChanged?.Invoke(visible);
        }
    }
}
