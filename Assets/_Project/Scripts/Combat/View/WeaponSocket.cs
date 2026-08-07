using UnityEngine;
using UnityEngine.Rendering;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Точка крепления оружия в ладони. Висит на пустышке под костью кисти и умеет ровно две
    /// вещи: собрать оружие из ассета и показать/спрятать его. Ни про ввод, ни про состояния
    /// не знает - иначе оружие нельзя было бы повесить на кого-то, кроме игрока
    /// </summary>
    public sealed class WeaponSocket : MonoBehaviour
    {
        [Tooltip("Какой блок посадки из ассета оружия читать. Тело и вьюмодель держат " +
                 "одну и ту же саблю по-разному: одна лежит в кулаке анатомически, " +
                 "вторая - как удобно кадру")]
        [SerializeField] private WeaponSocketSlot _slot = WeaponSocketSlot.Body;

        [Tooltip("Как рисовать собранное оружие. На физическом теле - ShadowsOnly: " +
                 "рука с саблей в кадр от первого лица не попадает, но тень на земле " +
                 "должна остаться с клинком. На вьюмодели - Off: она рисуется поверх мира " +
                 "и второй тени давать не должна")]
        [SerializeField] private ShadowCastingMode _shadows = ShadowCastingMode.On;

        private WeaponDefinition _weapon;
        private GameObject _instance;
        private Transform _mounted;

        /// <summary>Собранное оружие. <c>null</c>, пока <see cref="Mount"/> не вызвали</summary>
        public GameObject Mounted => _instance;

        /// <summary>
        /// Создаёт экземпляр оружия в сокете и применяет посадку из ассета.
        /// Повторный вызов заменяет прежнее оружие
        /// </summary>
        public void Mount(WeaponDefinition weapon)
        {
            if (weapon == null || weapon.Prefab == null)
            {
                Debug.LogError($"Сокету '{name}' нечего вешать: ассет оружия или его префаб не назначен.", this);
                return;
            }

            if (_instance != null)
                Destroy(_instance);

            _weapon = weapon;
            _instance = Instantiate(weapon.Prefab, transform);
            _mounted = _instance.transform;

            ApplyFit();
            ApplyPresentation();
        }

        /// <summary>
        /// Посадку применяем покадрово, а не один раз в <see cref="Mount"/>. Это числа под
        /// ручной подбор, и крутят их слайдерами прямо в play mode - результат должен быть
        /// виден сразу, а не после переспавна оружия. Аллокаций здесь нет, сокетов на игроке
        /// два, и оба применяют по три присваивания в кадр
        /// </summary>
        private void LateUpdate()
        {
            if (_mounted != null)
                ApplyFit();
        }

        private void ApplyFit()
        {
            WeaponFit fit = _slot == WeaponSocketSlot.ViewModel
                ? _weapon.ViewModelSocket
                : _weapon.BodySocket;

            _mounted.localPosition = fit.Position;
            _mounted.localRotation = Quaternion.Euler(fit.Rotation);
            _mounted.localScale = Vector3.one * fit.Scale;
        }

        /// <summary>
        /// Ножен в проекте нет, поэтому убранное оружие просто выключается: рисовать его
        /// на поясе всё равно нечем
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_instance != null)
                _instance.SetActive(visible);
        }

        /// <summary>
        /// Оружие рисуется так же, как рука, которая его держит: слой и режим теней берутся
        /// у самого сокета, а не у префаба оружия. Один и тот же ассет сабли висит и на
        /// физическом теле, и на вьюмодели - если бы посадка не приносила с собой слой,
        /// обе копии попали бы в кадр обеим камерам сразу
        /// </summary>
        private void ApplyPresentation()
        {
            int layer = gameObject.layer;

            foreach (Transform part in _instance.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = layer;

            foreach (Renderer renderer in _instance.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = _shadows;
        }
    }
}
