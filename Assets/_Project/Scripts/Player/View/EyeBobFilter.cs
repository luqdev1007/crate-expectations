using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Сколько глаз игрока проезжает вслед за костью груди. Обычный C#-класс: «насколько
    /// сильно камера повторяет качку тела» - это правило, и проверять его тряской головы
    /// в play mode значило бы мерить укачивание на глаз.
    /// <para>
    /// На вход приходит ОТКЛОНЕНИЕ кости от её покоя, в осях тела, а не мировая позиция
    /// глаза. Это не мелочь: сглаживать мировую позицию нельзя, иначе камера будет
    /// отставать от идущего игрока и тащиться за ним на верёвке. Отклонение же около
    /// нуля всегда, и сглаживается ровно то, что нужно, - качка.
    /// </para>
    /// </summary>
    public sealed class EyeBobFilter
    {
        private Vector3 _current;
        private Vector3 _smoothVelocity;

        /// <summary>Текущее отклонение глаза от анатомической точки, в осях тела</summary>
        public Vector3 Current => _current;

        /// <summary>
        /// Подвинуть глаз к тому отклонению, которого требует поза
        /// </summary>
        /// <param name="boneDeviation">
        /// Куда уехала кость груди от своего покоя, в осях тела, м
        /// </param>
        /// <param name="follow">
        /// Доля качки, которую камера повторяет: 0 - неподвижная камера, как было
        /// до кости груди, 1 - глаз жёстко сидит в груди. Кламп здесь, а не у
        /// вызывающего: число подбирают руками, и значение больше единицы означало бы
        /// камеру, которую качает СИЛЬНЕЕ, чем само тело
        /// </param>
        /// <param name="smoothTime">За сколько примерно секунд глаз доезжает до цели</param>
        /// <param name="deltaTime">Шаг времени, с</param>
        public Vector3 Tick(Vector3 boneDeviation, float follow, float smoothTime, float deltaTime)
        {
            Vector3 target = boneDeviation * Mathf.Clamp01(follow);

            // Мгновенная подстановка - это не частный случай сглаживания, а его отсутствие:
            // SmoothDamp с нулевым временем делит на ноль
            if (smoothTime <= 0f || deltaTime <= 0f)
            {
                _current = target;
                _smoothVelocity = Vector3.zero;

                return _current;
            }

            _current = Vector3.SmoothDamp(
                _current, target, ref _smoothVelocity, smoothTime, Mathf.Infinity, deltaTime);

            return _current;
        }

        /// <summary>
        /// Вернуть глаз в анатомическую точку немедленно. Нужно тем, кто телепортирует
        /// игрока: доезд к нулю из старого отклонения выглядел бы качкой на новом месте
        /// </summary>
        public void Reset()
        {
            _current = Vector3.zero;
            _smoothVelocity = Vector3.zero;
        }
    }
}
