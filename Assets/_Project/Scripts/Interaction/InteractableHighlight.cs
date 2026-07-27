using UnityEngine;

namespace CrateExpectations.Interaction
{
    public sealed class InteractableHighlight : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Color _highlightTint = new(1f, 0.92f, 0.55f);
        [SerializeField][Range(0f, 1f)] private float _highlightBlend = 0.55f;

        private MaterialPropertyBlock _propertyBlock;
        private Color _baseColor = Color.white;
        private bool _isHighlighted;

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>();

            _propertyBlock = new MaterialPropertyBlock();

            if (_renderers.Length > 0 && _renderers[0].sharedMaterial != null &&
                _renderers[0].sharedMaterial.HasProperty(BaseColorId))
            {
                _baseColor = _renderers[0].sharedMaterial.GetColor(BaseColorId);
            }

            Apply();
        }

        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            Apply();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_isHighlighted == highlighted)
                return;

            _isHighlighted = highlighted;
            Apply();
        }

        private void Apply()
        {
            Color color = _isHighlighted
                ? Color.Lerp(_baseColor, _highlightTint, _highlightBlend)
                : _baseColor;

            _propertyBlock.SetColor(BaseColorId, color);

            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].SetPropertyBlock(_propertyBlock);
        }
    }
}
