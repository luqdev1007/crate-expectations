using System;
using CrateExpectations.Core.StateMachine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Состояние фиксированной длительности: отсчитывает время, ровно на середине один раз
    /// дёргает <c>onMidpoint</c> и по истечении сообщает, что закончилось.
    /// Середина нужна доставанию и убиранию - там в этот момент оружие появляется в руке
    /// или исчезает из неё, посреди кроссфейда, когда рука уже пошла, но ещё не дошла
    /// </summary>
    internal sealed class WeaponTimedState : IState
    {
        private readonly float _duration;
        private readonly Action _onMidpoint;
        private readonly Action _onFinished;

        private float _elapsed;
        private bool _midpointPassed;

        /// <param name="duration">Длительность, с. Источник - ассет оружия, не длина клипа</param>
        /// <param name="onMidpoint">Необязательный разовый вызов на половине длительности</param>
        /// <param name="onFinished">Куда уходить по истечении времени</param>
        public WeaponTimedState(float duration, Action onMidpoint, Action onFinished)
        {
            _duration = duration;
            _onMidpoint = onMidpoint;
            _onFinished = onFinished;
        }

        public void Enter()
        {
            _elapsed = 0f;
            _midpointPassed = false;

            // Нулевая длительность - вырожденный, но легальный случай (ассет с нулём в поле):
            // середина и конец приходятся на один и тот же момент входа
            if (_duration <= 0f)
                Finish();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (!_midpointPassed && _elapsed >= _duration * 0.5f)
            {
                _midpointPassed = true;
                _onMidpoint?.Invoke();
            }

            if (_elapsed >= _duration)
                Finish();
        }

        public void Exit()
        {
        }

        private void Finish()
        {
            // Состояние могли покинуть досрочно, а середину при этом так и не проиграть -
            // тогда оружие осталось бы в прежней видимости. Гарантируем разовый вызов здесь
            if (!_midpointPassed)
            {
                _midpointPassed = true;
                _onMidpoint?.Invoke();
            }

            _onFinished();
        }
    }
}
