using System;
using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Жизненный цикл оружия в руке: убрано - достаёт - готов - бьёт - убирает.
    /// Обычный C#-класс поверх <see cref="StateMachine"/> из Core: про Animator, префабы и
    /// ввод ничего не знает, только отсчитывает время и сообщает, что изменилось.
    /// Все длительности приходят снаружи (<see cref="WeaponTimings"/> из ассета оружия),
    /// поэтому темп фехтования правится в данных, а не здесь
    /// </summary>
    public sealed class WeaponStateMachine
    {
        private readonly StateMachine _machine = new();

        private readonly IState _sheathed;
        private readonly IState _drawing;
        private readonly IState _ready;
        private readonly IState _attacking;
        private readonly IState _sheathing;

        /// <summary>Текущее состояние. Меняется только изнутри машины</summary>
        public WeaponState State { get; private set; }

        /// <summary>Видно ли оружие в руке прямо сейчас</summary>
        public bool IsWeaponVisible { get; private set; }

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

            _attacking = new WeaponTimedState(
                timings.AttackDuration,
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

        /// <summary>Взмах. Бить можно только из боевой стойки: ни серий, ни отмен пока нет</summary>
        public void Attack()
        {
            if (State == WeaponState.Ready)
                Enter(WeaponState.Attacking);
        }

        public void Tick(float deltaTime) => _machine.Tick(deltaTime);

        private void Enter(WeaponState next)
        {
            // Порядок важен: сначала новое значение и оповещение, потом сам вход в состояние.
            // Иначе слушатель, спросивший State из обработчика, увидел бы предыдущее
            State = next;
            StateChanged?.Invoke(next);

            _machine.ChangeState(Resolve(next));
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
