using CrateExpectations.Core.Events;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Ответ на зов о помощи: докричались - бегу. Органов чувств здесь нет вовсе -
    /// ни конуса, ни рейкаста, ни даже стен: кричат громко и специально для стражи,
    /// а не роняют ящик за углом.
    /// <para>
    /// <b>Радиус приходит В СОБЫТИИ, а не лежит в ассете стражника</b>, и это
    /// центральное решение компонента. Громкость - свойство крика, а не уха:
    /// один и тот же зов обязан поднять всех, до кого он достал, одинаково.
    /// Держи радиус слушатель - стражники с разными ассетами слышали бы один
    /// и тот же крик по-разному, и настройка тревоги расползлась бы по префабам.
    /// </para>
    /// <para>
    /// <b>Отдельный компонент от <see cref="GuardWitness"/></b> намеренно, хотя оба
    /// кончаются одной строчкой: тот отвечает «заметил ли сам», этот - «дозвались ли
    /// до него». Разные поводы сработать, разные числа и разные причины снять
    /// компонент с префаба: глухого стражника делают снятием первого, неподчиняющегося
    /// приказу - снятием второго.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(GuardAI))]
    public sealed class GuardAlertResponder : MonoBehaviour
    {
        private GuardAI _guard;
        private IEventBus _bus;

        [Inject]
        public void Construct(IEventBus bus) => _bus = bus;

        private void Awake() => _guard = GetComponent<GuardAI>();

        /// <summary>
        /// Подписка в <c>Start</c> по той же причине, что у <see cref="GuardWitness"/>:
        /// инъекция стражникам раздаётся из <c>RegisterBuildCallback</c> скоупа,
        /// то есть в чужом <c>Awake</c>, и в <c>OnEnable</c> шины может ещё не быть
        /// </summary>
        private void Start() => _bus?.Subscribe<AlertRaised>(OnAlertRaised);

        private void OnDestroy() => _bus?.Unsubscribe<AlertRaised>(OnAlertRaised);

        private void OnAlertRaised(AlertRaised alert)
        {
            if (_guard.IsAggro)
                return;

            // Та же проверка дистанции, что у слуха, и намеренно она же: зов - это
            // и есть звук, просто заведомо громкий. Своей арифметики компонент
            // не заводит - иначе «слышно» считалось бы в проекте двумя разными способами
            if (PerceptionCheck.CanHear(transform.position, alert.Position, alert.Radius))
                _guard.RaiseAggro();
        }
    }
}
