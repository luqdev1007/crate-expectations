using UnityEngine;
using UnityEngine.AI;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Переводит фактическую скорость агента в параметр <c>Speed</c> аниматора.
    /// Именно адаптер, а не поведение - по образцу <c>InspectorAnimationView</c>:
    /// состояния патруля про аниматор не знают и знать не должны, они двигают агента,
    /// а чем это отыграть, решает этот компонент. Снять анимацию с NPC можно удалением
    /// одного компонента, и обход продолжит работать
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GuardAnimatorDriver : MonoBehaviour
    {
        // Хеш считается один раз на тип, а не строкой каждый кадр: строковый вызов
        // SetFloat("Speed") хеширует заново и аллоцирует - ноль аллокаций в Update
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        private Animator _animator;
        private NavMeshAgent _agent;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
        }

        /// <summary>
        /// Пишется в <c>Update</c>, а не в <c>LateUpdate</c>, и это не мелочь: аниматор
        /// считает позу МЕЖДУ ними, поэтому параметр, выставленный в <c>LateUpdate</c>,
        /// доезжает до картинки только следующим кадром. У инспектора это сделано
        /// наоборот, потому что там скорость меряется смещением тела и до конца кадра
        /// её просто нет; здесь скорость готова заранее - её отдаёт сам агент.
        /// <para>
        /// В параметр уходят <b>метры в секунду без нормировки</b>: пороги блендтри
        /// стражника заданы в м/с (0 / 1.17 / 2.78, фаза B §6.4) - в отличие от
        /// инспектора, у которого <c>Speed</c> нормирован на его маршевую скорость
        /// </para>
        /// </summary>
        private void Update() => _animator.SetFloat(SpeedId, _agent.velocity.magnitude);
    }
}
