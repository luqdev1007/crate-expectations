using System;

namespace CrateExpectations.Contracts
{
    /// <summary>
    /// Взятый заказ на диске. Ассет здесь не ссылкой, а именем: ссылку в JSON не положить,
    /// а сохранение должно пережить и перезапуск редактора, и пересборку Addressables.
    /// Разрешает имя обратно в ассет сам <see cref="ContractManager"/> - по своему каталогу,
    /// потому что больше никто не знает, какие заказы бывают.
    ///
    /// <para>Пустой <see cref="ContractId"/> - активного заказа не было. Это нормальное
    /// состояние, а не потеря данных.</para>
    /// </summary>
    [Serializable]
    public struct ContractSnapshot
    {
        /// <summary>Имя ассета заказа или пустая строка, если заказа нет</summary>
        public string ContractId;

        /// <summary>Сколько ящиков принято</summary>
        public int Delivered;

        /// <summary>Сколько ящиков задержано</summary>
        public int Seized;

        /// <summary>
        /// Заказы, листки которых уже сняли с доски. Обратно они не возвращаются никогда,
        /// поэтому список переживает сохранение наравне с активным заказом
        /// </summary>
        public string[] TakenIds;
    }
}
