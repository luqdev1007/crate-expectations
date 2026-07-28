using System.Text;
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
    /// сам не спрашивает: событие приносит и новое значение, и разбивку, из чего оно сложилось.
    ///
    /// <para>Изменение подсвечивается: цифра вспыхивает цветом и рядом всплывает строка
    /// "+250 доставка". Без этого на записи демо не видно, что вообще произошло, -
    /// баланс молча меняется между кадрами.</para>
    /// </summary>
    public sealed class BalanceView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _balance;
        [Tooltip("Всплывающая строка изменения: сумма и за что")]
        [SerializeField] private TMP_Text _delta;

        [Header("Подсветка")]
        [Tooltip("Ритм подачи. Сколько держится вспышка - оттуда")]
        [SerializeField] private HudTimingsDefinition _timings;
        [Tooltip("На сколько подрастает цифра в момент вспышки")]
        [SerializeField] private float _punchScale = 1.35f;

        [Tooltip("Цвет суммы в покое: золото самой монетки-иконки, чтобы число и спрайт читались одним")]
        [SerializeField] private Color _idleColor = new(0.94f, 0.81f, 0.16f);
        [SerializeField] private Color _gainColor = new(0.45f, 0.85f, 0.45f);
        [SerializeField] private Color _lossColor = new(0.90f, 0.35f, 0.30f);
        [Tooltip("Баланс ушёл в минус - долг виден и в покое")]
        [SerializeField] private Color _debtColor = new(0.90f, 0.55f, 0.25f);

        private readonly StringBuilder _builder = new(64);

        private IEventBus _bus;
        private IEconomyService _economy;

        private float _flashRemaining;
        private float _flashSeconds;
        private Color _flashColor;

        [Inject]
        public void Construct(IEventBus bus, IEconomyService economy)
        {
            _bus = bus;
            _economy = economy;
        }

        private void Awake() => _flashSeconds = _timings != null ? _timings.BalanceFlashSeconds : 2.5f;

        private void Start()
        {
            _bus.Subscribe<BalanceChanged>(OnBalanceChanged);
            _bus.Subscribe<GameLoaded>(OnGameLoaded);

            // Стартовый баланс никем не публикуется: он не "изменение", а начальное условие
            ShowBalance(_economy.Balance);
            _balance.color = RestColor();
            _delta.text = string.Empty;
        }

        private void OnDestroy()
        {
            if (_bus == null)
                return;

            _bus.Unsubscribe<BalanceChanged>(OnBalanceChanged);
            _bus.Unsubscribe<GameLoaded>(OnGameLoaded);
        }

        /// <summary>
        /// После загрузки баланс просто "стал таким" - начисления не было, вспыхивать нечему
        /// и разбивку показывать нечего. Поэтому цифра переписывается тихо
        /// </summary>
        private void OnGameLoaded(GameLoaded loaded)
        {
            _flashRemaining = 0f;
            _delta.text = string.Empty;
            _balance.transform.localScale = Vector3.one;

            ShowBalance(_economy.Balance);
            _balance.color = RestColor();
        }

        private void Update()
        {
            if (_flashRemaining <= 0f) 
                return;

            _flashRemaining -= Time.deltaTime;
            float t = Mathf.Clamp01(_flashRemaining / _flashSeconds);

            // Затухание к покою: цвет возвращается, всплывшая строка тает, цифра садится на место
            _balance.color = Color.Lerp(RestColor(), _flashColor, t);
            _balance.transform.localScale = Vector3.one * Mathf.Lerp(1f, _punchScale, t * t);

            Color faded = _flashColor;
            faded.a = t;
            _delta.color = faded;

            if (_flashRemaining > 0f) 
                return;

            _delta.text = string.Empty;
            _balance.transform.localScale = Vector3.one;
        }

        private void OnBalanceChanged(BalanceChanged changed)
        {
            ShowBalance(changed.Balance);

            _delta.text = Describe(changed.Payout);
            _flashColor = changed.Delta < 0 ? _lossColor : _gainColor;
            _flashRemaining = _flashSeconds;
        }

        /// <summary>
        /// Валюта показывается монетой, а не словом: значок читается с одного взгляда и
        /// избавляет от падежей - "1 дукат", "2 дуката", "5 дукатов" пришлось бы согласовывать
        /// с числом. Спрайт живёт в <c>Ducat_SpriteAsset</c>, прописанном дефолтным в
        /// TMP Settings, поэтому тег работает в любом тексте и ссылки на ассет здесь не нужно
        /// </summary>
        private void ShowBalance(int balance) => _balance.SetText("{0} <sprite name=\"ducat\">", balance);

        /// <summary>Цвет, к которому вспышка затухает: долг остаётся заметным и без неё</summary>
        private Color RestColor() => _economy.Balance < 0 ? _debtColor : _idleColor;

        /// <summary>
        /// Разбивка одной строкой: "+250 доставка, +50 чисто". Строится раз на сдачу,
        /// а не каждый кадр
        /// </summary>
        private string Describe(in PayoutResult payout)
        {
            _builder.Clear();

            var lines = payout.Lines;

            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) 
                    _builder.Append(", ");

                PayoutLine line = lines[i];

                if (line.Amount > 0) 
                    _builder.Append('+');

                _builder.Append(line.Amount).Append(' ').Append(Name(line.Reason));
            }

            return _builder.ToString();
        }

        private static string Name(PayoutReason reason)
        {
            switch (reason)
            {
                case PayoutReason.Delivery: return "за груз";
                case PayoutReason.CleanBonus: return "чисто сработано";
                case PayoutReason.Seizure: return "штраф за изъятое";
                default: return string.Empty;
            }
        }
    }
}
