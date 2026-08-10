using CrateExpectations.Core.Hands;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Вьюмодель собрана из того же префаба, что и физическое тело, но в кадре от неё нужны
    /// только руки: торс, ноги и голова стоят вплотную к камере и показали бы изнанку.
    /// Ноги в кадре не появятся вовсе - это осознанный размен схемы "тело отдельно,
    /// вьюмодель отдельно".
    /// <para>
    /// Лишние рендереры именно выключаются, а не гасятся в <c>ShadowsOnly</c>, как у
    /// физического тела: тень игрок уже отбрасывает телом, и вторая тень от вьюмодели была
    /// бы двойником, стоящим вплотную к первому. По той же причине руки не отбрасывают
    /// тень сами.
    /// </para>
    /// <para>
    /// Руки в кадре тогда, когда они ЧЕМ-ТО ЗАНЯТЫ, - с саблей, с грузом или с листком.
    /// Раньше условие было одно: «сабля видна», - и руки появлялись строго вместе с ней.
    /// Теперь спрашиваем занятость у <see cref="HandsState"/>, а не заводим второй список
    /// условий: у переноски и листка своё состояние, и любая местная копия «а сейчас руки
    /// нужны?» разъехалась бы с ним в первый же раз, когда груз выпадет сам.
    /// </para>
    /// <para>
    /// Сабля к этому отношения не имеет: её показывает и прячет свой сокет по видимости
    /// оружия. Руки и оружие теперь появляются по разным правилам - и это ровно то, ради
    /// чего правило вынесено наружу
    /// </para>
    /// </summary>
    public sealed class ViewModelBody : MonoBehaviour
    {
        [Tooltip("Единственный рендерер, который может оказаться в кадре - руки")]
        [SerializeField] private Renderer _armsRenderer;

        private HandsState _hands;

        // Совпадает с тем, что сделал Awake: там погашены все рендереры, включая руки
        private bool _visible;

        /// <summary>
        /// Отдаёт вьюмодели модель занятости. Ссылкой из <c>GameLifetimeScope</c>, а не
        /// поиском по типу, и это не вкусовщина: вьюмоделей в сцене несколько - живая
        /// и отключённые остатки прежних сборок, - а поиск по иерархии отдаёт ПЕРВУЮ,
        /// то есть отключённую. Модель уехала бы в мёртвый объект, руки не появились бы
        /// никогда, и в консоли при этом не было бы ни строчки
        /// </summary>
        public void BindHands(HandsState hands) => _hands = hands;

        private void Awake()
        {
            if (_armsRenderer == null)
            {
                Debug.LogError($"Вьюмодели '{name}' не назначен рендерер рук - показывать нечего.", this);
                enabled = false;
                return;
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>
        /// Занятость не «меняется», а вычисляется из трёх источников - события у неё нет,
        /// и опрос здесь не лень, а единственный честный способ её узнать. Рендерер при этом
        /// трогаем только на смене: <c>enabled</c> у Unity-объекта не бесплатный
        /// </summary>
        private void Update()
        {
            if (_hands == null)
                return;

            bool visible = _hands.Occupancy != HandsOccupancy.Free;

            if (visible == _visible)
                return;

            _visible = visible;
            _armsRenderer.enabled = visible;
        }
    }
}
