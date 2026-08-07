using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Состояние покоя: само по себе не заканчивается и ждёт команды снаружи.
    /// Таких два - "убрано" и "готов бить", и различает их только значение
    /// <see cref="WeaponState"/>, поэтому класс один
    /// </summary>
    internal sealed class WeaponRestState : IState
    {
        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
        }

        public void Exit()
        {
        }
    }
}
