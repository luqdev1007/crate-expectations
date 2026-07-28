namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Во сколько инспектор оценивает каждую улику. Поля именованные, а не словарь:
    /// таблица маленькая и известна на этапе компиляции, а обращение к ней не должно
    /// ни аллоцировать, ни зависеть от порядка элементов в ассете
    /// </summary>
    public readonly struct ClueWeights
    {
        public ClueWeights(
            float declaredContraband,
            float contentMismatch,
            float paintMismatch,
            float missingStamp,
            float wrongStamp,
            float incompleteDisguise)
        {
            DeclaredContraband = declaredContraband;
            ContentMismatch = contentMismatch;
            PaintMismatch = paintMismatch;
            MissingStamp = missingStamp;
            WrongStamp = wrongStamp;
            IncompleteDisguise = incompleteDisguise;
        }

        public float DeclaredContraband { get; }
        public float ContentMismatch { get; }
        public float PaintMismatch { get; }
        public float MissingStamp { get; }
        public float WrongStamp { get; }
        public float IncompleteDisguise { get; }

        /// <summary>Вес улики этого типа</summary>
        public float Of(ClueType type) => type switch
        {
            ClueType.DeclaredContraband => DeclaredContraband,
            ClueType.ContentMismatch => ContentMismatch,
            ClueType.PaintMismatch => PaintMismatch,
            ClueType.MissingStamp => MissingStamp,
            ClueType.WrongStamp => WrongStamp,
            ClueType.IncompleteDisguise => IncompleteDisguise,
            _ => 0f,
        };
    }

    /// <summary>
    /// Правила досмотра в виде значения: что инспектор проверяет, как сильно нервничает
    /// по каждому поводу, когда задерживает груз и насколько он невнимателен.
    /// Снимается с <see cref="InspectorProfile"/> - ядро не зависит от ScriptableObject-обвязки
    /// </summary>
    public readonly struct InspectionPolicy
    {
        public InspectionPolicy(
            ClueChecks checks,
            in ClueWeights weights,
            float suspicionThreshold,
            float overlookChance = 0f)
        {
            Checks = checks;
            Weights = weights;
            SuspicionThreshold = suspicionThreshold;
            OverlookChance = overlookChance;
        }

        /// <summary>Какие проверки выполняются</summary>
        public ClueChecks Checks { get; }

        /// <summary>Веса улик</summary>
        public ClueWeights Weights { get; }

        /// <summary>С какого уровня подозрения груз задерживают</summary>
        public float SuspicionThreshold { get; }

        /// <summary>Шанс пропустить отдельную улику, 0..1. Ноль - инспектор не ошибается</summary>
        public float OverlookChance { get; }

        /// <summary>Выполняет ли инспектор такую проверку</summary>
        public bool Performs(ClueChecks check) => (Checks & check) != 0;
    }
}
