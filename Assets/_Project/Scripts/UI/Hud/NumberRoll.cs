using System;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Прокрутка числа: ведёт показанное значение от текущего к заданному за отведённое время.
    /// Замедляется к концу - глаз успевает поймать итоговую цифру, а не только мельтешение.
    ///
    /// <para>Чистая логика: ни о тексте, ни о кадрах не знает - её тикают снаружи. Поэтому
    /// прокрутку можно проверить edit-mode тестом, не поднимая сцену.</para>
    /// </summary>
    public sealed class NumberRoll
    {
        private int _from;
        private int _to;
        private float _seconds;
        private float _elapsed;

        /// <summary>Значение, которое нужно показать прямо сейчас</summary>
        public int Value { get; private set; }

        /// <summary>Прокрутка ещё идёт</summary>
        public bool IsRolling => _elapsed < _seconds;

        /// <summary>
        /// Доля пройденного пути, 0..1. Ею удобно гасить подсветку: она приходит в покой
        /// ровно тогда же, когда цифра встаёт на итоговое значение
        /// </summary>
        public float Progress => _seconds > 0f ? Math.Min(_elapsed / _seconds, 1f) : 1f;

        /// <summary>Показать значение без анимации: загрузка и старт - не «начисление»</summary>
        public void JumpTo(int value)
        {
            _from = value;
            _to = value;
            _seconds = 0f;
            _elapsed = 0f;
            Value = value;
        }

        /// <summary>
        /// Начать прокрутку к новому значению. Отсчёт всегда идёт от того, что видно
        /// на экране: изменение посреди прокрутки подхватывается без скачка
        /// </summary>
        public void RollTo(int target, float seconds)
        {
            if (seconds <= 0f)
            {
                JumpTo(target);
                return;
            }

            _from = Value;
            _to = target;
            _seconds = seconds;
            _elapsed = 0f;
        }

        /// <summary>Продвинуть прокрутку на прошедший кадр</summary>
        public void Advance(float deltaTime)
        {
            if (!IsRolling)
                return;

            _elapsed += deltaTime;

            if (_elapsed >= _seconds)
            {
                _elapsed = _seconds;
                Value = _to;
                return;
            }

            Value = _from + (int)Math.Round((_to - _from) * EaseOut(Progress));
        }

        /// <summary>Кубическое замедление: быстрый старт, мягкая остановка</summary>
        private static float EaseOut(float t)
        {
            float rest = 1f - t;
            return 1f - rest * rest * rest;
        }
    }
}
