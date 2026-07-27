using System;
using CrateExpectations.Core.Input;
using CrateExpectations.Player.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace CrateExpectations.Player
{
    public sealed class InputReader : IInputReader, PlayerControls.IPlayerActions, IStartable, IDisposable
    {
        private readonly PlayerControls _controls;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        public event Action Jump;
        public event Action Interact;
        public event Action Grab;
        public event Action Throw;
        public event Action ViewContract;
        public event Action SaveGame;
        public event Action LoadGame;

        public InputReader()
        {
            _controls = new PlayerControls();
            _controls.Player.SetCallbacks(this);
        }

        void IStartable.Start() => _controls.Player.Enable();

        void PlayerControls.IPlayerActions.OnMove(InputAction.CallbackContext context)
            => MoveInput = context.ReadValue<Vector2>();

        void PlayerControls.IPlayerActions.OnLook(InputAction.CallbackContext context)
            => LookInput = context.ReadValue<Vector2>();

        void PlayerControls.IPlayerActions.OnJump(InputAction.CallbackContext context)
        {
            if (context.performed) 
                Jump?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed) 
                Interact?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnGrab(InputAction.CallbackContext context)
        {
            if (context.performed) 
                Grab?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnThrow(InputAction.CallbackContext context)
        {
            if (context.performed) 
                Throw?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnViewContract(InputAction.CallbackContext context)
        {
            if (context.performed)
                ViewContract?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnSaveGame(InputAction.CallbackContext context)
        {
            if (context.performed) 
                SaveGame?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnLoadGame(InputAction.CallbackContext context)
        {
            if (context.performed) 
                LoadGame?.Invoke();
        }

        public void Dispose()
        {
            _controls.Player.Disable();
            _controls.Dispose();
        }
    }
}
