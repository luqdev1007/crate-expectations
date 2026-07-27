using System;
using CrateExpectations.Interaction;
using UnityEngine;

namespace CrateExpectations.Cargo
{
    [RequireComponent(typeof(Carriable))]
    public sealed class CargoBox : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Что внутри на самом деле")]
        [SerializeField] private CargoTypeDefinition _trueType;

        [Tooltip("Заводская окраска для ящика")]
        [SerializeField] private PaintDefinition _factoryPaint;

        [Tooltip("Рендереры корпуса")]
        [SerializeField] private Renderer[] _bodyRenderers;

        [Tooltip("Метка содержимого")]
        [SerializeField] private Renderer _labelRenderer;

        [Tooltip("Куда станция печати вешает декаль-пломбу")]
        [SerializeField] private Transform _stampAnchor;

        private MaterialPropertyBlock _propertyBlock;
        private CargoIdentity _identity;
        private CargoState _state;

        public CargoIdentity Identity => _identity;

        public CargoState State => _state;

        public Transform StampAnchor => _stampAnchor != null ? _stampAnchor : transform;

        public bool IsCarried => _carriable != null && _carriable.IsCarried;

        public event Action<CargoBox> StateChanged;

        private Carriable _carriable;

        private void Awake()
        {
            _carriable = GetComponent<Carriable>();
            _propertyBlock = new MaterialPropertyBlock();

            if (_bodyRenderers == null || _bodyRenderers.Length == 0)
                _bodyRenderers = GetComponentsInChildren<Renderer>();

            _identity = new CargoIdentity(_trueType);
            _state = CargoState.Undisguised(_identity, _factoryPaint);

            ApplyVisuals();
        }

        public void AssignIdentity(CargoTypeDefinition trueType)
        {
            _trueType = trueType;
            _identity = new CargoIdentity(trueType);

            PaintDefinition factoryPaint = trueType != null ? trueType.FactoryPaint : _factoryPaint;
            _state = CargoState.Undisguised(_identity, factoryPaint);

            ApplyVisuals();
            StateChanged?.Invoke(this);
        }

        public void ApplyState(in CargoState state)
        {
            _state = state;
            ApplyVisuals();
            StateChanged?.Invoke(this);
        }

        private void ApplyVisuals()
        {
            if (_state.Paint != null) 
                SetColor(_bodyRenderers, _state.Paint.Color);

            if (_labelRenderer != null && _state.DeclaredType != null)
                SetColor(_labelRenderer, _state.DeclaredType.LabelColor);
        }

        private void SetColor(Renderer[] renderers, Color color)
        {
            for (int i = 0; i < renderers.Length; i++) 
                SetColor(renderers[i], color);
        }

        private void SetColor(Renderer target, Color color)
        {
            if (target == null) 
                return;

            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            target.SetPropertyBlock(_propertyBlock);
        }
    }
}
