using System;
using CrateExpectations.Core.Hands;
using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Interaction
{
    /// <summary>
    /// Рука игрока: один луч, ОДНА цель под прицелом и одна кнопка на всё.
    /// <para>
    /// Цель выбирается здесь, а не у каждого потребителя своя, потому что кнопка руки
    /// одна: ею жмут станцию, ею же поднимают и кладут груз. Пока действий было два
    /// (E на станцию, F на груз), выбирать было нечего - каждый отвечал на свою клавишу.
    /// С одной клавишей появляется вопрос «что именно сейчас», и ответ на него обязан
    /// быть один: подсказка на экране и то, что случится по нажатию, - это одно и то же
    /// решение, принятое в одном месте. Иначе подсказка рано или поздно начнёт врать.
    /// </para>
    /// <para>
    /// Приоритет: груз в руках важнее всего (положить можно всегда), дальше побеждает
    /// то, что БЛИЖЕ по лучу. Смотришь на станцию - жмёшь станцию, смотришь на ящик
    /// перед ней - берёшь ящик.
    /// </para>
    /// <para>
    /// Само взятие и отпускание исполняет <see cref="Carrier"/>: здесь только выбор
    /// и диспетчеризация, физика переноски - не наше дело
    /// </para>
    /// </summary>
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField] private InteractionDefinition _definition;
        [SerializeField] private Transform _rayOrigin;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private IInputReader _input;
        private Carrier _carrier;

        private IInteractable _current;
        private string _currentPrompt = string.Empty;

        private Collider _interactableCollider;
        private IInteractable _resolvedInteractable;
        private float _interactableDistance;

        private Carriable _grabTarget;
        private float _grabDistance;

        private Collider _focusCollider;
        private Transform _focus;

        private ReachAction _reach;

        public event Action<string> PromptChanged;

        /// <summary>
        /// Предмет под прицелом - не обязательно интерактивный: так о нём узнают мировые плашки,
        /// не пуская собственный луч. Пусто, когда игрок не смотрит ни на что из <see cref="InteractionDefinition.FocusMask"/>
        /// </summary>
        public event Action<Transform> FocusChanged;

        /// <inheritdoc cref="FocusChanged"/>
        public Transform Focus => _focus;

        /// <summary>Что сделает кнопка руки прямо сейчас</summary>
        public ReachAction Reach => _reach;

        [Inject]
        public void Construct(IInputReader input, Carrier carrier)
        {
            _input = input;
            _carrier = carrier;
        }

        /// <summary>
        /// Отдаёт общую модель занятости рук. Не через <c>[Inject]</c> - по той же причине,
        /// что и у переноски: связывание идёт из <c>GameLifetimeScope</c> после того,
        /// как модель и её источники уже созданы
        /// </summary>
        public void BindHands(HandsState hands) => _hands = hands;

        private HandsState _hands;

        private void Start() => _input.Interact += OnInteractPressed;

        private void OnDestroy()
        {
            if (_input != null)
                _input.Interact -= OnInteractPressed;
        }

        private void Update()
        {
            Scan();

            _reach = ResolveReach();

            // Подсвечивается и «сфокусирована» станция ровно тогда, когда нажатие достанется
            // ЕЙ. Иначе ящик, перехвативший цель, оставлял бы станцию гореть - и подсветка
            // обещала бы одно, а кнопка делала другое
            IInteractable interactable = _reach == ReachAction.Interact ? _resolvedInteractable : null;

            if (!ReferenceEquals(interactable, _current))
            {
                _current?.OnUnfocused();
                _current = interactable;
                _current?.OnFocused();
            }

            string prompt = ResolvePrompt();

            if (string.Equals(prompt, _currentPrompt))
                return;

            _currentPrompt = prompt;
            PromptChanged?.Invoke(prompt);
        }

        /// <summary>
        /// Один луч на трёх потребителей: ближайшая станция, ближайший поднимаемый груз
        /// и ближайший предмет под прицелом для мировых плашек. Разбираем их по маскам
        /// раздельно и в один проход - второй луч ради груза не пускаем
        /// </summary>
        private void Scan()
        {
            var ray = new Ray(_rayOrigin.position, _rayOrigin.forward);

            int count = Physics.RaycastNonAlloc(
                ray, _hits, _definition.ScanDistance,
                _definition.ScanMask, QueryTriggerInteraction.Ignore);

            Collider interactable = null;
            float interactableDistance = float.MaxValue;

            Collider focus = null;
            float focusDistance = float.MaxValue;

            Carriable grab = null;
            float grabDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                float distance = _hits[i].distance;

                if (distance < interactableDistance
                    && distance <= _definition.MaxDistance
                    && IsInMask(collider, _definition.InteractableMask))
                {
                    interactable = collider;
                    interactableDistance = distance;
                }

                if (distance < focusDistance
                    && distance <= _definition.FocusDistance
                    && IsInMask(collider, _definition.FocusMask))
                {
                    focus = collider;
                    focusDistance = distance;
                }

                // Что считается поднимаемым, знает переноска - у неё маска и дальность
                // захвата. Здесь только луч
                if (distance < grabDistance
                    && _carrier.TryResolveGrabTarget(collider, distance, out Carriable candidate))
                {
                    grab = candidate;
                    grabDistance = distance;
                }
            }

            TakeInteractable(interactable, interactableDistance);
            TakeFocus(focus);

            _grabTarget = grab;
            _grabDistance = grabDistance;
        }

        private static bool IsInMask(Collider collider, LayerMask mask) =>
            (mask.value & (1 << collider.gameObject.layer)) != 0;

        private void TakeInteractable(Collider collider, float distance)
        {
            _interactableDistance = distance;

            // Тот же коллайдер - тот же компонент: GetComponentInParent зовём только на смене цели
            if (ReferenceEquals(collider, _interactableCollider))
                return;

            _interactableCollider = collider;
            _resolvedInteractable = collider != null
                ? collider.GetComponentInParent<IInteractable>()
                : null;
        }

        private void TakeFocus(Collider collider)
        {
            if (ReferenceEquals(collider, _focusCollider))
                return;

            _focusCollider = collider;
            _focus = collider != null ? collider.transform : null;

            FocusChanged?.Invoke(_focus);
        }

        /// <summary>
        /// Выбор цели. Занятость рук учитывается ЗДЕСЬ, а не только на нажатии: подсказка
        /// «Взять» с саблей в руке была бы обещанием, которого кнопка не выполнит.
        /// <para>
        /// А вот занятость ДЕЙСТВИЕМ (замах, блок) сюда не входит намеренно: она живёт доли
        /// секунды, и подсказка моргала бы на каждом ударе. Её проверяет само нажатие
        /// </para>
        /// </summary>
        private ReachAction ResolveReach()
        {
            bool canGrab = _grabTarget != null && (_hands == null || _hands.CanGrab);

            return ReachPriority.Resolve(
                _carrier.IsCarrying,
                canGrab, _grabDistance,
                _resolvedInteractable != null, _interactableDistance);
        }

        private string ResolvePrompt()
        {
            switch (_reach)
            {
                case ReachAction.Interact:
                    return _current != null ? _current.Prompt : string.Empty;

                case ReachAction.Grab:
                    return _definition.GrabPrompt;

                case ReachAction.Drop:
                    return _definition.DropPrompt;

                default:
                    return string.Empty;
            }
        }

        private void OnInteractPressed()
        {
            // Посреди удара, блока или доставания оружия рука занята делом и никуда
            // не дотягивается. С опущенной саблей, с грузом и с листком - дотягивается:
            // это не занятость рук, а занятость действием
            if (_hands != null && !_hands.CanReachOut)
                return;

            switch (_reach)
            {
                case ReachAction.Interact:
                    if (_current != null && _current.CanInteract)
                        _current.Interact(this);

                    break;

                case ReachAction.Grab:
                    _carrier.Grab(_grabTarget);
                    break;

                case ReachAction.Drop:
                    _carrier.Drop();
                    break;
            }
        }
    }
}
