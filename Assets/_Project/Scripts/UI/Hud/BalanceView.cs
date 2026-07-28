using CrateExpectations.Core.Events;
using CrateExpectations.Economy;
using CrateExpectations.Economy.Events;
using CrateExpectations.Persistence.Events;
using TMPro;
using UnityEngine;
using VContainer;

namespace CrateExpectations.UI
{
    /// <summary>
    /// Кошелёк на экране. Подписан на <see cref="BalanceChanged"/> - ни баланса, ни расчёта
    /// сам не спрашивает: событие приносит новое значение готовым.
    ///
    /// <para>Изменение отыгрывается в два такта: сперва над кошельком всплывает полупрозрачное
    /// "+250" - сколько именно прибавили, - и следом счётчик прокручивается к новой сумме.
    /// Прибыль зелёная и уходит вверх, убыток красный и оседает вниз: знак, цвет и направление
    /// говорят одно и то же, поэтому понятно даже боковым зрением.</para>
    /// </summary>
    public sealed class BalanceView : MonoBehaviour
    {
        private const string GainFormat = "+{0} <sprite name=\"ducat\">";
        private const string LossFormat = "-{0} <sprite name=\"ducat\">";

        [SerializeField] private TMP_Text _balance;
        [Tooltip("Всплывающая сумма изменения над кошельком")]
        [SerializeField] private TMP_Text _delta;

        [Header("Анимация")]
        [Tooltip("Ритм подачи: сколько идёт прокрутка и сколько живёт всплывшая сумма - оттуда")]
        [SerializeField] private HudTimingsDefinition _timings;

        [Tooltip("На сколько пикселей всплывшая сумма уходит от своего места. " +
                 "Кошелёк висит в самом углу, и запаса над ним немного: подъём должен " +
                 "укладываться в отступ панели от края экрана")]
        [SerializeField] private float _deltaDrift = 8f;

        [Tooltip("Непрозрачность всплывшей суммы в самый заметный момент")]
        [Range(0.1f, 1f)][SerializeField] private float _deltaAlpha = 0.75f;

        [Header("Цвета")]
        [Tooltip("Цвет суммы в покое: золото самой монетки-иконки, чтобы число и спрайт читались одним")]
        [SerializeField] private Color _idleColor = new(0.94f, 0.81f, 0.16f);
        [SerializeField] private Color _gainColor = new(0.45f, 0.85f, 0.45f);
        [SerializeField] private Color _lossColor = new(0.90f, 0.35f, 0.30f);
        [Tooltip("Баланс ушёл в минус - долг виден и в покое")]
        [SerializeField] private Color _debtColor = new(0.90f, 0.55f, 0.25f);

        private readonly NumberRoll _roll = new();

        private IEventBus _bus;
        private IEconomyService _economy;

        private RectTransform _deltaRect;
        private Vector2 _deltaHome;

        private float _leadSeconds;
        private float _rollSeconds;
        private float _deltaSeconds;

        // Начисление, которое уже всплыло над кошельком, но за которым счётчик ещё не тронулся
        private int _pendingBalance;
        private float _leadRemaining;

        private float _deltaRemaining;
        private Color _changeColor;
        private bool _isGain;

        [Inject]
        public void Construct(IEventBus bus, IEconomyService economy)
        {
            _bus = bus;
            _economy = economy;
        }

        private void Awake()
        {
            _deltaRect = _delta.rectTransform;
            _deltaHome = _deltaRect.anchoredPosition;

            _leadSeconds = _timings != null ? _timings.BalanceLeadSeconds : 0.2f;
            _rollSeconds = _timings != null ? _timings.BalanceRollSeconds : 0.8f;
            _deltaSeconds = _timings != null ? _timings.BalanceDeltaSeconds : 1.6f;
        }

        private void Start()
        {
            _bus.Subscribe<BalanceChanged>(OnBalanceChanged);
            _bus.Subscribe<GameLoaded>(OnGameLoaded);

            // Стартовый баланс никем не публикуется: он не "изменение", а начальное условие
            ShowInstantly(_economy.Balance);
        }

        private void OnDestroy()
        {
            if (_bus == null)
                return;

            _bus.Unsubscribe<BalanceChanged>(OnBalanceChanged);
            _bus.Unsubscribe<GameLoaded>(OnGameLoaded);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (_deltaRemaining > 0f)
                AnimateDelta(deltaTime);

            if (_leadRemaining > 0f)
                CountLead(deltaTime);

            if (_roll.IsRolling)
                AnimateRoll(deltaTime);
        }

