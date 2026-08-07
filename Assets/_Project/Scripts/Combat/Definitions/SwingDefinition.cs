using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Дуга взмаха, заданная кривыми, а не клипом. Отдельный ассет от
    /// <see cref="WeaponDefinition"/> по трём причинам:
    /// <list type="number">
    /// <item>это числа кадра, а не оружия. Дуга подбирается под то, как клинок читается
    /// на экране, и переживёт замену сабли на топор без единой правки;</item>
    /// <item>ассетов дуги со временем станет больше одного (лёгкий / тяжёлый / укол),
    /// а ассет оружия так и останется один - складывать их в одно поле значило бы
    /// раздувать оружие каждым новым приёмом;</item>
    /// <item>шесть <see cref="AnimationCurve"/> в инспекторе занимают экран целиком.
    /// В <see cref="WeaponDefinition"/> лежат тайминги и посадка - вещи, которые правят
    /// в другие моменты и другими руками, и терять их под кривыми не стоит.</item>
    /// </list>
    /// <para>
    /// Кривые нормированы по амплитуде: форма отдельно, сила отдельно. Подбирая размах,
    /// двигают <c>…Amplitude</c>, а кривую не трогают - иначе каждая правка силы стирает
    /// подобранный характер движения. Амплитуда со знаком: минус разворачивает ось,
    /// не переписывая кривую.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "SwingDefinition",
        menuName = "CrateExpectations/Combat/Swing Definition")]
    public sealed class SwingDefinition : ScriptableObject
    {
        [Header("Тумблер A/B")]
        [Tooltip("Вкл - дугу вьюмодели рисует ViewModelSwing по кривым отсюда, клип взмаха " +
                 "на вьюмодель не триггерится. Выкл - вьюмодель играет клип, как физическое " +
                 "тело. Физическое тело играет клип в обоих случаях: оно даёт тень")]
        [SerializeField] private bool _useProceduralSwing = true;

        [Header("Точка вращения (пространство камеры)")]
        [Tooltip("Вокруг чего проворачивается вьюмодель, м от глаза. Примерно плечо: " +
                 "назад и вправо от камеры. Вращение вокруг точки перед камерой выглядит " +
                 "как проворот предмета, вокруг точки за ней - как движение руки")]
        [SerializeField][Range(-1f, 1f)] private float _pivotX = 0.25f;

        [SerializeField][Range(-1f, 1f)] private float _pivotY = -0.2f;

        [SerializeField][Range(-1f, 1f)] private float _pivotZ = -0.3f;

        // Роли осей продиктованы геометрией, а не вкусом. Кисть в стойке лежит впереди
        // пивота примерно на 0.7 м и практически на его оси Z. Поворот вокруг Z (крен)
        // точку на самой оси не двигает - он только проворачивает клинок вокруг
        // собственного направления. Дугу в кадре несут те две оси, для которых у кисти
        // есть плечо рычага: тангаж (вверх-вниз) и рыскание (вправо-влево).
        // Диагональный руб - это тангаж вниз и рыскание влево одновременно
        [Header("Углы: форма (нормировано -1..1 по времени 0..1)")]
        [Tooltip("Тангаж. НЕСУЩАЯ ОСЬ: поднимает и опускает кисть. " +
                 "Внимание на знак: по соглашению Unity положительный тангаж кисть ОПУСКАЕТ, " +
                 "поэтому замах (подъём) лежит в минусе, а проводка (удар вниз) - в плюсе")]
        [SerializeField] private AnimationCurve _pitch = DefaultPitch();

        [Tooltip("Рыскание. НЕСУЩАЯ ОСЬ: уводит кисть вправо и влево. " +
                 "Плюс - вправо, минус - влево")]
        [SerializeField] private AnimationCurve _yaw = DefaultYaw();

        [Tooltip("Крен. ВТОРИЧНАЯ ОСЬ: кисть лежит на оси вращения и почти не двигается, " +
                 "меняется только ориентация лезвия в плоскости экрана. Амплитуда малая - " +
                 "большая здесь не добавляет размаха, а только вертит саблю в руке")]
        [SerializeField] private AnimationCurve _roll = DefaultRoll();

        [Header("Углы: амплитуда, градусы")]
        [SerializeField][Range(-180f, 180f)] private float _pitchAmplitude = 45f;

        [SerializeField][Range(-180f, 180f)] private float _yawAmplitude = 35f;

        [SerializeField][Range(-180f, 180f)] private float _rollAmplitude = 30f;

        [Header("Смещение: форма (нормировано -1..1 по времени 0..1)")]
        [Tooltip("Вправо-влево от камеры")]
        [SerializeField] private AnimationCurve _offsetX = DefaultOffsetX();

        [Tooltip("Вверх-вниз от камеры")]
        [SerializeField] private AnimationCurve _offsetY = DefaultOffsetY();

        [Tooltip("Вперёд-назад от камеры. Отвод на замахе и выпад на ударе - " +
                 "то, что читается как вес удара")]
        [SerializeField] private AnimationCurve _offsetZ = DefaultOffsetZ();

        [Header("Смещение: амплитуда, м")]
        [SerializeField][Range(-0.5f, 0.5f)] private float _offsetXAmplitude = 0.04f;

        [SerializeField][Range(-0.5f, 0.5f)] private float _offsetYAmplitude = 0.04f;

        [SerializeField][Range(-0.5f, 0.5f)] private float _offsetZAmplitude = 0.2f;

        /// <summary>Процедурная дуга (<c>true</c>) или клип взмаха на вьюмодели (<c>false</c>)</summary>
        public bool UseProceduralSwing => _useProceduralSwing;

        /// <summary>Точка вращения в пространстве камеры</summary>
        public Vector3 Pivot => new(_pivotX, _pivotY, _pivotZ);

        /// <summary>
        /// Поворот вьюмодели на нормализованном времени взмаха <paramref name="t"/>, градусы.
        /// Порядок осей - обычный для Unity Эйлер: Z (крен), X (тангаж), Y (рыскание)
        /// </summary>
        public Vector3 EvaluateAngles(float t) => new(
            _pitch.Evaluate(t) * _pitchAmplitude,
            _yaw.Evaluate(t) * _yawAmplitude,
            _roll.Evaluate(t) * _rollAmplitude);

        /// <summary>Смещение вьюмодели на нормализованном времени взмаха <paramref name="t"/>, м</summary>
        public Vector3 EvaluateOffset(float t) => new(
            _offsetX.Evaluate(t) * _offsetXAmplitude,
            _offsetY.Evaluate(t) * _offsetYAmplitude,
            _offsetZ.Evaluate(t) * _offsetZAmplitude);

        // Заготовки ниже - это не "настройки по умолчанию", а рабочая точка, с которой
        // начинают подбор. Держим их в коде, а не в ассете, чтобы форма дуги читалась
        // в ревью: по ассету с кривыми не видно ничего, кроме того, что кривые там есть.
        //
        // Фазы едины для всех осей: замах 0..0.42, активная 0.42..0.60, возврат 0.60..1.
        // Касательные в активной фазе линейные - резкий рывок; в замахе и возврате
        // сглаженные - оружие разгоняется и тормозит, а не телепортируется

        private const float WindupEnd = 0.42f;
        private const float ActiveEnd = 0.60f;

        /// <summary>
        /// Тангаж: замах поднимает кисть, проводка роняет её вниз. Полный ход - две
        /// амплитуды, от -0.78 до +1. Знак обратный интуиции, потому что положительный
        /// тангаж по Unity кисть опускает
        /// </summary>
        private static AnimationCurve DefaultPitch() => Build(-0.78f, 1f, -0.12f);

        /// <summary>Рыскание: замах уводит кисть вправо, проводка проносит её влево через кадр</summary>
        private static AnimationCurve DefaultYaw() => Build(0.71f, -1f, 0.12f);

        /// <summary>Крен: доворот лезвия. Плоскость реза, а не размах</summary>
        private static AnimationCurve DefaultRoll() => Build(-0.67f, 1f, -0.1f);

        /// <summary>Мелкий увод вправо на замахе и влево на проводке, вслед за рысканием</summary>
        private static AnimationCurve DefaultOffsetX() => Build(0.75f, -1f, 0.1f);

        /// <summary>Мелкий подъём на замахе и провал на ударе, вслед за тангажом</summary>
        private static AnimationCurve DefaultOffsetY() => Build(1f, -0.8f, 0.12f);

        /// <summary>
        /// Только выпад вперёд на ударе. Отвода назад на замахе здесь намеренно нет:
        /// пивот стоит позади камеры, поэтому подъём тангажом сам по себе утаскивает кисть
        /// назад примерно на четверть метра - из 0.40 м от глаза остаётся 0.15. Явный отвод
        /// поверх этого клал кисть ровно в плоскость объектива, и вьюмодель на пике замаха
        /// пропадала из кадра целиком. Отвод в этой посадке уже сделан тангажом; ось Z нужна
        /// ровно на выпад
        /// </summary>
        private static AnimationCurve DefaultOffsetZ() => Build(0f, 1f, -0.1f);

        /// <summary>
        /// Кривая из пяти ключей: покой - конец замаха - конец активной фазы - перелёт - покой.
        /// <paramref name="overshoot"/> - перелёт через ноль на возврате: без него рука
        /// приезжает в стойку и встаёт как вкопанная, с ним у удара появляется вес
        /// </summary>
        private static AnimationCurve Build(float windup, float active, float overshoot)
        {
            // Ключ перелёта ставим в первой трети возврата: перелёт должен успеть
            // отыграться до конца взмаха, а не оборваться на нём
            const float OvershootTime = ActiveEnd + (1f - ActiveEnd) * 0.35f;

            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(WindupEnd, windup),
                new Keyframe(ActiveEnd, active),
                new Keyframe(OvershootTime, overshoot),
                new Keyframe(1f, 0f));

            // Возврат сглаживаем: рука тормозит, а не телепортируется
            curve.SmoothTangents(3, 0f);
            curve.SmoothTangents(4, 0f);

            // Старт и вершина замаха - плоские. Плоский старт даёт "медленно" в начале,
            // плоская вершина - паузу на изготовке: рука доходит до верхней точки,
            // на мгновение замирает и оттуда падает. Это классическая антиципация,
            // и именно она делает последующий рывок читаемым
            SetTangents(curve, index: 0, inTangent: 0f, outTangent: 0f);
            SetTangents(curve, index: 1, inTangent: 0f, outTangent: 0f);

            // А удар - наоборот. Касательные линейные, по хорде к соседям: кривая входит
            // в пик активной фазы и выходит из него по прямой, без разгона и без выката.
            // Сглаженный тут ключ превратил бы рубящий удар в мягкое помахивание
            SetTangents(curve, index: 2,
                inTangent: (active - windup) / (ActiveEnd - WindupEnd),
                outTangent: (overshoot - active) / (OvershootTime - ActiveEnd));

            return curve;
        }

        private static void SetTangents(AnimationCurve curve, int index, float inTangent, float outTangent)
        {
            Keyframe key = curve[index];
            key.inTangent = inTangent;
            key.outTangent = outTangent;
            curve.MoveKey(index, key);
        }
    }
}
