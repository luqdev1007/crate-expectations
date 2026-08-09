using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Всё про одно оружие: что вешать в руку, как оно там стоит и в каком темпе им машут.
    /// Чисел фехтования в коде нет - они здесь
    /// </summary>
    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "CrateExpectations/Combat/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        // Прямая ссылка, а не Addressables-ключ - осознанно, вопреки тому, как грузится груз.
        // Груз ходит через каталог потому, что его типов много, набор меняется от контракта
        // к контракту и держать всё в памяти незачем. Оружие противоположно по всем трём
        // пунктам: оно одно, нужно с первого кадра боя и живёт всю сессию. Асинхронная
        // загрузка тут не экономит память, зато добавляет кадр ожидания ровно в тот момент,
        // когда игрок нажал "достать". Плюс в бандл уходит копия ассета, и сравнение
        // ссылок на префаб перестаёт работать в билде - для оружия это лишний риск даром
        [Tooltip("Префаб оружия. Инстанцируется один раз при старте и просто прячется, " +
                 "пока оружие убрано")]
        [field: SerializeField] public GameObject Prefab { get; private set; }

        // Стоит первым и слайдером намеренно: это единственное число оружия, которое крутят
        // подбором, а не выставляют один раз. Ниже по ассету лежат две посадки, каждая
        // в семь полей, и темп удара из-за них уезжал под сгиб
        [Header("Темп удара")]
        [Tooltip("Взмах, с. Клип взмаха подгоняется под это число скоростью стейта, " +
                 "а не наоборот - источник истины по темпу здесь. Читается в Awake: " +
                 "правка на ходу подхватится со следующего входа в play mode")]
        [field: SerializeField][Range(0.05f, 2f)] public float AttackDuration { get; private set; } = 0.55f;

        // Посадок две, и они независимы намеренно. У физического тела задача анатомическая:
        // сабля должна выглядеть зажатой в кулаке, потому что её видно в тени на земле и
        // по ней же будут читать позу игрока NPC. У вьюмодели задачи анатомической нет вовсе -
        // её никто, кроме камеры, не видит, и клинок там ставят так, как он лучше лежит
        // в кадре. Одно поле на обе посадки означало бы, что подбор кадра ломает тень
        [Header("Посадка на физическом теле")]
        [Tooltip("Как сабля лежит в ладони мировой модели. Видна в тени, не в кадре")]
        [field: SerializeField] public WeaponFit BodySocket { get; private set; }

        [Header("Посадка на вьюмодели")]
        [Tooltip("Как сабля лежит в ладони модели под камерой. Видна в кадре, не в тени")]
        [field: SerializeField] public WeaponFit ViewModelSocket { get; private set; }

        [Header("Тайминги")]
        [Tooltip("Доставание, с. Оружие появляется в руке на середине этого времени")]
        [field: SerializeField][Min(0f)] public float DrawDuration { get; private set; } = 0.25f;

        [Tooltip("Убирание, с. Оружие исчезает из руки на середине этого времени")]
        [field: SerializeField][Min(0f)] public float SheatheDuration { get; private set; } = 0.25f;

        [Header("Дуга взмаха")]
        [Tooltip("Чем машут вьюмоделью. Ассет отдельный: дуга - это числа кадра, " +
                 "а не оружия, и переживёт замену сабли на топор")]
        [field: SerializeField] public SwingDefinition Swing { get; private set; }

        // Свип идёт из камеры, а не по клинку вьюмодели, и это не упрощение. Вьюмодель
        // врёт про положение в мире: она висит под камерой, живёт в своём объективе и
        // подобрана под то, как красиво лежит в кадре, - её клинок физически находится
        // в паре десятков сантиметров от глаза, а не там, где его видит игрок. Детекция
        // обязана быть привязана к тому, КУДА ИГРОК ЦЕЛИТСЯ, иначе попадание расходится
        // с прицелом ровно на величину подгонки кадра
        [Header("Свип: объём")]
        [Tooltip("Радиус капсулы свипа, м")]
        [field: SerializeField][Range(0.05f, 1f)] public float SweepRadius { get; private set; } = 0.35f;

        [Tooltip("Дальность свипа от камеры, м")]
        [field: SerializeField][Range(0.5f, 5f)] public float SweepDistance { get; private set; } = 2.2f;

        [Tooltip("Высота капсулы вдоль вертикали камеры, м. Ноль превращает капсулу " +
                 "в сферу - ровно тот объём, что задан радиусом. Поднимать, если удар " +
                 "должен доставать цель другого роста, а не чтобы усилить попадания")]
        [field: SerializeField][Range(0f, 2f)] public float SweepHeight { get; private set; }

        [Tooltip("По кому бьём. Слои цели, не всё подряд: свип не должен цеплять самого " +
                 "игрока и то, что он несёт в руках")]
        [field: SerializeField] public LayerMask HitMask { get; private set; } = ~0;

        [Header("Свип: окно")]
        [Tooltip("Начало активной фазы, доля времени взмаха. До неё удар не считается")]
        [field: SerializeField][Range(0f, 1f)] public float SweepStart { get; private set; } = 0.35f;

        [Tooltip("Конец активной фазы, доля времени взмаха")]
        [field: SerializeField][Range(0f, 1f)] public float SweepEnd { get; private set; } = 0.6f;

        [Tooltip("Сколько проверок разложить по активному окну. Одна проверка на кадр " +
                 "означала бы, что на просадке частоты цель проскочит между кадрами")]
        [field: SerializeField][Range(1, 16)] public int SweepSamples { get; private set; } = 6;

        [Header("Удар")]
        [Tooltip("Урон за одно попадание")]
        [field: SerializeField][Min(0)] public int Damage { get; private set; } = 1;

        [Tooltip("Импульс отбрасывания, Н·с. Замерено: ящик массой 4 кг от 6 Н·с трогается " +
                 "с 1.75 м/с, проезжает 0.7 м и заваливается набок")]
        [field: SerializeField][Min(0f)] public float Impulse { get; private set; } = 6f;

        // Звуки лежат у оружия, а не в ассете фидбека: свист сабли и свист топора -
        // это разные звуки, а вот заморозка и тычок камеры у них общие
        [Header("Звук")]
        [Tooltip("Свист на старте взмаха. Играется с разбросом высоты тона")]
        [field: SerializeField] public AudioClip SwingSound { get; private set; }

        [Tooltip("Удар на контакте. Играется всегда одной высотой тона - " +
                 "он должен читаться как событие, а не как продолжение свиста")]
        [field: SerializeField] public AudioClip HitSound { get; private set; }

        [Header("Отдача кадра")]
        [Tooltip("Заморозка, тычок камеры и отдача вьюмодели. Ассет отдельный: " +
                 "это ощущение удара вообще, а не свойство конкретной сабли")]
        [field: SerializeField] public ImpactFeedbackDefinition Feedback { get; private set; }

        [Header("Задел")]
        [Tooltip("Длина клинка от сокета до острия, м. Дальность удара задаёт не она, " +
                 "а SweepDistance: клинок вьюмодели в мире стоит не там, где выглядит")]
        [field: SerializeField][Min(0f)] public float BladeLength { get; private set; } = 0.76f;

        /// <summary>Тайминги в виде значения - то, с чем работает <see cref="WeaponStateMachine"/></summary>
        public WeaponTimings Timings => new(DrawDuration, SheatheDuration, AttackDuration);

        /// <summary>Расписание проверок попадания внутри взмаха</summary>
        public SweepSchedule Sweep => new(SweepStart, SweepEnd, SweepSamples);
    }
}
