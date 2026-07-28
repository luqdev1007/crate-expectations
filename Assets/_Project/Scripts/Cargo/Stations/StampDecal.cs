using UnityEngine;

namespace CrateExpectations.Cargo.Stations
{
    /// <summary>
    /// Одна декаль-пломба из пула. Ничего не решает - только показывает переданную печать
    /// </summary>
    public sealed class StampDecal : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [Tooltip("Рендерер декали. Пусто - берётся с этого объекта")]
        [SerializeField] private Renderer _renderer;

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>Показать печать. Вызывается пулом при выдаче и при замене пломбы</summary>
        public void Show(StampDefinition stamp)
        {
            if (stamp == null) return;

            // Ленивая инициализация: объект в пуле может ни разу не проснуться до первой выдачи
            if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null) return;
            _propertyBlock ??= new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, stamp.Color);
            if (stamp.Texture != null) _propertyBlock.SetTexture(BaseMapId, stamp.Texture);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
