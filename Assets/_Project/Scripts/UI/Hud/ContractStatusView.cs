using CrateExpectations.Contracts;
using CrateExpectations.Contracts.Events;
using CrateExpectations.Core.Events;
using CrateExpectations.Persistence.Events;
using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.UI
{
    public sealed class ContractStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _progress;

        [Header("Итог заказа")]
        [SerializeField] private HudTimingsDefinition _timings;
        [SerializeField] private Color _activeColor = new(0.92f, 0.88f, 0.72f);
        [SerializeField] private Color _completedColor = new(0.45f, 0.85f, 0.45f);
        [SerializeField] private Color _failedColor = new(0.90f, 0.35f, 0.30f);

        private IEventBus _bus;
        private IContractManager _manager;

        private float _outcomeRemaining;
        private float _outcomeSeconds;

        [Inject]
        public void Construct(IEventBus bus, IContractManager manager)
        {
            _bus = bus;
            _manager = manager;
        }

        private void Awake() =>
            _outcomeSeconds = _timings != null ? _timings.ContractOutcomeSeconds : 4f;

        private void Start()
        {
            _bus.Subscribe<ContractAccepted>(OnAccepted);
            _bus.Subscribe<ContractProgressed>(OnProgressed);
            _bus.Subscribe<ContractCompleted>(OnCompleted);
            _bus.Subscribe<ContractFailed>(OnFailed);
            _bus.Subscribe<GameLoaded>(OnGameLoaded);

            ShowActive(_manager.Active);
        }

        private void OnDestroy()
        {
            if (_bus == null) 
                return;

            _bus.Unsubscribe<ContractAccepted>(OnAccepted);
            _bus.Unsubscribe<ContractProgressed>(OnProgressed);
            _bus.Unsubscribe<ContractCompleted>(OnCompleted);
            _bus.Unsubscribe<ContractFailed>(OnFailed);
            _bus.Unsubscribe<GameLoaded>(OnGameLoaded);
        }
        private void OnGameLoaded(GameLoaded loaded)
        {
            _outcomeRemaining = 0f;
            ShowActive(_manager.Active);
        }

        private void Update()
        {
            if (_outcomeRemaining <= 0f) 
                return;

            _outcomeRemaining -= Time.deltaTime;

            if (_outcomeRemaining > 0f) 
                return;

            ShowActive(_manager.Active);
        }

        private void OnAccepted(ContractAccepted accepted)
        {
            _outcomeRemaining = 0f;
            ShowActive(accepted.Progress);
        }

        private void OnProgressed(ContractProgressed progressed) => ShowActive(progressed.Progress);

        private void OnCompleted(ContractCompleted completed) =>
            ShowOutcome("ЗАКАЗ ВЫПОЛНЕН", completed.Progress, _completedColor);

        private void OnFailed(ContractFailed failed) =>
            ShowOutcome("ЗАКАЗ ПРОВАЛЕН", failed.Progress, _failedColor);

        private void ShowActive(in ContractProgress progress)
        {
            bool active = progress.IsActive;
            _title.color = _activeColor;
            _title.text = active ? progress.Contract.DisplayName : "Заказа нет - загляните на доску";

            _progress.enabled = active;

            if (active == false) 
                return;

            if (progress.Seized > 0)
            {
                _progress.SetText(
                    "{0}/{1} сдано, изъято {2}", progress.Delivered, progress.Required, progress.Seized);
            }
            else
            {
                _progress.SetText("{0}/{1} сдано", progress.Delivered, progress.Required);
            }
        }

        private void ShowOutcome(string headline, in ContractProgress progress, Color color)
        {
            _title.text = headline;
            _title.color = color;

            _progress.enabled = true;
            _progress.SetText(
                "{0}/{1} сдано, изъято {2}", progress.Delivered, progress.Required, progress.Seized);

            _outcomeRemaining = _outcomeSeconds;
        }
    }
}
