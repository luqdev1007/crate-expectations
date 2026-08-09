using System;
using CrateExpectations.Core.StateMachine;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Состояние фиксированной длительности: отсчитывает время, ровно на середине один раз
    /// дёргает <c>onMidpoint</c> и по истечении сообщает, что закончилось.
    /// Середина нужна доставанию и убиранию - там в этот момент оружие появляется в руке
    /// или исчезает из неё, посреди кроссфейда, когда рука уже пошла, но ещё не дошла.
    /// <para>
    /// <see cref="Duration"/> меняется снаружи, потому что у удара она своя на каждый приём:
    /// машина ставит её перед входом. Доставание и убирание своё значение просто не трогают.
    /// </para>
    /// </summary>
    internal sealed class WeaponTimedState : IState
    {
        private readonly Action _onMidpoint;
        private readonly Action _onFinished;

        private float _elapsed;
        private bool _midpointPassed;

        /// <param name="duration">Длительность, с. Источник - ассет, не длина клипа</param>
        /// <param name="onMidpoint">Необязательный разовый вызов на половине длительности</param>
        /// <param name="onFinished">Куда уходить по истечении времени</param>
        public WeaponTimedState(float duration, Action onMidpoint, Action onFinished)
        {
            Duration = duration;
            _onMidpoint = onMidpoint;
            _onFinished = onFinished;
        }

        /// <summary>Длительность, с. Ставится перед входом в состояние</summary>
        public float Duration { get; set; }

        /// <summary>Сколько состояния уже прошло, 0..1</summary>
        public float Progress => Duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / Duration);

        public void Enter()
        {
            _elapsed = 0f;
            _midpointPassed = false;

            // Нулевая длительность - вырожденный, но легальный случай (ассет с нулём в поле):
            // середина и конец приходятся на один и тот же момент входа
            if (Duration <= 0f)
                Finish();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (!_midpointPassed && _elapsed >= Duration * 0.5f)
            {
                _midpointPassed = true;
                _onMidpoint?.Invoke();
            }

            if (_elapsed >= Duration)
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
