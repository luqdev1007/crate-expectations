using CrateExpectations.Player.Combat;
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
    /// Руки видны ровно тогда, когда в них есть сабля. Посадка вьюмодели подобрана под
    /// боевую стойку (<see cref="ViewModelRig"/>), а безоружный клип разводит руки по
    /// бокам - при той же посадке в кадр въезжает ладонь поперёк экрана. Отдельной
    /// безоружной стойки от первого лица в проекте нет, поэтому без оружия рук в кадре
    /// просто нет, ровно как было до появления вьюмодели.
    /// </para>
    /// </summary>
    public sealed class ViewModelBody : MonoBehaviour
    {
        [Tooltip("Единственный рендерер, который может оказаться в кадре - руки")]
        [SerializeField] private Renderer _armsRenderer;

        [Tooltip("Чьё оружие держим в кадре. Руки появляются и исчезают вместе с саблей")]
        [SerializeField] private PlayerWeaponController _weapon;

        private void Awake()
        {
            if (_armsRenderer == null || _weapon == null)
            {
                Debug.LogError($"Вьюмодели '{name}' не назначен рендерер рук или оружие - показывать нечего.", this);
                enabled = false;
                return;
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private void OnEnable() => _weapon.WeaponVisibilityChanged += OnWeaponVisibilityChanged;

        private void OnDisable() => _weapon.WeaponVisibilityChanged -= OnWeaponVisibilityChanged;

        private void OnWeaponVisibilityChanged(bool visible) => _armsRenderer.enabled = visible;
    }
}
