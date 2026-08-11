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
        private readonly WeaponTimedState _blockRaise;
        private readonly IState _blocking;
        private readonly WeaponTimedState _blockLower;

        // Кнопку блока могли отпустить ещё на подъёме клинка. Дожимать подъём до конца
        // всё равно надо - иначе поза оборвётся на середине, - поэтому намерение
        // запоминается и проверяется в тот момент, когда подъём закончился
        private bool _blockHeld;

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
        /// <summary>
        /// Можно ли ударить прямо сейчас. Из стойки - всегда; посреди удара - только
        /// когда тот дошёл до своего окна отмены.
        /// <para>
        /// Блок в этот список не входит намеренно: пока клинок поднят, опущен или стоит
        /// в блоке, удара нет. Запрет живёт здесь, а не в контроллере ввода, потому что
        /// это правило боя, а не свойство кнопки.
        /// </para>
        /// </summary>
        public bool CanAttack =>
            State == WeaponState.Ready ||
            (State == WeaponState.Attacking && _attacking.Progress >= _cancelAfter);

        /// <summary>Держит ли игрок блок прямо сейчас - в любой из трёх его фаз</summary>
        public bool IsBlocking =>
            State == WeaponState.BlockRaise ||
            State == WeaponState.Blocking ||
            State == WeaponState.BlockLower;

        public event Action<WeaponState> StateChanged;

        /// <summary>
        /// Оружие пора показать (<c>true</c>) или спрятать (<c>false</c>). Срабатывает
        /// не на краях доставания и убирания, а на доле, заданной в
        /// <see cref="WeaponTimings.DrawRevealFraction"/> и
        /// <see cref="WeaponTimings.SheatheHideFraction"/>: момент зависит от того,
        /// когда кисть в клипе выходит из-за объектива, и подбирается глазами
        /// </summary>
        public event Action<bool> WeaponVisibilityChanged;

        public WeaponStateMachine(WeaponTimings timings)
        {
            _sheathed = new WeaponRestState();
            _ready = new WeaponRestState();

            _drawing = new WeaponTimedState(
                timings.DrawDuration,
                () => SetWeaponVisible(true),
                () => Enter(WeaponState.Ready),
                timings.DrawRevealFraction);

            _sheathing = new WeaponTimedState(
                timings.SheatheDuration,
                () => SetWeaponVisible(false),
                () => Enter(WeaponState.Sheathed),
                timings.SheatheHideFraction);

            // Длительность удара ставится на каждый вход: она своя у каждого приёма
            _attacking = new WeaponTimedState(0f, null, () => Enter(WeaponState.Ready));

            // Удержание - состояние без времени: из него выводит не таймер, а отпущенная
            // кнопка. Подъём и опускание, наоборот, идут ровно столько, сколько длится клип
            _blocking = new WeaponRestState();

            _blockRaise = new WeaponTimedState(
                timings.BlockRaiseDuration,
                null,
                () => Enter(_blockHeld ? WeaponState.Blocking : WeaponState.BlockLower));

            _blockLower = new WeaponTimedState(
                timings.BlockLowerDuration,
                null,
                () => Enter(WeaponState.Ready));

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

        /// <summary>
        /// Игрок зажал блок. Из стойки клинок идёт вверх; из любого другого состояния
        /// нажатие проглатывается - блок посреди удара это уже парирование, а его в проекте нет
        /// </summary>
        public void RaiseBlock()
        {
            _blockHeld = true;

            if (State == WeaponState.Ready)
                Enter(WeaponState.BlockRaise);
        }

        /// <summary>
        /// Игрок отпустил блок. С удержания клинок опускается сразу, а вот подъём
        /// прерывать нечем: он доигрывает и уходит вниз сам, увидев снятое намерение
        /// </summary>
        public void LowerBlock()
        {
            _blockHeld = false;

            if (State == WeaponState.Blocking)
                Enter(WeaponState.BlockLower);
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
                case WeaponState.BlockRaise: return _blockRaise;
                case WeaponState.Blocking: return _blocking;
                case WeaponState.BlockLower: return _blockLower;
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
