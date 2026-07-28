using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Указка инспектора: маленький маркер, который встаёт на осматриваемую грань ящика
    /// и краснеет, когда там что-то не сошлось. Нужен, чтобы шаги осмотра читались с экрана
    /// без пояснений - какой именно признак смотрят прямо сейчас.
    /// Объёмный примитив, а не Quad: у Quad нормаль вдоль локального -Z, и повёрнутая
    /// "по forward" плоскость молча исчезает за backface culling (грабли Фазы 2)
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class ExamineFocusMarker : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Цвет маркера, когда на этой грани всё в порядке")]
        [SerializeField] private Color _neutralColor = new(0.95f, 0.9f, 0.55f, 1f);

        [Tooltip("Цвет маркера, когда инспектор нашёл здесь улику")]
        [SerializeField] private Color _suspiciousColor = new(0.95f, 0.25f, 0.2f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            Hide();
        }

        /// <summary>Показать маркер в точке осмотра</summary>
        /// <param name="worldPoint">Куда встать - точка на осматриваемой грани</param>
        /// <param name="suspicious">На этой грани нашлась улика</param>
        public void Show(Vector3 worldPoint, bool suspicious)
        {
            transform.position = worldPoint;
            SetColor(suspicious ? _suspiciousColor : _neutralColor);
            _renderer.enabled = true;
        }

        /// <summary>Убрать маркер</summary>
        public void Hide()
        {
            if (_renderer != null) _renderer.enabled = false;
        }

        private void SetColor(Color color)
        {
            // MaterialPropertyBlock: цвет меняется без инстансов материалов и лишних батчей
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
