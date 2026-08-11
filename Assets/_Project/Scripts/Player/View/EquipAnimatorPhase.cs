using CrateExpectations.Combat;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Фаза доставания/убирания числом для аниматора. Отдельный класс по той же причине,
    /// что и <see cref="HandsAnimatorMode"/>: числа объявлены ровно один раз, и берут их
    /// отсюда оба конца - водитель, который пишет параметр в рантайме, и генератор графа,
    /// который ставит эти же числа в условия переходов.
    /// <para>
    /// Одно число на две взаимоисключающие фазы, а не два флага: двумя одновременно
    /// поднятыми выбор стейта достался бы порядку переходов в графе.
    /// </para>
    /// <para>
    /// Ноль здесь означает не «оружие убрано», а «переход не идёт»: и стойка с саблей,
    /// и стойка без неё - обе стоят на нуле, потому что различает их <c>IsArmed</c>.
    /// Именно поэтому вход в стейт и выход из него описываются ОДНИМ параметром:
    /// пока фаза не ноль, мы в переходе, стала нулём - вышли, и вернуться некуда
    /// </para>
    /// </summary>
    public static class EquipAnimatorPhase
    {
        /// <summary>Перехода нет: либо стойка, либо удар, либо блок</summary>
        public const int None = 0;

        /// <summary>Достаёт оружие</summary>
        public const int Drawing = 1;

        /// <summary>Убирает оружие</summary>
        public const int Sheathing = 2;

        /// <summary>Перевести состояние оружия в число для параметра аниматора</summary>
        public static int Of(WeaponState state)
        {
            switch (state)
            {
                case WeaponState.Drawing: return Drawing;
                case WeaponState.Sheathing: return Sheathing;
                default: return None;
            }
        }
    }
}
