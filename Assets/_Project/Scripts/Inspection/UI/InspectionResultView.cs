using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrateExpectations.Inspection.UI
{
    public sealed class InspectionResultView : MonoBehaviour, IInspectorVoice
    {
        [Tooltip("Корень плашки с итогом: включается только на вердикте")]
        [SerializeField] private GameObject _verdictPanel;

        [Tooltip("Исход одним словом")]
        [SerializeField] private TMP_Text _headline;

        [Tooltip("Перечень найденных улик")]
        [SerializeField] private TMP_Text _clues;

        [Tooltip("Заполняемая полоса шкалы подозрения (fill type imgae)")]
        [SerializeField] private Image _suspicionFill;

        [Tooltip("Подпись к шкале: подозрение против порога")]
        [SerializeField] private TMP_Text _suspicionLabel;

        private void Awake()
        {
            if (!IsWiredUp())
            {
                enabled = false;
                return;
            }

            Clear();
        }

        /// <summary>
        /// Реплики переехали в мировой пузырь над головой инспектора, плашка их не дублирует:
        /// здесь остаётся только сводка по итогу досмотра.
        /// </summary>
        public void Say(string line)
        {
        }

        public void ShowVerdict(in VerdictReport report)
        {
            _headline.text = report.Headline;
            _headline.color = report.Accent;

            _clues.text = report.Clues;
            _clues.enabled = !string.IsNullOrEmpty(report.Clues);

            _suspicionFill.fillAmount = report.Pressure;
            _suspicionFill.color = report.Accent;
            _suspicionLabel.text = report.Scale;

            _verdictPanel.SetActive(true);
        }
        public void Clear()
        {
            if (_verdictPanel == null) 
                return;

            _verdictPanel.SetActive(false);
        }

        private bool IsWiredUp()
        {
            if (_verdictPanel != null && _headline != null &&
                _clues != null && _suspicionFill != null && _suspicionLabel != null)
            {
                return true;
            }

            Debug.LogError(
                $"Плашке досмотра '{name}' не назначены поля - вердикт показать будет негде.", this);

            return false;
        }
    }
}
