using System;
using System.Collections.Generic;
using CrateExpectations.Cargo;

namespace CrateExpectations.Inventory
{
    /// <summary>
    /// Реестр груза, которым распоряжается игрок: что стоит на доке, во что оно сейчас
    /// одето и что уже сдано или отобрано. Наполняется событиями - сканировать сцену
    /// в поисках ящиков реестр не умеет и не должен
    /// </summary>
    public interface ICargoInventory
    {
        /// <summary>Все известные ящики, включая сданные и задержанные</summary>
        IReadOnlyList<CargoRecord> Records { get; }

        /// <summary>Сколько ящиков ещё на доке</summary>
        int OnDockCount { get; }

        /// <summary>Сколько сдано и принято</summary>
        int DeliveredCount { get; }

        /// <summary>Сколько задержано</summary>
        int SeizedCount { get; }

        /// <summary>Реестр изменился. Для UI: перерисоваться по событию, а не опрашивать каждый кадр</summary>
        event Action Changed;

        /// <summary>Найти запись по идентификатору ящика</summary>
        bool TryGet(int id, out CargoRecord record);

        /// <summary>Взять ящик на учёт. Повторная постановка того же ящика ничего не меняет</summary>
        void Register(int id, in CargoIdentity truth, in CargoState declared);

        /// <summary>Записать новое заявленное состояние - сработала станция маскировки</summary>
        void Redeclare(int id, in CargoState declared);

        /// <summary>Решить судьбу ящика: сдан или задержан. Дважды одну судьбу не пишут</summary>
        void Settle(int id, CargoStanding standing);

        /// <summary>Забыть всё - новая смена</summary>
        void Clear();
    }
}
