using System;
using CrateExpectations.Interaction;
using UnityEngine;

namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Ящик груза: тонкий носитель двух личностей - истинной (<see cref="Identity"/>) и заявленной
    /// (<see cref="State"/>). Решать, как состояние меняется, - не его дело: это
    /// <see cref="DisguiseProcessor"/>. Здесь только хранение и отрисовка того, что уже решено.
    /// Ящик остаётся <see cref="Carriable"/>: одновременно физический груз и носитель состояния
    /// </summary>
    [RequireComponent(typeof(Carriable))]
    public sealed class CargoBox : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Что внутри на самом деле. Станции это не меняют никогда")]
        [SerializeField] private CargoTypeDefinition _trueType;

        [Tooltip("Заводская окраска для ящика, поставленного в сцену руками. У ящиков из " +
                 "каталога она приходит вместе с типом груза и это поле не читается.")]
        [SerializeField] private PaintDefinition _factoryPaint;

        [Tooltip("Рендереры корпуса - им применяется окраска. Пусто - берётся рендерер объекта")]
        [SerializeField] private Renderer[] _bodyRenderers;

        [Tooltip("Метка содержимого: красится в цвет заявленного типа груза (отклик на перелив)")]
        [SerializeField] private Renderer _labelRenderer;

        [Tooltip("Куда станция печати вешает декаль-пломбу")]
        [SerializeField] private Transform _stampAnchor;

        private MaterialPropertyBlock _propertyBlock;
        private CargoIdentity _identity;
        private CargoState _state;

        /// <summary>Истина о грузе. Только чтение - менять её некому и нечем</summary>
        public CargoIdentity Identity => _identity;

        /// <summary>Заявленное состояние - то, что увидит инспектор</summary>
        public CargoState State => _state;

        /// <summary>Точка крепления декали-пломбы</summary>
        public Transform StampAnchor => _stampAnchor != null ? _stampAnchor : transform;

        /// <summary>Груз в руках игрока - станция такой ящик "в зоне" не считает</summary>
        public bool IsCarried => _carriable != null && _carriable.IsCarried;

        /// <summary>Заявленное состояние сменилось</summary>
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

        /// <summary>
        /// Назначить истинный тип груза при спавне из каталога и сбросить заявленное состояние
        /// к "ничего не маскировали". Ящик, поставленный в сцену руками, обходится сериализованным
        /// полем и этот метод не трогает.
        ///
        /// <para>Заводская окраска берётся у типа груза, а не из поля префаба. Так префаб
        /// не ссылается ни на один ассет-определение - а значит, уезжая в Addressables-бандл,
        /// не тянет за собой их копии. Копия ассета сравнивается по ссылке с оригиналом
        /// как "другой ассет", и в билде это ломает всё, что построено на таком сравнении:
        /// зачёт контракта, улики инспектора, сохранение груза.</para>
        /// </summary>
        public void AssignIdentity(CargoTypeDefinition trueType)
        {
            _trueType = trueType;
            _identity = new CargoIdentity(trueType);

            PaintDefinition factoryPaint = trueType != null ? trueType.FactoryPaint : _factoryPaint;
            _state = CargoState.Undisguised(_identity, factoryPaint);

            ApplyVisuals();
            StateChanged?.Invoke(this);
        }

        /// <summary>Записать новое заявленное состояние и перерисовать ящик</summary>
        public void ApplyState(in CargoState state)
        {
            _state = state;
            ApplyVisuals();
            StateChanged?.Invoke(this);
        }

        private void ApplyVisuals()
        {
            if (_state.Paint != null) SetColor(_bodyRenderers, _state.Paint.Color);

            if (_labelRenderer != null && _state.DeclaredType != null)
                SetColor(_labelRenderer, _state.DeclaredType.LabelColor);
        }

        private void SetColor(Renderer[] renderers, Color color)
        {
            for (int i = 0; i < renderers.Length; i++) SetColor(renderers[i], color);
        }

        private void SetColor(Renderer target, Color color)
        {
            if (target == null) return;

            // MaterialPropertyBlock: цвет меняется без инстансов материалов и без новых батчей
            target.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            target.SetPropertyBlock(_propertyBlock);
        }
    }
}
