using System;
using CrateExpectations.Interaction;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    /// <summary>
    /// Верстак маскировки: игрок ставит ящик в зону и жмёт "взаимодействовать" на самой станции.
    /// Станция только оркестрирует - считает результат <see cref="DisguiseProcessor"/>,
    /// записывает его в ящик, просит пул обновить декаль и даёт визуальный отклик.
    /// Чем именно станция красит/штампует/переливает, решает ассет
    /// <see cref="DisguiseStationDefinition"/>, а не отдельный класс на каждый тип.
    /// </summary>
    public sealed class DisguiseStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private DisguiseStationDefinition _definition;

        [Tooltip("Зона размещения груза над рабочей поверхностью")]
        [SerializeField] private CargoPlacementZone _zone;

        [Tooltip("Подсветка станции при наведении камеры")]
        [SerializeField] private InteractableHighlight _highlight;

        [Tooltip("Пул декалей. Нужен станциям печати")]
        [SerializeField] private StampDecalPool _decalPool;

        private readonly DisguiseProcessor _processor = new();

        private CargoBox _observed;
        private string _prompt = string.Empty;
        private bool _canInteract;
        private bool _feedbackRunning;

        /// <inheritdoc />
        public string Prompt => _prompt;

        /// <inheritdoc />
        public bool CanInteract => _canInteract;

        private void Awake()
        {
            if (_definition == null || _definition.Recipe == null)
            {
                Debug.LogError($"Станции '{name}' не назначен рецепт - работать она не будет", this);
                enabled = false;
                return;
            }

            if (_zone == null)
            {
                Debug.LogError($"Станции '{name}' не назначена зона размещения", this);
                enabled = false;
                return;
            }

            if (_definition.Recipe.Action == DisguiseAction.Stamp && _decalPool == null)
            {
                Debug.LogError($"Станции печати '{name}' не назначен пул декалей - пломба не появится", this);
            }
        }

        private void OnEnable()
        {
            _zone.OccupantChanged += OnOccupantChanged;
            Observe(_zone.Occupant);
            RefreshPrompt();
        }

        private void OnDisable()
        {
            _zone.OccupantChanged -= OnOccupantChanged;
            Observe(null);
        }

        /// <inheritdoc />
        public void OnFocused()
        {
            if (_highlight != null) _highlight.SetHighlighted(true);
        }

        /// <inheritdoc />
        public void OnUnfocused()
        {
            if (_highlight != null) _highlight.SetHighlighted(false);
        }

        /// <inheritdoc />
        public void Interact(Interactor source)
        {
            CargoBox box = _zone.Occupant;

            if (box == null) 
                return;

            DisguiseResult result = _processor.Apply(
                box.State, box.Identity, _definition.Recipe.Operation);

            if (!result.Changed)
            {
                RefreshPrompt();
                return;
            }

            box.ApplyState(result.State);

            if (_decalPool != null) 
                _decalPool.Sync(box, result.State.Stamp);

            RefreshPrompt();
            PlayFeedbackAsync().Forget();

            Debug.Log(
                $"[{name}] {_definition.Recipe.Description}. Заявлено - {result.State}. " +
                $"Истина - {box.Identity} (не менялась)" +
                (result.DivergesFromTruth ? ", есть расхождение" : "."), box);
        }

        private void OnOccupantChanged(CargoBox box)
        {
            Observe(box);
            RefreshPrompt();
        }

        private void Observe(CargoBox box)
        {
            if (ReferenceEquals(_observed, box)) 
                return;

            if (_observed != null) 
                _observed.StateChanged -= OnCargoStateChanged;

            _observed = box;

            if (_observed != null) 
                _observed.StateChanged += OnCargoStateChanged;
        }

        private void OnCargoStateChanged(CargoBox box) => RefreshPrompt();

        /// <summary>
        /// Пересобрать подсказку и доступность. Дёргается только по событиям (сменился груз в зоне
        /// или его состояние), а <see cref="Prompt"/> каждый кадр отдаёт готовую строку.
        /// "Примерка" рецепта безопасна: процессор ничего не мутирует
        /// </summary>
        private void RefreshPrompt()
        {
            CargoBox box = _zone.Occupant;

            if (box == null)
            {
                _prompt = _definition.EmptyZonePrompt;
                _canInteract = false;
                return;
            }

            DisguiseRecipe recipe = _definition.Recipe;
            DisguiseResult preview = _processor.Apply(box.State, box.Identity, recipe.Operation);

            switch (preview.Outcome)
            {
                case DisguiseOutcome.Applied:
                    _prompt = string.Format(_definition.ReadyPromptFormat, recipe.Description);
                    _canInteract = true;
                    break;

                case DisguiseOutcome.AlreadyApplied:
                    _prompt = string.Format(_definition.AlreadyDonePromptFormat, recipe.Description);
                    _canInteract = false;
                    break;

                default:
                    string required = recipe.RequiredPaint != null
                        ? recipe.RequiredPaint.DisplayName
                        : string.Empty;
                    _prompt = string.Format(
                        _definition.BlockedPromptFormat, recipe.Description, required);
                    _canInteract = false;
                    break;
            }
        }

        /// <summary>Короткая вспышка зоны: "сработало". UniTask, не корутина</summary>
        private async UniTaskVoid PlayFeedbackAsync()
        {
            if (_feedbackRunning) 
                return;

            _feedbackRunning = true;
            _zone.SetColorOverride(_definition.FeedbackColor);

            await UniTask
                .Delay(TimeSpan.FromSeconds(_definition.FeedbackSeconds),
                    cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();

            _feedbackRunning = false;

            if (_zone != null) 
                _zone.ClearColorOverride();
        }
    }
}
