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
        event Action Interact;
        event Action Grab;
        event Action Throw;

        /// <summary>Достать/убрать листок текущего заказа</summary>
        event Action ViewContract;

        /// <summary>Достать/убрать оружие</summary>
        event Action ToggleWeapon;

        /// <summary>Взмах оружием</summary>
        event Action Attack;

        event Action SaveGame;
        event Action LoadGame;
    }
}
