using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Что происходит с кадром в момент касания: заморозка, тычок камеры, отдача вьюмодели
    /// и что именно вспыхивает в точке удара.
    /// <para>
    /// Ассет отдельный от <see cref="WeaponDefinition"/> по той же причине, по которой
    /// отдельна <see cref="SwingDefinition"/>: это числа ОЩУЩЕНИЯ, а не свойство сабли.
    /// Заморозка на 0.06 с и тычок в полтора градуса останутся теми же, когда саблю
    /// сменят на топор, - меняться будут импульс, урон и звук, и они лежат у оружия.
    /// </para>
    /// <para>
    /// Кривые нормированы: форма отдельно, сила отдельно. Подбирая размах, двигают
    /// амплитуду и кривую не трогают - иначе каждая правка силы стирает подобранный
    /// характер движения.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "ImpactFeedbackDefinition",
        menuName = "CrateExpectations/Combat/Impact Feedback Definition")]
    public sealed class ImpactFeedbackDefinition : ScriptableObject
    {
        [Header("Заморозка")]
        [Tooltip("Сколько держать время на попадании, с реального времени. " +
                 "Это самая чувствительная величина во всём ударе: 0.03 не замечают, " +
                 "0.12 читается как лаг")]
        [SerializeField][Range(0f, 0.3f)] private float _hitStopDuration = 0.06f;

        [Tooltip("Масштаб времени во время заморозки. Ноль - полная остановка. " +
                 "Малое ненулевое значение даёт не стоп, а вязкость")]
        [SerializeField][Range(0f, 1f)] private float _hitStopScale;

        // Направление тычка задано ПРОТИВ дуги удара. Клинок на активной фазе идёт
        // вниз и влево (см. SwingDefinition), поэтому камера отдаёт вверх и вправо:
        // это читается как сопротивление цели, а не как второй замах
        [Header("Тычок камеры")]
        [Tooltip("Тангаж, градусы. Минус поднимает взгляд - удар идёт вниз, " +
                 "значит камера отдаёт вверх")]
        [SerializeField][Range(-10f, 10f)] private float _cameraPunchPitch = -1.5f;

        [Tooltip("Рыскание, градусы. Плюс уводит вправо - удар идёт влево")]
        [SerializeField][Range(-10f, 10f)] private float _cameraPunchYaw = 0.5f;

        [Tooltip("Длительность тычка, с")]
        [SerializeField][Range(0.02f, 1f)] private float _cameraPunchDuration = 0.15f;

        [Tooltip("Затухание тычка: 1 в начале, 0 в конце. Провал ниже нуля - " +
                 "возврат с перелётом, он и делает движение живым")]
        [SerializeField] private AnimationCurve _cameraPunchFalloff = DefaultFalloff();

        [Header("Отдача вьюмодели")]
        [Tooltip("Насколько руки уходят назад от камеры на контакте, м")]
        [SerializeField][Range(0f, 0.3f)] private float _viewModelKick = 0.05f;

        [Tooltip("Длительность отдачи, с. Короче тычка камеры: руки должны вернуться " +
                 "в стойку раньше, чем закончится взмах")]
        [SerializeField][Range(0.02f, 1f)] private float _viewModelKickDuration = 0.12f;

        [SerializeField] private AnimationCurve _viewModelKickFalloff = DefaultFalloff();

        [Header("Частицы")]
        [Tooltip("Искры. Живут доли секунды, разлетаются из точки касания")]
        [SerializeField] private ParticleSystem _sparksPrefab;

        [Tooltip("След на поверхности. Живёт секунды, лежит по нормали")]
        [SerializeField] private ParticleSystem _markPrefab;

        [Tooltip("Сколько экземпляров каждого эффекта создать заранее. Instantiate " +
                 "случается на разогреве, а не в момент удара")]
        [SerializeField][Range(1, 32)] private int _effectPrewarm = 6;

        /// <summary>Длительность заморозки на попадании, с</summary>
        public float HitStopDuration => _hitStopDuration;

        /// <summary>Масштаб времени во время заморозки</summary>
        public float HitStopScale => _hitStopScale;

        /// <summary>Тычок камеры как углы (тангаж, рыскание), градусы</summary>
        public Vector2 CameraPunchAngles => new(_cameraPunchPitch, _cameraPunchYaw);

        /// <summary>Длительность тычка камеры, с</summary>
        public float CameraPunchDuration => _cameraPunchDuration;

        /// <summary>Отдача вьюмодели назад, м</summary>
        public float ViewModelKick => _viewModelKick;

        /// <summary>Длительность отдачи вьюмодели, с</summary>
        public float ViewModelKickDuration => _viewModelKickDuration;

        /// <summary>Префаб искр</summary>
        public ParticleSystem SparksPrefab => _sparksPrefab;

        /// <summary>Префаб следа на поверхности</summary>
        public ParticleSystem MarkPrefab => _markPrefab;

        /// <summary>Размер разогрева пула на каждый эффект</summary>
        public int EffectPrewarm => _effectPrewarm;

        /// <summary>Множитель тычка камеры на нормализованном времени <paramref name="t"/></summary>
        public float EvaluateCameraPunch(float t) => _cameraPunchFalloff.Evaluate(t);

        /// <summary>Множитель отдачи вьюмодели на нормализованном времени <paramref name="t"/></summary>
        public float EvaluateViewModelKick(float t) => _viewModelKickFalloff.Evaluate(t);

        /// <summary>
        /// Рабочая точка, с которой начинают подбор, а не "настройки по умолчанию".
        /// Держим в коде, а не в ассете, чтобы форма читалась в ревью: по ассету с кривой
        /// не видно ничего, кроме того, что кривая там есть.
        /// <para>
        /// Мгновенный удар в начале (плоская вершина - это не разгон, а уже случившийся
        /// толчок), быстрый спад через ноль в небольшой перелёт и мягкий возврат.
        /// Без перелёта камера приезжает на место и встаёт как вкопанная.
        /// </para>
        /// </summary>
        private static AnimationCurve DefaultFalloff()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.45f, -0.18f),
                new Keyframe(1f, 0f));

            curve.SmoothTangents(1, 0f);
            curve.SmoothTangents(2, 0f);

            // Старт плоский: толчок уже произошёл к нулевому кадру, разгоняться неоткуда
            Keyframe start = curve[0];
            start.inTangent = 0f;
            start.outTangent = 0f;
            curve.MoveKey(0, start);

            return curve;
        }
    }
}
