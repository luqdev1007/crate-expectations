using System.Collections.Generic;

namespace CrateExpectations.Contracts
{
    /// <summary>
    /// Кто держит взятый заказ и считает по нему прогресс. Наблюдателям - доске и HUD -
    /// нужен только этот интерфейс; про то, что заказ продвигается по событию досмотра,
    /// они не знают
    /// </summary>
    public interface IContractManager
    {
        /// <summary>Взятый заказ и прогресс по нему. <c>Contract == null</c> - заказа нет</summary>
        ContractProgress Active { get; }

        /// <summary>Что ещё висит на доске: каталог за вычетом уже взятых заказов</summary>
        IReadOnlyList<ContractDefinition> Available { get; }

        /// <summary>Листок этого заказа уже сняли с доски - обратно он не вернётся</summary>
        bool IsTaken(ContractDefinition contract);

        /// <summary>Можно ли взять этот заказ прямо сейчас</summary>
        bool CanAccept(ContractDefinition contract);

        /// <summary>Взять заказ. <c>false</c> - взять было нельзя, ничего не изменилось</summary>
        bool Accept(ContractDefinition contract);

        /// <summary>Снять состояние для сохранения</summary>
        ContractSnapshot Capture();

        /// <summary>
        /// Вернуть состояние из сохранения. События приёма и прогресса при этом не летят:
        /// заказ не берут заново, его продолжают
        /// </summary>
        void Restore(in ContractSnapshot snapshot);
    }
}
