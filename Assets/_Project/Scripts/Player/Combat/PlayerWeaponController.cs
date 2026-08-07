using System;
using CrateExpectations.Combat;
using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Player.Combat
{
    /// <summary>
    /// Тонкий слой между вводом и <see cref="WeaponStateMachine"/>: нажатия переводит в команды,
    /// а решение о видимости оружия - в сокет. Сама логика состояний живёт в обычном C#-классе
    /// и про <see cref="MonoBehaviour"/> ничего не знает
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [Tooltip("Чем машем. Отсюда берутся и тайминги, и посадка оружия в руке")]
        [SerializeField] private WeaponDefinition _weapon;

        [Tooltip("Пустышки под костью кисти, в которые вешается оружие: одна на физическом " +
                 "теле, вторая на вьюмодели. Обе всегда держат одно и то же оружие в одном " +
                 "и том же состоянии - решение о видимости принимается здесь, один раз на всех")]
        [SerializeField] private WeaponSocket[] _sockets;

        private IInputReader _input;
        private WeaponStateMachine _machine;

        /// <summary>Ассет текущего оружия - единый источник таймингов для всех, кто их спросит</summary>
        public WeaponDefinition Weapon => _weapon;

        /// <summary>Что сейчас с оружием</summary>
        public WeaponState State => _machine == null ? WeaponState.Sheathed : _machine.State;

        /// <summary>
        /// Состояние сменилось. Событие своё, а не проброшенное из машины напрямую: подписчик
        /// живёт на другом объекте, и порядок <c>Awake</c> между объектами Unity не обещает -
        /// проброс через свойство падал бы, окажись подписка раньше сборки машины
        /// </summary>
        public event Action<WeaponState> StateChanged;

        /// <summary>
        /// Оружие появилось в руке или исчезло из неё. Момент не совпадает со сменой состояния:
        /// сабля возникает на середине доставания, а не в его начале, - и всё, что должно
        /// появиться вместе с ней, обязано слушать именно это событие
        /// </summary>
        public event Action<bool> WeaponVisibilityChanged;

        [Inject]
        public void Construct(IInputReader input) => _input = input;

        private void Awake()
        {
            if (_weapon == null || _sockets == null || _sockets.Length == 0)
            {
                Debug.LogError($"Оружию игрока '{name}' не назначен ассет или сокет - доставать нечего.", this);
                enabled = false;
                return;
            }

            _machine = new WeaponStateMachine(_weapon.Timings);
            _machine.WeaponVisibilityChanged += OnWeaponVisibilityChanged;
            _machine.StateChanged += OnStateChanged;

            // Оружие собирается один раз и дальше только прячется: пересоздавать префаб
            // на каждое доставание значило бы аллоцировать в момент, когда игрок нажал клавишу
            foreach (WeaponSocket socket in _sockets)
                if (socket != null)
                    socket.Mount(_weapon);

            SetWeaponVisible(false);
        }

        private void Start()
        {
            _input.ToggleWeapon += OnToggleWeaponPressed;
            _input.Attack += OnAttackPressed;
        }

        private void OnDestroy()
        {
            if (_input == null)
                return;

            _input.ToggleWeapon -= OnToggleWeaponPressed;
            _input.Attack -= OnAttackPressed;
        }

        private void OnToggleWeaponPressed() => _machine.ToggleWeapon();

        private void OnAttackPressed() => _machine.Attack();

        private void OnWeaponVisibilityChanged(bool visible)
        {
            SetWeaponVisible(visible);
            WeaponVisibilityChanged?.Invoke(visible);
        }

        /// <summary>
        /// Сабля появляется и исчезает сразу у обеих копий: физическая даёт тень, вьюмодельная -
        /// картинку, и разъехаться они не должны ни на кадр
        /// </summary>
        private void SetWeaponVisible(bool visible)
        {
            foreach (WeaponSocket socket in _sockets)
                if (socket != null)
                    socket.SetVisible(visible);
        }

        private void OnStateChanged(WeaponState state) => StateChanged?.Invoke(state);

        private void Update() => _machine.Tick(Time.deltaTime);
    }
}
