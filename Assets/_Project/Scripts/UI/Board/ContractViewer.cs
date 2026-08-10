using CrateExpectations.Contracts;
using CrateExpectations.Contracts.Events;
using CrateExpectations.Core.Events;
using CrateExpectations.Core.Hands;
using CrateExpectations.Core.Input;
using CrateExpectations.Persistence.Events;
using UnityEngine;
using VContainer;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Листок текущего заказа в руках игрока. Тот же префаб, что висит на доске,
    /// только на слое FirstPersonItem - его рисует отдельная наложенная камера,
    /// поэтому листок не проваливается в стены и не режется геометрией
    /// </summary>
    public sealed class ContractViewer : MonoBehaviour, IContractViewSource
    {
        [SerializeField] private ContractViewDefinition _definition;

        [Tooltip("Экземпляр листка под камерой от первого лица")]
        [SerializeField] private ContractPaperView _paper;

        [Tooltip("Камера игрока: относительно неё считается поза и запаздывание")]
        [SerializeField] private Transform _eye;

        private IInputReader _input;
        private IContractManager _manager;
        private IEventBus _bus;

        private Transform _sheet;
        private Quaternion _heldRotation;
        private Quaternion _stowedRotation;
        private Quaternion _previousEyeRotation;
        private Quaternion _sway = Quaternion.identity;

        /// <summary>0 - листок убран, 1 - поднят</summary>
        private float _raise;
        private bool _wanted;

        /// <inheritdoc />
        public bool IsRaised => _wanted;

        [Inject]
        public void Construct(IInputReader input, IContractManager manager, IEventBus bus)
        {
            _input = input;
            _manager = manager;
            _bus = bus;
        }

        /// <summary>
        /// Отдаёт листку общую модель занятости рук. Не через <c>[Inject]</c> намеренно:
        /// этот же компонент - ИСТОЧНИК для <see cref="HandsState"/>, и инъекция замкнула бы
        /// граф зависимостей сам на себя. Связывание идёт из <c>GameLifetimeScope</c>,
        /// когда оба конца уже созданы
        /// </summary>
        public void BindHands(HandsState hands) => _hands = hands;

        private HandsState _hands;

        private void Awake()
        {
            if (!IsWiredUp())
            {
                enabled = false;
                return;
            }

            _sheet = _paper.transform;
            _sheet.SetParent(_eye, worldPositionStays: false);
            _sheet.localScale = Vector3.one * _definition.HeldScale;

            _heldRotation = Quaternion.Euler(_definition.HeldEulerAngles);
            _stowedRotation = _heldRotation * Quaternion.Euler(_definition.StowedEulerOffset);

            ApplyLayer(_sheet.gameObject, _definition.FirstPersonLayer);

            _previousEyeRotation = _eye.rotation;

            Stow();
        }

        private void Start()
        {
            _input.ViewContract += OnViewContractPressed;

            // Листок занимает руки: берём ящик или швыряем - листок уходит сам
            _input.Grab += Lower;
            _input.Throw += Lower;

            _bus.Subscribe<ContractCompleted>(OnContractClosed);
            _bus.Subscribe<ContractFailed>(OnContractFailed);
            _bus.Subscribe<GameLoaded>(OnGameLoaded);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.ViewContract -= OnViewContractPressed;
                _input.Grab -= Lower;
                _input.Throw -= Lower;
            }

            if (_bus == null)
                return;

            _bus.Unsubscribe<ContractCompleted>(OnContractClosed);
            _bus.Unsubscribe<ContractFailed>(OnContractFailed);
            _bus.Unsubscribe<GameLoaded>(OnGameLoaded);
        }

        private void OnViewContractPressed()
        {
            // Опустить поднятое можно всегда и первым делом: иначе занятость "Reading",
            // которую создаёт сам листок, запретила бы его же убрать
            if (_wanted)
            {
                Lower();
                return;
            }

            // Руки заняты грузом или оружием - нажатие просто пропадает. Ни автосброса
            // ящика, ни "одной рукой": освободить руки - решение игрока
            if (_hands != null && !_hands.CanRaiseContract)
                return;

            ContractProgress active = _manager.Active;

            if (!active.IsActive)
                return;

            _paper.Bind(active.Contract);

            _wanted = true;
            _sheet.gameObject.SetActive(true);
        }

        private void OnContractClosed(ContractCompleted _) => Lower();

        private void OnContractFailed(ContractFailed _) => Lower();

        private void OnGameLoaded(GameLoaded _) => Lower();

        private void Lower() => _wanted = false;

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;

            if (!Raise(deltaTime))
                return;

            Sway(deltaTime);

            float eased = _raise * _raise * (3f - 2f * _raise);

            _sheet.SetLocalPositionAndRotation(
                Vector3.Lerp(_definition.HeldPosition + _definition.StowedOffset,
                    _definition.HeldPosition, eased),
                _sway * Quaternion.Slerp(_stowedRotation, _heldRotation, eased));
        }

        /// <summary>Двигает степень подъёма. Возвращает false, когда листок полностью убран</summary>
        private bool Raise(float deltaTime)
        {
            float target = _wanted ? 1f : 0f;

            if (_raise != target)
            {
                float duration = _wanted ? _definition.RaiseSeconds : _definition.LowerSeconds;

                _raise = duration > 0f
                    ? Mathf.MoveTowards(_raise, target, deltaTime / duration)
                    : target;
            }

            if (_raise > 0f)
                return true;

            if (_sheet.gameObject.activeSelf)
                Stow();

            return false;
        }

        /// <summary>
        /// Листок догоняет камеру не мгновенно: копим поворот камеры за кадр и распускаем его обратно
        /// </summary>
        private void Sway(float deltaTime)
        {
            Quaternion eye = _eye.rotation;

            if (_definition.SwayStiffness <= 0f || _definition.MaxSwayDegrees <= 0f)
            {
                _previousEyeRotation = eye;
                _sway = Quaternion.identity;
                return;
            }

            Quaternion lag = Quaternion.Inverse(eye) * _previousEyeRotation;
            _previousEyeRotation = eye;

            _sway = Quaternion.Slerp(
                _sway * lag, Quaternion.identity,
                Mathf.Clamp01(_definition.SwayStiffness * deltaTime));

            _sway = Quaternion.RotateTowards(
                Quaternion.identity, _sway, _definition.MaxSwayDegrees);
        }

        private void Stow()
        {
            _raise = 0f;
            _sway = Quaternion.identity;
            _previousEyeRotation = _eye.rotation;
            _sheet.gameObject.SetActive(false);
        }

        private static void ApplyLayer(GameObject root, int layer)
        {
            root.layer = layer;

            Transform transform = root.transform;

            for (int i = 0; i < transform.childCount; i++)
                ApplyLayer(transform.GetChild(i).gameObject, layer);
        }

        private bool IsWiredUp()
        {
            if (_definition == null)
                return Missing("настройки листка в руках (ContractViewDefinition)");

            if (_paper == null)
                return Missing("экземпляр листка (ContractPaperView)");

            if (_eye == null)
                return Missing("камера игрока");

            int layer = _definition.FirstPersonLayer;

            if (layer < 0 || layer > 31)
                return Missing($"слой первого лица: {layer} - это не индекс слоя");

            return true;
        }

        private bool Missing(string what)
        {
            Debug.LogError($"Просмотру заказа '{name}' не назначено: {what}. Листок не поднять.", this);

            return false;
        }
    }
}
