using CrateExpectations.Combat;
using CrateExpectations.Player.Combat;
using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Взмах вьюмодели, нарисованный кривыми вместо клипа. Слой поверх стойки
    /// (<see cref="IViewModelLayer"/>): подобранное руками кадрирование остаётся базой,
    /// дуга ложится добавкой к нему.
    /// <para>
    /// Почему не клип: клип взмаха авторен для вида от третьего лица, и дуга в нём уходит
    /// вниз-вправо - туда, где у камеры от первого лица нет кадра. Подогнать его доворотом
    /// костей нельзя: доворот смещает стойку, но не трогает то, куда клип уводит руку дальше.
    /// Кривая в пространстве камеры, наоборот, считается прямо в тех координатах, в которых
    /// композицию и проверяют.
    /// </para>
    /// <para>
    /// Физическое тело клип при этом продолжает играть: оно даёт тень на земле, и увидеть
    /// его дугу игрок может только через неё. Расхождение намеренное - см.
    /// <see cref="PlayerAnimatorDriver"/>.
    /// </para>
    /// </summary>
    public sealed class ViewModelSwing : MonoBehaviour, IViewModelLayer
    {
        [Tooltip("Чей взмах отыгрываем. Отсюда же берётся ассет дуги и длительность взмаха")]
        [SerializeField] private PlayerWeaponController _weapon;

        // Прогресс считаем своим таймером, а не спрашиваем у машины состояний. Машина -
        // чистая логика с покрытыми тестами, и заводить в ней поле ради одного слоя картинки
        // значило бы менять её контракт под нужды рендера. Источник темпа при этом всё равно
        // один - AttackDuration из ассета оружия, тот же, из которого машина берёт свой
        private float _elapsed;
        private bool _swinging;

        private void Awake()
        {
            if (_weapon == null)
            {
                Debug.LogError($"Взмаху вьюмодели '{name}' не назначено оружие - " +
                               "не от чего отсчитывать дугу.", this);
                enabled = false;
            }
        }

        private void OnEnable() => _weapon.StateChanged += OnWeaponStateChanged;

        private void OnDisable() => _weapon.StateChanged -= OnWeaponStateChanged;

        private void OnWeaponStateChanged(WeaponState state)
        {
            if (state != WeaponState.Attacking)
            {
                // Взмах прервали убиранием оружия - дугу обрываем, а не доигрываем:
                // сабля к этому моменту уже уехала из руки
                _swinging = false;
                return;
            }

            _elapsed = 0f;
            _swinging = true;
        }

        /// <summary>
        /// Время копим здесь, а не в <see cref="Evaluate"/>: слой опрашивают из
        /// <c>LateUpdate</c> рига, и класть в опрос накопление состояния значит завязать
        /// результат на то, сколько раз за кадр слой спросили
        /// </summary>
        private void Update()
        {
            if (!_swinging || _weapon.Weapon == null)
                return;

            _elapsed += Time.deltaTime;

            if (_elapsed >= Duration)
                _swinging = false;
        }

        /// <inheritdoc />
        public ViewModelOffset Evaluate()
        {
            // Слой остаётся в списке рига, даже если сам себя выключил в Awake: порядок
            // наложения задаётся порядком компонентов, и выпадение одного из них сдвинуло бы
            // остальные. Молчим добавкой, а не отсутствием
            if (_weapon == null || _weapon.Weapon == null)
                return ViewModelOffset.None;

            SwingDefinition swing = _weapon.Weapon.Swing;

            // Тумблер A/B проверяем покадрово: его щёлкают живьём, чтобы сравнить дугу
            // с клипом, и ждать перезапуска play mode ради этого не должно быть нужно
            if (!_swinging || swing == null || !swing.UseProceduralSwing)
                return ViewModelOffset.None;

            float t = Mathf.Clamp01(_elapsed / Duration);

            return new ViewModelOffset(
                swing.Pivot,
                Quaternion.Euler(swing.EvaluateAngles(t)),
                swing.EvaluateOffset(t));
        }

        /// <summary>
        /// Нормализованный прогресс внутри взмаха, 0..1. Наружу нужен инструментам:
        /// по нему снимают кадры фаз дуги, не подлавливая момент руками
        /// </summary>
        public float Progress => _swinging ? Mathf.Clamp01(_elapsed / Duration) : 0f;

        private float Duration => Mathf.Max(_weapon.Weapon.AttackDuration, 0.01f);
    }
}
