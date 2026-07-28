using CrateExpectations.Cargo;

namespace CrateExpectations.Inventory
{
    /// <summary>Где сейчас ящик с точки зрения учёта</summary>
    public enum CargoStanding
    {
        /// <summary>Стоит на доке, им ещё можно распорядиться</summary>
        OnDock,

        /// <summary>Сдан и принят портом</summary>
        Delivered,

        /// <summary>Задержан инспектором - обратно его не отдадут</summary>
        Seized,
    }

    /// <summary>
    /// Одна строка реестра: обе личности ящика и что с ним стало. Значение, а не ссылка
    /// на <c>MonoBehaviour</c>, - поэтому реестр можно целиком прогнать в edit-mode тесте,
    /// а UI читает данные, не трогая сцену. Связь с настоящим ящиком держит
    /// <see cref="Id"/> (его <c>InstanceID</c>).
    /// </summary>
    public readonly struct CargoRecord
    {
        public CargoRecord(
            int id,
            in CargoIdentity truth,
            in CargoState declared,
            CargoStanding standing = CargoStanding.OnDock)
        {
            Id = id;
            Truth = truth;
            Declared = declared;
            Standing = standing;
        }

        /// <summary>Чем ящик опознаётся в реестре</summary>
        public int Id { get; }

        /// <summary>Что внутри на самом деле</summary>
        public CargoIdentity Truth { get; }

        /// <summary>Чем ящик себя объявляет сейчас</summary>
        public CargoState Declared { get; }

        /// <summary>Что с ящиком стало</summary>
        public CargoStanding Standing { get; }

        /// <summary>Ящик ещё на доке</summary>
        public bool IsOnDock => Standing == CargoStanding.OnDock;

        /// <summary>Заявленное разошлось с истиной - ящик выдают за что-то другое</summary>
        public bool IsDisguised => !Declared.MatchesTruth(Truth);

        /// <summary>Заявленное состояние сменилось (сработала станция маскировки)</summary>
        public CargoRecord Redeclared(in CargoState declared) =>
            new(Id, Truth, declared, Standing);

        /// <summary>Судьба ящика решена</summary>
        public CargoRecord Settled(CargoStanding standing) =>
            new(Id, Truth, Declared, standing);

        public override string ToString() => $"#{Id} {Truth} ({Standing}): {Declared}";
    }
}
