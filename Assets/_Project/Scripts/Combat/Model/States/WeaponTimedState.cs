using System;
using CrateExpectations.Core.StateMachine;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Состояние фиксированной длительности: отсчитывает время, ровно один раз дёргает
    /// <c>onCue</c> на заданной своей доле и по истечении сообщает, что закончилось.
    /// Отметка нужна доставанию и убиранию - в этот момент оружие появляется в руке
    /// или исчезает из неё, посреди кроссфейда, когда рука уже пошла, но ещё не дошла.
    /// <para>
    /// Доля приходит снаружи, а не прибита к середине: где именно кисть выходит из-за
    /// объектива, зависит от КЛИПА, и подбирается это число глазами. Середина была
    /// догадкой, и для клипов FPP-пака она неверна - там рука уходит за камеру
    /// в середине доставания, а не выходит вперёд.
    /// </para>
    /// <para>
    /// <see cref="Duration"/> меняется снаружи, потому что у удара она своя на каждый приём:
    /// машина ставит её перед входом. Доставание и убирание своё значение просто не трогают.
    /// </para>
    /// </summary>
    internal sealed class WeaponTimedState : IState
    {
        private readonly Action _onCue;
        private readonly Action _onFinished;

        /// <summary>Доля длительности, на которой срабатывает <see cref="_onCue"/></summary>
        private readonly float _cueFraction;

        private float _elapsed;
        private bool _cuePassed;

        /// <param name="duration">Длительность, с. Источник - ассет, не длина клипа</param>
        /// <param name="onCue">Необязательный разовый вызов на доле <paramref name="cueFraction"/></param>
        /// <param name="onFinished">Куда уходить по истечении времени</param>
        /// <param name="cueFraction">Доля длительности для <paramref name="onCue"/>, 0..1</param>
        public WeaponTimedState(float duration, Action onCue, Action onFinished, float cueFraction = 0.5f)
        {
            Duration = duration;
            _onCue = onCue;
            _onFinished = onFinished;
            _cueFraction = Mathf.Clamp01(cueFraction);
        }

        /// <summary>Длительность, с. Ставится перед входом в состояние</summary>
        public float Duration { get; set; }

        /// <summary>Сколько состояния уже прошло, 0..1</summary>
        public float Progress => Duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / Duration);

        public void Enter()
        {
            _elapsed = 0f;
            _cuePassed = false;

            // Нулевая длительность - вырожденный, но легальный случай (ассет с нулём в поле):
            // отметка и конец приходятся на один и тот же момент входа
            if (Duration <= 0f)
            {
                Finish();
                return;
            }

            // Нулевая доля - тоже легальный случай, и означает она «в тот же кадр»,
            // а не «на первом тике». Через Tick это отставало бы на кадр
            if (_cueFraction <= 0f)
                RaiseCue();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            if (!_cuePassed && _elapsed >= Duration * _cueFraction)
                RaiseCue();

            if (_elapsed >= Duration)
                Finish();
        }

        public void Exit()
        {
        }

        private void RaiseCue()
        {
            _cuePassed = true;
            _onCue?.Invoke();
        }

        private void Finish()
        {
            // Состояние могли покинуть досрочно, а отметку при этом так и не проиграть -
            // тогда оружие осталось бы в прежней видимости. Гарантируем разовый вызов здесь
            if (!_cuePassed)
                RaiseCue();

            _onFinished();
        }
    }
}
