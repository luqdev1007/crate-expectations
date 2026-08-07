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

        [Tooltip("Взмах, с. Клип взмаха подгоняется под это число скоростью стейта, " +
                 "а не наоборот - источник истины по темпу здесь")]
        [field: SerializeField][Min(0.05f)] public float AttackDuration { get; private set; } = 0.55f;

        [Header("Дуга взмаха")]
        [Tooltip("Чем машут вьюмоделью. Ассет отдельный: дуга - это числа кадра, " +
                 "а не оружия, и переживёт замену сабли на топор")]
        [field: SerializeField] public SwingDefinition Swing { get; private set; }

        [Header("Задел")]
        [Tooltip("Длина клинка от сокета до острия, м. На шаге 1 не используется - " +
                 "понадобится, когда у взмаха появится хитбокс")]
        [field: SerializeField][Min(0f)] public float BladeLength { get; private set; } = 0.76f;

        /// <summary>Тайминги в виде значения - то, с чем работает <see cref="WeaponStateMachine"/></summary>
        public WeaponTimings Timings => new(DrawDuration, SheatheDuration, AttackDuration);
    }
}
