using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Расписание проверок попадания внутри одного взмаха: когда именно, в долях
    /// нормализованного времени атаки, положено пустить свип.
    /// <para>
    /// Почему это отдельный тип, а не два <c>if</c> в компоненте. Активное окно короткое -
    /// при окне 0.35..0.60 и взмахе 0.55 с это около 0.14 с, то есть восемь кадров на
    /// шестидесяти и три на двадцати. Одна проверка на кадр означает, что на просадке
    /// частоты цель между кадрами просто не проверят. Расписание задаёт число проверок
    /// от ВРЕМЕНИ АТАКИ, а не от частоты кадров: сколько сэмплов пропустил кадр, столько
    /// свипов в нём и случится. Результат перестаёт зависеть от того, как быстро идёт игра.
    /// </para>
    /// <para>
    /// Обычная структура без <see cref="MonoBehaviour"/> и без состояния: её можно
    /// прогнать тестом, не запуская сцену, - чем и покрыта.
    /// </para>
    /// </summary>
    public readonly struct SweepSchedule
    {
        private readonly float _start;
        private readonly float _end;
        private readonly int _samples;

        /// <param name="start">Начало активного окна, доля нормализованного времени атаки</param>
        /// <param name="end">Конец активного окна. Меньше начала - окно схлопывается в точку</param>
        /// <param name="samples">Сколько проверок разложить по окну. Меньше одной не бывает</param>
        public SweepSchedule(float start, float end, int samples)
        {
            _start = Mathf.Clamp01(start);
            _end = Mathf.Clamp(end, _start, 1f);
            _samples = Mathf.Max(1, samples);
        }

        /// <summary>Начало активного окна</summary>
        public float Start => _start;

        /// <summary>Конец активного окна</summary>
        public float End => _end;

        /// <summary>Сколько всего проверок в окне</summary>
        public int Samples => _samples;

        /// <summary>
        /// Время <paramref name="index"/>-й проверки. Крайние сэмплы лежат ровно на краях
        /// окна: первый кадр активной фазы и последний обязаны проверяться, иначе окно
        /// фактически уже заявленного
        /// </summary>
        public float SampleTime(int index)
        {
            if (_samples == 1)
                return _start;

            float t = Mathf.Clamp(index, 0, _samples - 1) / (float)(_samples - 1);

            return Mathf.Lerp(_start, _end, t);
        }

        /// <summary>
        /// Собирает времена проверок, попавших в интервал (<paramref name="previous"/>,
        /// <paramref name="current"/>] - то есть те, что кадр только что перешагнул.
        /// <para>
        /// Интервал полуоткрыт слева намеренно: соседние кадры не должны выстрелить одним
        /// и тем же сэмплом дважды. Чтобы самый первый сэмпл не потерялся, взмах начинают
        /// с <paramref name="previous"/> строго меньше <see cref="Start"/> -
        /// отрицательное значение подходит.
        /// </para>
        /// </summary>
        /// <param name="buffer">Куда писать. Лишние сэмплы отбрасываются, а не растят массив:
        /// вызов идёт покадрово, и аллокациям здесь не место</param>
        /// <returns>Сколько времён записано в начало <paramref name="buffer"/></returns>
        public int Collect(float previous, float current, float[] buffer)
        {
            if (buffer == null || buffer.Length == 0 || current <= previous)
                return 0;

            int count = 0;

            for (int i = 0; i < _samples && count < buffer.Length; i++)
            {
                float time = SampleTime(i);

                if (time > previous && time <= current)
                    buffer[count++] = time;
            }

            return count;
        }
    }
}
