using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    public sealed class StampDecal : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _propertyBlock;

        public void Show(StampDefinition stamp)
        {
            if (stamp == null) 
                return;

            if (_renderer == null) 
                _renderer = GetComponentInChildren<Renderer>();

            if (_renderer == null) 
                return;

            _propertyBlock ??= new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, stamp.Color);

            if (stamp.Texture != null) 
                _propertyBlock.SetTexture(BaseMapId, stamp.Texture);

            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
