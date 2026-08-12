using UnityEngine;

namespace CrateExpectations.Interaction
{
    /// <summary>Режим удержания переносимого объекта. Меняется в ассете без правок кода</summary>
    public enum HoldMode
    {
        /// <summary>Ведём rigidbody к точке удержания через скорость (стабильно)</summary>
        VelocityHold,

        /// <summary>Присоединяем объект ConfigurableJoint'ом к кинематическому якорю</summary>
        ConfigurableJoint,
    }

    /// <summary>Параметры захвата/переноса/броска. Все числа - здесь</summary>
    [CreateAssetMenu(
        fileName = "CarryDefinition",
        menuName = "CrateExpectations/Interaction/Carry Definition")]
    public sealed class CarryDefinition : ScriptableObject
    {
        [Tooltip("Дистанция точки удержания перед камерой, м")]
        [field: SerializeField] public float HoldDistance { get; private set; } = 2f;

        [Tooltip("Доводка точки удержания в пространстве камеры: вправо/вверх/вперёд. " +
                 "Живёт здесь, а не в трансформе сцены, потому что подбирается в play mode: " +
                 "правка ассета переживает выход из него, правка сцены - нет. " +
                 "Ноль по умолчанию - точка стоит ровно перед прицелом, как и раньше")]
        [field: SerializeField] public Vector3 HoldOffset { get; private set; }

        [Tooltip("Ближе этого к камере точку удержания не подтягиваем, м")]
        [field: SerializeField] public float MinHoldDistance { get; private set; } = 0.6f;

        [Tooltip("Что перекрывает точку удержания: всё твёрдое, кроме игрока и слоя переноса. " +
                 "Без этого точка уезжает в пол при взгляде вниз, и груз дребезжит о землю.")]
        [field: SerializeField] public LayerMask HoldBlockingMask { get; private set; } = ~0;

        [Tooltip("Дальность луча захвата, м")]
        [field: SerializeField] public float GrabDistance { get; private set; } = 3f;

        [Tooltip("Сколько длится проводка руками к грузу, с. Физика её НЕ ждёт: груз " +
                 "прилипает к рукам в тот же кадр, а клип взятия просто ужимается " +
                 "до этого числа множителем скорости - тот же размен, что у ударов")]
        [field: SerializeField][Min(0.01f)] public float GrabDuration { get; private set; } = 0.3f;

        [Tooltip("Насколько объект может отстать от точки удержания, прежде чем выпасть из рук, м")]
        [field: SerializeField] public float BreakDistance { get; private set; } = 3f;

        [Tooltip("Скорость следования объекта за точкой удержания (VelocityHold)")]
        [field: SerializeField] public float FollowSpeed { get; private set; } = 12f;

        [Tooltip("Ограничение линейной скорости удерживаемого объекта, м/с")]
        [field: SerializeField] public float MaxVelocity { get; private set; } = 15f;

        [Tooltip("Сила броска вдоль forward камеры. Это НИЖНЯЯ граница: столько уходит " +
                 "в бросок, сделанный без замаха")]
        [field: SerializeField] public float ThrowForce { get; private set; } = 8f;

        [Tooltip("Угловое демпфирование удерживаемого объекта - гасит вращение при переносе")]
        [field: SerializeField] public float CarriedAngularDamping { get; private set; } = 8f;

        [Tooltip("Механика удержания. Переключается без правок кода")]
        [field: SerializeField] public HoldMode HoldMode { get; private set; } = HoldMode.VelocityHold;

        [Header("ConfigurableJoint (если выбран режим сустава)")]
        [Tooltip("Жёсткость пружины сустава, тянущей объект к точке удержания")]
        [field: SerializeField] public float JointSpring { get; private set; } = 1000f;

        [Tooltip("Демпфирование пружины сустава")]
        [field: SerializeField] public float JointDamper { get; private set; } = 50f;

        [Tooltip("Слои объектов, которые можно поднимать (грубый фильтр луча; " +
                 "решает всё равно наличие компонента Carriable).")]
        [field: SerializeField] public LayerMask CarriableMask { get; private set; } = ~0;

        [Header("Заряженный бросок")]
        [Tooltip("Мёртвая зона удержания, с. Нажатие короче этого не начинает замах " +
                 "и не приводит ни к какому броску - короткий клик обязан быть " +
                 "полностью безобидным, а не слабым тычком")]
        [field: SerializeField] public float ActivationThreshold { get; private set; } = 0.18f;

