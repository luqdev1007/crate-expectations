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

        event Action SaveGame;
        event Action LoadGame;
    }
}
