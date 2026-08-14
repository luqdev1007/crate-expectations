using CrateExpectations.Core.Events;
using CrateExpectations.Inspection.Events;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Зов стражи: инспектор задержал груз - и кричит. Слушает вердикт на шине,
    /// на <see cref="Verdict.IsBust"/> публикует <see cref="AlertRaised"/> со своей
    /// позицией и радиусом из ассета.
    /// <para>
    /// <b>Отдельный компонент, а не строчка в <c>InspectorAI.CloseCase</c></b>, и это
    /// та же дисциплина, по которой в проекте живёт <c>CargoHandoff</c>: модуль досмотра
    /// сознательно не решает, что происходит ПОСЛЕ вердикта. Он объявляет решение,
    /// а увозить груз, начислять штраф и звать стражу - дело подписчиков. Снять зов
    /// с объекта инспектора должно быть можно, не трогая сам досмотр.
    /// </para>
    /// <para>
    /// <b>Про стражников не знает ни слова.</b> <c>Inspection</c> не видит <c>Guards</c>
    /// и видеть не должен: наружу уходит только «здесь кричали, слышно на столько-то»,
    /// а кто прибежит - и прибежит ли кто-нибудь вообще - вопрос не к инспектору.
    /// Событие поэтому лежит в <c>Core</c> (единственная сборка, видимая обеим сторонам),
    /// а не в модуле досмотра.
    /// </para>
    /// </summary>
    public sealed class InspectorAlarm : MonoBehaviour
    {
        [Tooltip("Откуда берётся радиус зова. Тот же ассет, что задаёт процедуру досмотра")]
        [SerializeField] private InspectionDefinition _definition;

        private IEventBus _bus;

        [Inject]
        public void Construct(IEventBus bus) => _bus = bus;

        private void Awake()
        {
            if (_definition != null)
                return;

            Debug.LogError($"Зову стражи '{name}' не назначен InspectionDefinition - " +
                           "радиус крика брать неоткуда.", this);
            enabled = false;
        }

        /// <summary>
        /// Подписка в <c>Start</c>, а не в <c>OnEnable</c>: <c>LifetimeScope</c> раздаёт
        /// зависимости в своём <c>Awake</c>, и в <c>OnEnable</c> шины может ещё не быть.
        /// Тот же приём, что у остальных сценовых слушателей проекта
        /// </summary>
        private void Start() => _bus?.Subscribe<CargoInspected>(OnCargoInspected);

        private void OnDestroy() => _bus?.Unsubscribe<CargoInspected>(OnCargoInspected);

        private void OnCargoInspected(CargoInspected inspected)
        {
            // Пропущенный груз - не повод для тревоги: инспектор попрощался и всё
            if (!inspected.Verdict.IsBust)
                return;

            // Позиция - своя собственная. Из события её не взять: CargoInspected.Inspector -
            // это ScriptableObject-характер, один на всех инспекторов, а не объект сцены.
            // Поэтому компонент и висит на самом инспекторе, а не резолвится из контейнера
            _bus.Publish(new AlertRaised(transform.position, _definition.AlarmRadius));
        }
    }
}