        [Tooltip("Сколько копить замах ПОСЛЕ мёртвой зоны до полного заряда, с")]
        [field: SerializeField] public float ChargeDuration { get; private set; } = 0.7f;

        [Tooltip("Сила броска при полном заряде. Верхняя граница; нижняя - ThrowForce")]
        [field: SerializeField] public float ChargedThrowForce { get; private set; } = 18f;

        [Tooltip("Перевод заряда (0..1) в долю силы и в глубину замаха. Линейная по " +
                 "умолчанию: кривой подкручивается ощущение, а не числа")]
        [field: SerializeField]
        public AnimationCurve ThrowCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Куда уезжает точка удержания при полном замахе, в пространстве камеры: " +
                 "вверх, над головой игрока. Раньше было вниз-назад, к плечу - но замах " +
                 "к плечу уводил груз ЗА камеру, где его не видно, и заряд читался только " +
                 "по тому, что ящик пропал. Подъём виден весь. " +
                 "Проверено замером: при (0, 0.45, 0) и HoldDistance 1.8 груз стоит в 1.86 м " +
                 "от глаза (было 1.31), ближняя грань в 1.45 м при near plane 0.1, " +
                 "верхняя грань в 0.80 м при полукадре 1.04 м - в кадре с запасом. " +
                 "Подтяжка SphereCast'ом считается ДО замаха и этим смещением не трогается")]
        [field: SerializeField]
        public Vector3 WindupOffset { get; private set; } = new Vector3(0f, 0.45f, 0f);

        [Tooltip("Пауза между началом анимации броска и вылетом груза, с. Больше не задел: " +
                 "анимация есть, и груз обязан уходить не по кнопке, а тогда, когда руки " +
                 "дошли до апекса выброса. Замер по клипу Carry_Throw (20 кадров, 30 fps, " +
                 "0.67 с): пик скорости кистей на 14-м кадре, то есть 0.47 с от начала. " +
                 "Ноль вернёт прежнее «улетает в тот же кадр»")]
        [field: SerializeField][Min(0f)] public float ReleaseDelay { get; private set; } = 0.47f;

        [Tooltip("Хвост занятости после вылета груза, с. Руки в кадре живут занятостью, " +
                 "и без хвоста они гасли бы в тот же кадр, когда стартует проводка броска - " +
                 "то есть анимацию броска не увидели бы ни разу. Инвариант точный, а не " +
                 "«с запасом»: ReleaseDelay + хвост == длина клипа Carry_Throw " +
                 "(0.47 + 0.1966667 = 0.6666667 с - 20 кадров при 30 fps). Число некруглое " +
                 "намеренно и округлению НЕ подлежит: точный хвост это 0.19666... в " +
                 "периоде, здесь он выписан до предела точности float, и любое " +
                 "«причёсывание» до 0.1967 или 0.20 вернёт инварианту остаток. " +
                 "Больше - руки лишнее время держат последний кадр клипа, " +
                 "меньше - уходят в стойку, не доиграв выброс")]
        [field: SerializeField][Min(0f)] public float ThrowTailDuration { get; private set; } = 0.1966667f;

        /// <summary>
        /// Заряд, пропущенный через кривую. Кламп здесь, а не у вызывающих: заряд приходит
        /// из модели удержания уже нормализованным, но кривую автор правит руками,
        /// и выход за 0..1 не должен превращаться в бросок неизвестной силы
        /// </summary>
        public float ThrowCurveAt(float chargeT) => ThrowCurve.Evaluate(Mathf.Clamp01(chargeT));

        /// <summary>
        /// Сила броска для заряда 0..1. Без заряда - ровно <see cref="ThrowForce"/>,
        /// при полном - ровно <see cref="ChargedThrowForce"/>
        /// </summary>
        public float ThrowForceAt(float chargeT) =>
            Mathf.Lerp(ThrowForce, ChargedThrowForce, ThrowCurveAt(chargeT));

        /// <summary>
        /// Смещение точки удержания для заряда 0..1, в пространстве камеры.
        /// Без заряда - ноль, то есть точка стоит там же, где стояла всегда
        /// </summary>
        public Vector3 WindupOffsetAt(float chargeT) => WindupOffset * ThrowCurveAt(chargeT);

        [Header("Слой переноса")]
        [Tooltip("Индекс слоя (Layer), на который переводится объект в руках. " +
                 "Этот слой должен быть исключён из столкновений со слоем игрока " +
                 "в матрице коллизий (Project Settings → Physics), иначе груз будет толкать игрока.")]
        [field: SerializeField] public int CarriedLayer { get; private set; }
    }
}
