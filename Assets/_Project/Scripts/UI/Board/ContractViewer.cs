using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    /// Листок текущего заказа в руках игрока. Тот же префаб, что висит на доске, только
    /// на слое ViewModel - его рисует та же наложенная камера, что и руки.
    /// <para>
    /// Позы здесь нет и быть не должно: листок висит на сокете под костью кисти и едет
    /// вместе с рукой, а поднимает и опускает его АНИМАЦИЯ, стейт <c>ContractHold</c>.
    /// Раньше листок жил на камере и приезжал к ней процедурно, со своим качанием, -
    /// теперь и то, и другое дала бы рука, а два источника движения дрались бы между собой.
    /// </para>
    /// <para>
    /// От этого компонента осталось ровно одно решение: ХОЧЕТ ли игрок видеть листок.
    /// Всё остальное - следствие
    /// </para>
    /// </summary>
    public sealed class ContractViewer : MonoBehaviour, IContractViewSource
    {
        [SerializeField] private ContractViewDefinition _definition;

        [Tooltip("Экземпляр листка на сокете кисти. Куда он там встал - правится в сцене")]
        [SerializeField] private ContractPaperView _paper;

        private IInputReader _input;
        private IContractManager _manager;
        private IEventBus _bus;

        private Transform _sheet;
        private bool _wanted;

        // Живёт, только пока идёт задержка показа или скрытия
        private CancellationTokenSource _visibility;

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

            ApplyLayer(_sheet.gameObject, _definition.ViewModelLayer);

            _sheet.gameObject.SetActive(false);
        }

        private void Start()
        {
            // Листок убирается той же кнопкой, что и достаётся, и только ею. Раньше его
            // опускали заодно клавиши захвата и броска - обеих больше нет: захват переехал
            // на кнопку руки, которой жмут ещё и станции, и опускать листок на КАЖДОЕ
            // такое нажатие значило бы отбирать его у игрока посреди работы со станцией
            _input.ViewContract += OnViewContractPressed;

            _bus.Subscribe<ContractCompleted>(OnContractClosed);
            _bus.Subscribe<ContractFailed>(OnContractFailed);
            _bus.Subscribe<GameLoaded>(OnGameLoaded);
        }

        private void OnDestroy()
        {
            CancelPendingVisibility();

            if (_input != null)
                _input.ViewContract -= OnViewContractPressed;

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

            // Перерисовываем ДО показа: листок ещё скрыт, и подстановка строк в него
            // никому не видна. Компонент это переживает и на выключенном объекте -
            // он ничего не кэширует в Awake
            _paper.Bind(active.Contract);

            _wanted = true;
            ScheduleVisibility(show: true);
        }

        private void OnContractClosed(ContractCompleted _) => Lower();

        private void OnContractFailed(ContractFailed _) => Lower();

        private void OnGameLoaded(GameLoaded _) => Lower();

        private void Lower()
        {
            // Опускать уже опущенное незачем: события шины приходят и тогда, когда листок
            // и так убран, а каждый заход плодил бы задержку скрытия на пустом месте
            if (!_wanted)
                return;

            _wanted = false;
            ScheduleVisibility(show: false);
        }

        /// <summary>
        /// Показ и скрытие ждут руку. Задержка берётся из ассета, а НЕ из длительности
        /// перехода в аниматоре: аниматор про листок ничего не знает, и лезть в него за
        /// числом значило бы завязать видимость на устройство графа
        /// </summary>
        private void ScheduleVisibility(bool show)
        {
            // Быстрое двойное нажатие: прежнее намерение отменяется целиком, иначе его
            // задержка дотикала бы и переключила листок уже после нового решения
            CancelPendingVisibility();

            float delay = show ? _definition.ShowDelay : _definition.HideDelay;

            if (delay <= 0f)
            {
                _sheet.gameObject.SetActive(show);
                return;
            }

            // Связан с токеном уничтожения: объект может умереть посреди задержки
            // (выход из play mode, выгрузка сцены), и трогать его Transform после этого нельзя
            _visibility = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            ApplyVisibilityAsync(show, delay, _visibility.Token).Forget();
        }

        private async UniTaskVoid ApplyVisibilityAsync(bool show, float delay, CancellationToken token)
        {
            bool canceled = await UniTask
                .Delay(TimeSpan.FromSeconds(delay), cancellationToken: token)
                .SuppressCancellationThrow();

            // Передумали или объекта уже нет - это не ошибка, просто ничего не делаем
            if (canceled)
                return;

            _sheet.gameObject.SetActive(show);
        }

        private void CancelPendingVisibility()
        {
            if (_visibility == null)
                return;

            _visibility.Cancel();
            _visibility.Dispose();
            _visibility = null;
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

            int layer = _definition.ViewModelLayer;

            if (layer < 0 || layer > 31)
                return Missing($"слой вьюмодели: {layer} - это не индекс слоя");

            return true;
        }

        private bool Missing(string what)
        {
            Debug.LogError($"Просмотру заказа '{name}' не назначено: {what}. Листок не поднять.", this);

            return false;
        }
    }
}