        /// <summary>
        /// Пустых изменений сюда не приходит: расчёт на ноль кошелёк не применяет и о нём
        /// не объявляет, - поэтому показывать всплывшую сумму можно без оговорок
        /// </summary>
        private void OnBalanceChanged(BalanceChanged changed)
        {
            _isGain = changed.Delta > 0;
            _changeColor = _isGain ? _gainColor : _lossColor;

            _delta.SetText(_isGain ? GainFormat : LossFormat, Mathf.Abs(changed.Delta));
            _delta.color = Transparent(_changeColor, 0f);
            _deltaRemaining = _deltaSeconds;

            _pendingBalance = changed.Balance;
            _leadRemaining = _leadSeconds;

            // Ритм важнее буквальности: если пауза настроена в ноль, счётчик трогается сразу
            if (_leadRemaining <= 0f)
                StartRoll();
        }

        /// <summary>
        /// После загрузки баланс просто "стал таким" - начисления не было, всплывать нечему
        /// и прокручивать нечего. Поэтому цифра переписывается тихо
        /// </summary>
        private void OnGameLoaded(GameLoaded loaded) => ShowInstantly(_economy.Balance);

        private void ShowInstantly(int balance)
        {
            _roll.JumpTo(balance);
            _leadRemaining = 0f;
            _deltaRemaining = 0f;

            _delta.text = string.Empty;
            _deltaRect.anchoredPosition = _deltaHome;

            ShowBalance(balance);
            _balance.color = RestColor();
        }

        /// <summary>
        /// Всплывшая сумма: проявляется, уплывает в сторону своего знака и тает.
        /// Прибыль тянет вверх, убыток оседает вниз к кошельку, из которого его вычли
        /// </summary>
        private void AnimateDelta(float deltaTime)
        {
            _deltaRemaining -= deltaTime;

            if (_deltaRemaining <= 0f)
            {
                _deltaRemaining = 0f;
                _delta.text = string.Empty;
                _deltaRect.anchoredPosition = _deltaHome;
                return;
            }

            float life = 1f - _deltaRemaining / _deltaSeconds;
            float travel = Mathf.SmoothStep(0f, 1f, life);

            Vector2 home = _deltaHome;
            home.y += _deltaDrift * (_isGain ? travel : 1f - travel);
            _deltaRect.anchoredPosition = home;

            _delta.color = Transparent(_changeColor, _deltaAlpha * Fade(life));
        }

        private void CountLead(float deltaTime)
        {
            _leadRemaining -= deltaTime;

            if (_leadRemaining > 0f)
                return;

            _leadRemaining = 0f;
            StartRoll();
        }

        private void StartRoll() => _roll.RollTo(_pendingBalance, _rollSeconds);

        /// <summary>
        /// Прокрутка счётчика. Цифра идёт от цвета изменения к покою вместе с самой прокруткой:
        /// подсветка гаснет ровно тогда, когда число встаёт на итоговое значение
        /// </summary>
        private void AnimateRoll(float deltaTime)
        {
            _roll.Advance(deltaTime);

            ShowBalance(_roll.Value);
            _balance.color = Color.Lerp(_changeColor, RestColor(), _roll.Progress);
        }

        /// <summary>
        /// Валюта показывается монетой, а не словом: значок читается с одного взгляда и
        /// избавляет от падежей - "1 дукат", "2 дуката", "5 дукатов" пришлось бы согласовывать
        /// с числом. Спрайт живёт в <c>Ducat_SpriteAsset</c>, прописанном дефолтным в
        /// TMP Settings, поэтому тег работает в любом тексте и ссылки на ассет здесь не нужно
        /// </summary>
        private void ShowBalance(int balance) => _balance.SetText("{0} <sprite name=\"ducat\">", balance);

        /// <summary>Цвет, к которому приходит счётчик: долг остаётся заметным и в покое</summary>
        private Color RestColor() => _economy.Balance < 0 ? _debtColor : _idleColor;

        /// <summary>Быстро проявиться, медленно растаять - так сумма успевает прочитаться</summary>
        private static float Fade(float life)
        {
            const float fadeIn = 0.15f;

            return life < fadeIn
                ? life / fadeIn
                : 1f - (life - fadeIn) / (1f - fadeIn);
        }

        private static Color Transparent(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
