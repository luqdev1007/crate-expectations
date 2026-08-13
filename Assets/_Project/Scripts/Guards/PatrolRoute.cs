using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Маршрут обхода: набор точек на доке, по которым ходит стражник.
    /// <para>
    /// Живёт в сцене отдельным объектом, а НЕ на префабе стражника, и это не мелочь
    /// расстановки: маршрут - свойство места, а не типа NPC. Один и тот же префаб
    /// стражника стоит на посту у трапа и обходит склад - разница только в том,
    /// назначена ему ссылка на маршрут или нет.
    /// </para>
    /// <para>
    /// Точки - <b>прямые дети этого объекта в порядке иерархии</b>, без ручного массива
    /// в инспекторе. Добавил пустышку-ребёнка, подвинул - точка готова; порядок правится
    /// перетаскиванием в иерархии, а не правкой индексов. Ручной массив пришлось бы
    /// чинить после каждого добавления точки, и рассинхрон в нём не виден глазом
    /// </para>
    /// </summary>
    public sealed class PatrolRoute : MonoBehaviour
    {
        [Tooltip("Замкнут ли маршрут. Петля - обход по кругу без конца; выключено - " +
                 "стражник останавливается на последней точке. Сейчас все маршруты " +
                 "кладём петлёй, поле оставлено под маршрут 'туда-обратно' в будущем")]
        [SerializeField] private bool _loop = true;

        // Дети не меняются по ходу игры, а список нужен на каждой смене точки:
        // собирается один раз, дальше только читается
        private readonly List<Transform> _points = new(8);

        /// <summary>Точки маршрута в порядке иерархии</summary>
        public IReadOnlyList<Transform> Points => _points;

        /// <summary>Замкнут ли маршрут в кольцо</summary>
        public bool Loop => _loop;

        private void Awake() => Rebuild();

        /// <summary>Пересобрать список точек по текущим детям</summary>
        private void Rebuild()
        {
            _points.Clear();

            foreach (Transform point in transform)
                _points.Add(point);

            if (_points.Count == 0)
                Debug.LogWarning($"У маршрута '{name}' нет ни одной точки - " +
                                 "стражник, которому его назначили, никуда не пойдёт.", this);
        }

#if UNITY_EDITOR
        // Рисуется в редакторе, чтобы маршрут был виден без Play mode: точки читаются
        // прямо из иерархии, а не из собранного списка, - в edit mode Awake не вызывался
        private void OnDrawGizmos()
        {
            int count = transform.childCount;
            if (count == 0) return;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < count; i++)
            {
                Vector3 point = transform.GetChild(i).position;
                Gizmos.DrawSphere(point, PointGizmoRadius);

                bool isLast = i == count - 1;
                if (isLast && !_loop) continue;

                // Последняя точка петли замыкается на нулевую
                Vector3 next = transform.GetChild(isLast ? 0 : i + 1).position;
                Gizmos.DrawLine(point, next);
            }
        }

        private const float PointGizmoRadius = 0.15f;
#endif
    }
}
