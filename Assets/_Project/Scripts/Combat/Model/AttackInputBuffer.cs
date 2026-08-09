using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Короткая память на одно нажатие. Нажав в конце текущего удара, игрок имеет в виду
    /// «сразу следующим» - и без буфера это нажатие просто пропадает, потому что бить
    /// в этот момент ещё нельзя. Ощущается такая потеря не как правило боя, а как
    /// «игра не заметила клавишу», и лечится она памятью в десятые доли секунды.
    /// <para>
    /// Нажатие ровно одно: очередь здесь была бы хуже: зажав кнопку, игрок накопил бы
    /// серию, которая продолжает идти после того, как он отпустил.
    /// </para>
    /// <para>
    /// Обычный C#-класс без времени Unity внутри: время приходит аргументом
    /// <see cref="Tick"/>, и тест проверяет истечение буфера, не запуская сцену.
    /// </para>
    /// </summary>
    public sealed class AttackInputBuffer
    {
        private readonly float _duration;

        private float _remaining;

        /// <param name="duration">Сколько держать нажатие, с. Ноль отключает буфер</param>
        public AttackInputBuffer(float duration) => _duration = Mathf.Max(0f, duration);

        /// <summary>Есть ли неотработанное нажатие</summary>
        public bool HasPending => _remaining > 0f;

        /// <summary>
        /// Игрок нажал. Повторное нажатие продлевает память, а не добавляет второй удар:
        /// помним всегда только последнее
        /// </summary>
        public void Press() => _remaining = _duration;

        /// <summary>Нажатие отработано</summary>
        public void Consume() => _remaining = 0f;

        /// <summary>
        /// Забыть нажатие без отработки. Нужно там, где сама возможность бить пропала -
        /// например, оружие убрали: буфер не должен выстрелить ударом после того,
        /// как сабля уехала за пояс
        /// </summary>
        public void Clear() => _remaining = 0f;

        public void Tick(float deltaTime) => _remaining = Mathf.Max(0f, _remaining - deltaTime);
    }
}
