using System;
using CrateExpectations.Core.Input;
using CrateExpectations.Player.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace CrateExpectations.Player
{
    /// <summary>
    /// POCO-обёртка над сгенерированным <see cref="PlayerControls"/>: единственное место, где
    /// Unity Input System встречается с игрой, наружу отдаётся только <see cref="IInputReader"/>
    /// (в DI живёт как singleton entry point - включается на старте, освобождается со scope)
    /// </summary>
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
        public event Action ToggleWeapon;
        public event Action Attack;
        public event Action AttackReleased;
        public event Action BlockPressed;
        public event Action BlockReleased;
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

        void PlayerControls.IPlayerActions.OnToggleWeapon(InputAction.CallbackContext context)
        {
            if (context.performed)
                ToggleWeapon?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnAttack(InputAction.CallbackContext context)
        {
            // Обе фазы: удар бывает заряженным, и тогда значение имеет не только
            // момент нажатия, но и то, сколько кнопку продержали
            if (context.performed)
                Attack?.Invoke();
            else if (context.canceled)
                AttackReleased?.Invoke();
        }

        void PlayerControls.IPlayerActions.OnBlock(InputAction.CallbackContext context)
        {
            // Блок держат так же, как заряженный удар, - обе фазы нужны и здесь
            if (context.performed)
                BlockPressed?.Invoke();
            else if (context.canceled)
                BlockReleased?.Invoke();
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
