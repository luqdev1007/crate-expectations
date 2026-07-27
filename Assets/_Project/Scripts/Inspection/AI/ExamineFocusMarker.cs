using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    [RequireComponent(typeof(Renderer))]
    public sealed class ExamineFocusMarker : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color _neutralColor = new(0.95f, 0.9f, 0.55f, 1f);
        [SerializeField] private Color _suspiciousColor = new(0.95f, 0.25f, 0.2f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();

            Hide();
        }

        public void Show(Vector3 worldPoint, bool suspicious)
        {
            transform.position = worldPoint;
            SetColor(suspicious ? _suspiciousColor : _neutralColor);
            _renderer.enabled = true;
        }

        public void Hide()
        {
            if (_renderer != null) 
                _renderer.enabled = false;
        }

        private void SetColor(Color color)
        {
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
