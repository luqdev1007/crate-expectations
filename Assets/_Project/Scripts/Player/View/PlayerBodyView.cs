using UnityEngine;
using UnityEngine.Rendering;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Физическое тело игрока. Висит на корне модели, который лежит дочерним объектом под
    /// физическим корнем игрока - оттуда тело бесплатно получает позицию и yaw, причём yaw
    /// уже интерполированный, тот же самый, по которому строится камера.
    /// <para>
    /// В кадр это тело не попадает вообще: все его рендереры переведены в
    /// <see cref="ShadowCastingMode.ShadowsOnly"/>. Изнутри модели видно только её изнанку,
    /// а боевые клипы держат оружие у бедра - то есть за нижней границей кадра. Руки с
    /// саблей, которые игрок видит перед собой, - отдельная вьюмодель под камерой
    /// (<see cref="ViewModelRig"/>); физическое тело остаётся ради тени на земле, физики
    /// и того, что его видят NPC.
    /// </para>
    /// <para>
    /// Камера при этом остаётся снаружи модели и к кости головы не привязана: боевые клипы
    /// мотают голову с амплитудой, от которой в первом лице укачивает. Наклон вверх-вниз
    /// живёт на пивоте камеры и в тело не уходит вообще - тело только поворачивается
    /// вслед за взглядом по горизонтали.
    /// </para>
    /// </summary>
    public sealed class PlayerBodyView : MonoBehaviour
    {
        [Tooltip("Пивот, на котором PlayerController крутит наклон камеры. " +
                 "Этот компонент задаёт ему только высоту, поворот не трогает")]
        [SerializeField] private Transform _cameraPivot;

        [Tooltip("Высота глаз над корнем тела (подошвами), м. Анатомическое число: " +
                 "модель ростом ~1.64 м смотрит примерно с 1.55")]
        [SerializeField][Min(0f)] private float _cameraHeight = 1.55f;

        private void Awake()
        {
            if (_cameraPivot == null)
            {
                Debug.LogError($"Телу игрока '{name}' не назначен пивот камеры - высоту глаз задавать нечему.", this);
                enabled = false;
                return;
            }

            HideBody();
        }

        /// <summary>
        /// Тело убираем из кадра, но не из мира: <c>enabled = false</c> унёс бы вместе с ним
        /// и тень, и игрок перестал бы отбрасывать силуэт на землю
        /// </summary>
        private void HideBody()
        {
            int hidden = 0;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                hidden++;
            }

            if (hidden == 0)
                Debug.LogWarning($"У тела игрока '{name}' нет ни одного рендерера - " +
                                 "тени на земле не будет.", this);
        }

        /// <summary>
        /// Высоту применяем покадрово, а не один раз в <c>Awake</c>: поле подбирают руками,
        /// в том числе на ходу в play mode, и результат должен быть виден сразу
        /// </summary>
        private void LateUpdate()
        {
            _cameraPivot.position = transform.position + transform.up * _cameraHeight;
        }
    }
}
