using System;
using UnityEngine;

namespace CrateExpectations.Core.Input
{
    /// <summary>
    /// Граница ввода: потребители (контроллер игрока, взаимодействие, перенос) зависят только
    /// от этого контракта, а не от Unity Input System (реализация живёт в модуле Player)
    /// </summary>
    public interface IInputReader
    {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }

        event Action Jump;

        /// <summary>
        /// Единственная кнопка «руки». Ею и жмут станцию, и поднимают груз, и кладут его
        /// обратно: что именно случится, решает то, на что игрок смотрит, а не то, какую
        /// клавишу он вспомнил. Отдельных Grab и Throw больше нет - бросок ушёл на кнопку
        /// удара, где он копит замах
        /// </summary>
        event Action Interact;

        /// <summary>Достать/убрать листок текущего заказа</summary>
        event Action ViewContract;

        /// <summary>Достать/убрать оружие</summary>
        event Action ToggleWeapon;

        /// <summary>Кнопку удара нажали</summary>
        event Action Attack;

        /// <summary>
        /// Кнопку удара отпустили. Нужно заряженным приёмам: пока кнопка зажата, бой
        /// копит удержание и по нему выбирает, каким ударом отвечать. Отдельным событием,
        /// а не флагом «зажата» - длительность удержания считает тот, кому она нужна,
        /// а ввод только сообщает края нажатия
        /// </summary>
        event Action AttackReleased;

        /// <summary>Кнопку блока нажали</summary>
        event Action BlockPressed;

        /// <summary>Кнопку блока отпустили. Отдельным событием, а не флагом: блок - это
        /// удержание, и состояние "держит" живёт в машине состояний оружия, а не во вводе</summary>
        event Action BlockReleased;

        event Action SaveGame;
        event Action LoadGame;
    }
}
