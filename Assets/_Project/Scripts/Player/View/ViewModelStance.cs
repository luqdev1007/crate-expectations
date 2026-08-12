using CrateExpectations.Core.Hands;
using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Стойка вьюмодели по занятости рук: слой (<see cref="IViewModelLayer"/>), который
    /// сдвигает всю модель на подобранную для этой занятости добавку.
    /// <para>
    /// Зачем понадобился: базовый корень (<see cref="ViewModelFramingDefinition.RootPosition"/>)
    /// и довороты костей одни на всю вьюмодель. Боевая композиция подобрана под них и верна,
    /// а клипы переноски приехали из анимации от третьего лица и держат кисти ниже нижней
    /// кромки кадра. Поднять их можно только тем, что различает состояния, - а до сих пор
    /// у кадрирования не было ничего, что их различает.
    /// </para>
    /// <para>
    /// Слоем, а не полем в <see cref="ViewModelRig"/>, ровно по причине из
    /// <see cref="IViewModelLayer"/>: риг складывает добавки и не обязан знать, откуда они
    /// берутся. Иначе ригу пришлось бы держать ссылку на модель занятости, то есть знать
    /// про груз, саблю и листок - при том что его работа это одна строка присваивания
    /// в трансформ.
    /// </para>
    /// <para>
    /// Своей логики здесь нет ни строки: что считать переноской, решает
    /// <see cref="HandsState"/>, насколько поднимать - ассет кадрирования. Слой только
    /// сглаживает переход между их ответами.
    /// </para>
    /// </summary>
    public sealed class ViewModelStance : MonoBehaviour, IViewModelLayer
    {
        [Tooltip("Откуда берутся смещения стойки и время перехода. Ассет, а не поля " +
                 "на объекте: правки должны переживать выход из play mode")]
        [SerializeField] private ViewModelFramingDefinition _framing;

        private HandsState _hands;

        // Текущее смещение и скорость его переезда - обе величины принадлежат сглаживанию,
        // а не занятости: занятость меняется мгновенно, кадр за ней доезжает
        private Vector3 _offset;
        private Vector3 _velocity;

        /// <summary>
        /// Отдаёт слою модель занятости. Ссылкой из <c>GameLifetimeScope</c>, как и
        /// <see cref="ViewModelBody"/>: занятость вычисляется из трёх источников, и собрать
        /// её на месте слой не может - ему пришлось бы найти все три
        /// </summary>
        public void BindHands(HandsState hands) => _hands = hands;

        private void Awake()
        {
            if (_framing == null)
            {
                Debug.LogError($"Стойке вьюмодели '{name}' не назначен ассет кадрирования - " +
                               "смещать нечем.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Смещение копим здесь, а не в <see cref="Evaluate"/>: слой опрашивают из
        /// <c>LateUpdate</c> рига, и класть накопление в опрос значит завязать результат
        /// на то, сколько раз за кадр слой спросили.
        /// <para>
        /// Цель перечитываем каждый кадр, а не только на смене занятости: меняться ей
        /// всё равно нечем, кроме смены режима, зато правка ползунка в ассете прямо
        /// в play mode доезжает сразу - ради этого числа и вынесены в ассет.
        /// Аллокаций тут нет: и занятость, и смещение - значимые типы
        /// </para>
        /// </summary>
        private void Update()
        {
            if (_hands == null)
                return;

            Vector3 target = _framing.StanceOffsetFor(_hands.Occupancy);

            _offset = Vector3.SmoothDamp(_offset, target, ref _velocity, _framing.StanceBlend);
        }

        /// <summary>
        /// Только перенос, без поворота - поэтому точка вращения здесь не значит ничего.
        /// Стойку поднимают параллельно, а не заваливают: доворот корня увёл бы дуги
        /// ударов туда, где их никто не считал
        /// </summary>
        public ViewModelOffset Evaluate() => _hands == null
            ? ViewModelOffset.None
            : new ViewModelOffset(Vector3.zero, Quaternion.identity, _offset);
    }
}
