using System;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Economy;

namespace CrateExpectations.Persistence
{
    /// <summary>
    /// Файл сохранения целиком. Обычный сериализуемый класс: ни <c>MonoBehaviour</c>,
    /// ни <c>ScriptableObject</c>, ни одной ссылки на сцену - его можно собрать, записать
    /// и прочитать в edit-mode тесте.
    ///
    /// <para>Каждый модуль кладёт сюда <b>свой</b> снимок и сам его разбирает. Снимок
    /// собирается из готовых кусков, а не переписывается полями: добавить новую систему
    /// в сохранение - значит добавить сюда одно поле и не трогать ни одну из существующих.</para>
    /// </summary>
    [Serializable]
    public sealed class GameSnapshot
    {
        // 2: в снимок заказов добавлен список уже снятых с доски листков
        public const int CurrentVersion = 2;

        /// <summary>Версия формата, которой этот файл записан. 0 - поля в файле не было</summary>
        public int Version;

        /// <summary>Кошелёк</summary>
        public EconomySnapshot Economy;

        /// <summary>Взятый заказ и прогресс по нему</summary>
        public ContractSnapshot Contract;

        /// <summary>Груз на доке</summary>
        public CargoSceneSnapshot Cargo;

        /// <summary>Умеет ли текущая игра прочитать сейв такой версии</summary>
        public bool IsReadable => Version == CurrentVersion;
    }
}
