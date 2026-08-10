using System;

namespace CrateExpectations.Core.Hands
{
    /// <summary>
    /// Единая точка правды о том, свободны ли руки. Занятость здесь ВЫЧИСЛЯЕТСЯ по трём
    /// источникам, а не хранится: у переноски, оружия и листка заказа уже есть своё
    /// состояние, и четвёртая копия рано или поздно разъехалась бы с ними - груз выпадает
    /// из рук сам, удар кончается по таймеру, листок опускается по событию шины,
    /// и ни одно из этих мест не обязано помнить про общий флаг.
    /// <para>
    /// Обычный C#-класс без единой ссылки на <c>MonoBehaviour</c>: правила занятости -
    /// это логика, и проверяться она должна таблицей случаев в edit-mode тесте,
    /// а не перебором клавиш в play mode.
    /// </para>
    /// <para>
    /// Класс отвечает на вопросы, но ничего не запрещает сам: гейты стоят у тех, кто
    /// обрабатывает нажатия. Иначе <see cref="HandsState"/> пришлось бы знать про ввод,
    /// про бой и про UI одновременно.
    /// </para>
    /// </summary>
    public sealed class HandsState
    {
        private readonly ICarryStateSource _carry;
        private readonly IWeaponStateSource _weapon;
        private readonly IContractViewSource _contract;

        /// <summary>
        /// Источники обязательны все три. Молча подставлять «ничем не занято» вместо
        /// забытого источника нельзя: получилась бы модель, которая всегда разрешает
        /// всё, и незакрытый гейт нашёлся бы уже руками в игре
        /// </summary>
        public HandsState(
            ICarryStateSource carry, IWeaponStateSource weapon, IContractViewSource contract)
        {
            _carry = carry ?? throw new ArgumentNullException(nameof(carry));
            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            _contract = contract ?? throw new ArgumentNullException(nameof(contract));
        }

        /// <summary>
        /// Чем заняты руки. Порядок проверок - это приоритет, и он не случаен:
        /// груз держат обеими руками и он тяжелее всего остального; листок поверх
        /// оружия читается как «убрал клинок, чтобы посмотреть»; оружие - самое лёгкое
        /// из занятий, из него игрок выходит одной кнопкой
        /// </summary>
        public HandsOccupancy Occupancy
        {
            get
            {
                if (_carry.IsCarrying)
                    return HandsOccupancy.Carrying;

                if (_contract.IsRaised)
                    return HandsOccupancy.Reading;

                // Занятость боем шире, чем «сабля в руке»: заряд можно копить и с убранным
                // оружием, и это тоже занятые руки
                if (_weapon.IsWeaponDrawn || _weapon.IsBusy)
                    return HandsOccupancy.Combat;

                return HandsOccupancy.Free;
            }
        }

        /// <summary>Можно ли поднять груз. Только пустыми руками - ни поверх сабли, ни поверх листка</summary>
        public bool CanGrab => Occupancy == HandsOccupancy.Free;

        /// <summary>
        /// Можно ли положить или швырнуть груз. Ровно тогда, когда он в руках: выпустить
        /// уже взятое игроку не должно мешать ничто, иначе занятые руки оставили бы ящик
        /// приклеенным навсегда
        /// </summary>
        public bool CanDropOrThrow => Occupancy == HandsOccupancy.Carrying;

        /// <summary>Можно ли ударить. Пустыми руками (удар достаёт саблю по ходу) или уже с саблей</summary>
        public bool CanAttack =>
            Occupancy == HandsOccupancy.Free || Occupancy == HandsOccupancy.Combat;

        /// <summary>Можно ли поднять листок заказа. Только свободными руками</summary>
        public bool CanRaiseContract => Occupancy == HandsOccupancy.Free;

        /// <summary>Можно ли достать или убрать оружие</summary>
        public bool CanToggleWeapon =>
            Occupancy == HandsOccupancy.Free || Occupancy == HandsOccupancy.Combat;

        /// <summary>
        /// Можно ли тянуться рукой к миру - подобрать груз или нажать на станцию.
        /// Смотрит не на занятость, а на ЗАНЯТОСТЬ ДЕЙСТВИЕМ: с саблей в опущенной руке
        /// игрок дотянуться может, а посреди замаха, блока или доставания - нет.
        /// Груз и листок здесь не мешают: положить ящик или ткнуть станцию с листком
        /// в руке - это отдельные правила, и живут они у своих обработчиков
        /// </summary>
        public bool CanReachOut => !(_weapon.IsWeaponDrawn && _weapon.IsBusy);
    }
}
