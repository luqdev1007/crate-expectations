namespace CrateExpectations.Core.StateMachine
{
    /// <summary>Лёгкий FSM на инстансах состояний</summary>
    public sealed class StateMachine
    {
        public IState Current { get; private set; }

        public void ChangeState(IState next)
        {
            if (ReferenceEquals(Current, next)) 
                return;

            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
