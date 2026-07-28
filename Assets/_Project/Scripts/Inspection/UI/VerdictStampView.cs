using System;
using System.Threading;
using CrateExpectations.Inspection.AI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CrateExpectations.Inspection.UI
{
    /// <summary>
    /// Итог досмотра как удар печатью: оснастка входит в кадр, бьёт - и под ней проступает
    /// оттиск "approved"/"disapproved". Ни цифр, ни строк: исход читается картинкой, поэтому
    /// вид знает о вердикте ровно одно - задержан груз или нет.
    ///
    /// <para>Стоит за <see cref="IInspectorVoice"/> на месте прежней плашки, так что FSM
    /// досмотра о замене не знает: она по-прежнему просто "показывает вердикт".</para>
    /// </summary>
    public sealed class VerdictStampView : MonoBehaviour, IInspectorVoice
    {
        [Tooltip("Ритм удара и сами оттиски")]
        [SerializeField] private StampPressDefinition _definition;

        [Tooltip("Корень оснастки перед камерой. Его позиция в сцене - это точка удара: " +
                 "замах и уход отсчитываются вверх от неё")]
        [SerializeField] private Transform _press;

        [Tooltip("Оттиск в центре экрана")]
        [SerializeField] private Image _imprint;

        private CancellationTokenSource _cts;

        private Vector3 _struckPosition;
        private Vector3 _readyPosition;
        private Vector3 _hiddenPosition;
        private Vector3 _imprintScale;

        private void Awake()
        {
            if (!IsWiredUp())
            {
                enabled = false;
                return;
            }

            // Точку удара задаёт сцена: подогнать оснастку под кадр можно мышью, не трогая код
            _struckPosition = _press.localPosition;
            _readyPosition = _struckPosition + Vector3.up * _definition.ReadyLift;
            _hiddenPosition = _struckPosition + Vector3.up * _definition.HiddenLift;
            _imprintScale = _imprint.rectTransform.localScale;

            Clear();
        }

        private void OnDestroy() => CancelPlayback();

        /// <summary>
        /// Реплики инспектора живут в пузыре над его головой - экранный слой их не дублирует
        /// </summary>
        public void Say(string line)
        {
        }

        /// <inheritdoc />
        public void ShowVerdict(in VerdictReport report)
        {
            if (!enabled) return;

            CancelPlayback();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            PlayAsync(report.IsBust, _cts.Token).Forget();
        }

        /// <summary>
        /// Инспектор закончил - экран пустеет сразу. Досмотр может оборваться на любом кадре
        /// удара, и оттиск не должен пережить ящик, о котором он был
        /// </summary>
        public void Clear()
        {
            CancelPlayback();

            _press.localPosition = _hiddenPosition;
            _press.gameObject.SetActive(false);

            _imprint.rectTransform.localScale = _imprintScale;
            _imprint.color = Fade(_imprint.color, 0f);
            _imprint.enabled = false;
        }

        private async UniTaskVoid PlayAsync(bool bust, CancellationToken token)
        {
            _imprint.sprite = _definition.StampFor(bust);
            _imprint.color = Fade(_imprint.color, 0f);
            _imprint.enabled = false;

            _press.localPosition = _hiddenPosition;
            _press.gameObject.SetActive(true);

            if (await MoveAsync(_hiddenPosition, _readyPosition, _definition.EnterSeconds, EaseOut, token)) return;
            if (await MoveAsync(_readyPosition, _struckPosition, _definition.StrikeSeconds, EaseIn, token)) return;

            // С момента удара оттиск проступает сам по себе: оснастка успевает уйти из кадра,
            // пока печать ещё держится - иначе она загораживала бы собственный отпечаток
            ImprintAsync(token).Forget();

            if (await Pause.ForAsync(_definition.PressHoldSeconds, token)) return;
            if (await MoveAsync(_struckPosition, _hiddenPosition, _definition.LeaveSeconds, EaseIn, token)) return;

            _press.gameObject.SetActive(false);
        }

        /// <summary>Оттиск: проступает, садясь с чуть большего размера, держится и тает</summary>
        private async UniTaskVoid ImprintAsync(CancellationToken token)
        {
            _imprint.enabled = true;

            float elapsed = 0f;

            while (elapsed < _definition.ImprintFadeInSeconds)
            {
                if (await NextFrame(token)) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _definition.ImprintFadeInSeconds);

                _imprint.color = Fade(_imprint.color, t);
                _imprint.rectTransform.localScale =
                    _imprintScale * Mathf.LerpUnclamped(_definition.ImprintPunchScale, 1f, EaseOut(t));
            }

            _imprint.color = Fade(_imprint.color, 1f);
            _imprint.rectTransform.localScale = _imprintScale;

            if (await Pause.ForAsync(_definition.ImprintHoldSeconds, token)) return;

            elapsed = 0f;

            while (elapsed < _definition.ImprintFadeOutSeconds)
            {
                if (await NextFrame(token)) return;

                elapsed += Time.deltaTime;
                _imprint.color = Fade(
                    _imprint.color, 1f - Mathf.Clamp01(elapsed / _definition.ImprintFadeOutSeconds));
            }

            _imprint.enabled = false;
        }

        /// <summary>Провести оснастку между двумя точками. <c>true</c> - показ прервали</summary>
        private async UniTask<bool> MoveAsync(
            Vector3 from, Vector3 to, float seconds, Func<float, float> ease, CancellationToken token)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                if (await NextFrame(token)) return true;

                elapsed += Time.deltaTime;
                _press.localPosition = Vector3.LerpUnclamped(from, to, ease(Mathf.Clamp01(elapsed / seconds)));
            }

            _press.localPosition = to;

            return false;
        }

        private static UniTask<bool> NextFrame(CancellationToken token) =>
            UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();

        private static float EaseOut(float t)
        {
            float rest = 1f - t;
            return 1f - rest * rest * rest;
        }

        private static float EaseIn(float t) => t * t;

        private static Color Fade(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void CancelPlayback()
        {
            if (_cts == null) return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private bool IsWiredUp()
        {
            if (_definition != null && _press != null && _imprint != null)
                return true;

            Debug.LogError(
                $"Печати досмотра '{name}' не назначены оснастка, оттиск или ассет настроек - " +
                "вердикт показать будет нечем.", this);

            return false;
        }
    }
}
