using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Перевод скорости игрока в пару чисел для блендтри ног: X - вбок, Y - вперёд,
    /// оба в долях от максимальной скорости. Обычный C#-класс без единой ссылки
    /// на <c>MonoBehaviour</c>: "куда и насколько быстро едет тело" - это правило,
    /// и проверять его перебором клавиш в play mode значило бы мерить походку на глаз.
    /// <para>
    /// Скорость приходит УЖЕ в локальных осях тела. Пересчёт из мировых - дело того,
    /// кто знает про трансформ; здесь он был бы лишней зависимостью и лишил бы тест
    /// возможности задать «бежит вбок» одним вектором.
    /// </para>
    /// <para>
    /// Сглаживание живёт здесь же, а не в блендтри: дерево смешивает позы, но само
    /// по себе мгновенно, и без сглаживания ноги переключались бы с покоя на полный
    /// бег за один кадр - ровно на том кадре, когда физика поставила скорость.
    /// </para>
    /// </summary>
    public sealed class LocomotionBlend
    {
        private Vector2 _current;

        // Служебное состояние SmoothDamp. Именно поле, а не локальная переменная:
        // без него каждый вызов начинал бы разгон заново и сглаживание не сглаживало бы
        private Vector2 _smoothVelocity;

        /// <summary>Текущая точка в дереве. То же, что вернул последний <see cref="Tick"/></summary>
        public Vector2 Current => _current;

        /// <summary>
        /// Подвинуть точку к той, которой соответствует текущая скорость
        /// </summary>
        /// <param name="localVelocity">Скорость тела в ЕГО осях, м/с</param>
        /// <param name="maxSpeed">Скорость, при которой ноги бегут в полную силу, м/с</param>
        /// <param name="smoothTime">За сколько примерно секунд точка доезжает до цели</param>
        /// <param name="deadband">
        /// Ниже какой доли максимума скорость считается нулевой. Не косметика: физика
        /// оставляет стоящему игроку остаточную скорость - его толкают ящики, он сползает
        /// с уклона, - и без мёртвой зоны ноги вечно перебирали бы на месте
        /// </param>
        /// <param name="deltaTime">Шаг времени, с</param>
        public Vector2 Tick(
            Vector3 localVelocity, float maxSpeed, float smoothTime, float deadband, float deltaTime)
        {
            Vector2 target = TargetOf(localVelocity, maxSpeed, deadband);

            // Мгновенная подстановка - это не «частный случай сглаживания», а его отсутствие:
            // SmoothDamp с нулевым временем делит на ноль
            if (smoothTime <= 0f || deltaTime <= 0f)
            {
                _current = target;
                _smoothVelocity = Vector2.zero;

                return _current;
            }

            _current = Vector2.SmoothDamp(
                _current, target, ref _smoothVelocity, smoothTime, Mathf.Infinity, deltaTime);

            return _current;
        }

        /// <summary>
        /// Вернуть ноги в покой немедленно, не тратя на это сглаживание. Нужно тем,
        /// кто телепортирует игрока: доезд к нулю из старой скорости выглядел бы
        /// пробежкой на новом месте
        /// </summary>
        public void Reset()
        {
            _current = Vector2.zero;
            _smoothVelocity = Vector2.zero;
        }

        /// <summary>
        /// Куда дерево должно приехать при этой скорости. Кламп по длине, а не покоординатно:
        /// по диагонали покоординатный дал бы длину 1.41 и увёл бы точку за край дерева,
        /// где ни одного клипа нет
        /// </summary>
        private static Vector2 TargetOf(Vector3 localVelocity, float maxSpeed, float deadband)
        {
            if (maxSpeed <= 0f)
                return Vector2.zero;

            var planar = new Vector2(localVelocity.x, localVelocity.z) / maxSpeed;

            if (planar.magnitude <= deadband)
                return Vector2.zero;

            return Vector2.ClampMagnitude(planar, 1f);
        }
    }
}
