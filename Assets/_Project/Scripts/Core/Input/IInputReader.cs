using System;
using UnityEngine;

namespace CrateExpectations.Core.Input
{
    public interface IInputReader
    {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }

        event Action Jump;
        event Action Interact;
        event Action Grab;
        event Action Throw;

        /// <summary>Достать/убрать листок текущего заказа.</summary>
        event Action ViewContract;

        event Action SaveGame;
        event Action LoadGame;
    }
}
