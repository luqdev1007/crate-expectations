using CrateExpectations.Cargo;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Что лежит перед инспектором: заявленное состояние груза, истина о нём и требования
    /// порта к заявленному содержимому. Значение неизменяемое - оценщик может только читать,
    /// переписать истину ему нечем
    /// </summary>
    public readonly struct InspectionSubject
    {
        public InspectionSubject(
            in CargoState declared,
            in CargoIdentity truth,
            PaintDefinition expectedPaint = null,
            StampDefinition requiredStamp = null)
        {
            Declared = declared;
            Truth = truth;
            ExpectedPaint = expectedPaint;
            RequiredStamp = requiredStamp;
        }

        /// <summary>Как груз выглядит и чем себя объявляет</summary>
        public CargoState Declared { get; }

        /// <summary>Что внутри на самом деле. Только для чтения</summary>
        public CargoIdentity Truth { get; }

        /// <summary>Окраска, положенная заявленному содержимому. <c>null</c> - требований нет</summary>
        public PaintDefinition ExpectedPaint { get; }

        /// <summary>Пломба, положенная заявленному содержимому. <c>null</c> - требований нет</summary>
        public StampDefinition RequiredStamp { get; }
    }
}
