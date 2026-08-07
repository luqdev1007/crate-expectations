using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Вьюмодель - вторая копия модели игрока, подвешенная под камеру и нарисованная поверх
    /// мира отдельной overlay-камерой. Схема как в Skyrim / Dark Messiah: физическое тело
    /// остаётся в мире тенью и физикой (<see cref="PlayerBodyView"/>), а в кадре живут руки,
    /// которые не зависят ни от анатомии скелета, ни от того, куда TPS-клип уводит кисть.
    /// <para>
    /// В отличие от физического тела, вьюмодель наследует и yaw, и pitch - она дочерняя
    /// прямо к камере и никогда от неё не отстаёт.
    /// </para>
    /// <para>
    /// Компонент ставит модель под камеру и задаёт объектив. Композицию кадра при этом задаёт
    /// не он, а доворот костей (<see cref="ViewModelPoseOverride"/>): здесь остаётся только
    /// мелкая доводка. Разворот корня на десятки градусов - признак того, что композицию
    /// вытягивают не тем слоем.
    /// </para>
    /// <para>
    /// Поза корня складывается из слоёв: подобранная руками стойка из ассета кадрирования -
    /// это база, поверх неё ложатся добавки от всех <see cref="IViewModelLayer"/> на этом
    /// объекте (взмах, дальше - покачивание от ходьбы и увод при повороте камеры). Слоёв
    /// здесь не два и не три, их столько, сколько компонентов повесили: сложение идёт
    /// циклом, и новый слой не требует правок ни здесь, ни в соседях.
    /// </para>
    /// </summary>
    public sealed class ViewModelRig : MonoBehaviour
    {
        [Tooltip("Корень копии модели под камерой. Именно его двигают офсеты из ассета")]
        [SerializeField] private Transform _root;

        [Tooltip("Overlay-камера, которая рисует вьюмодель поверх мира")]
        [SerializeField] private Camera _camera;

        [Tooltip("Откуда берутся числа кадрирования. Ассет, а не поля на объекте: " +
                 "правки должны переживать выход из play mode")]
        [SerializeField] private ViewModelFramingDefinition _framing;

        // Слои собираются с этого же объекта, а не назначаются ссылками в инспекторе:
        // порядок наложения должен быть виден глазами, а порядок компонентов на объекте
        // виден - его же и берём. Список кэшируем, потому что GetComponents аллоцирует,
        // а вызывать его пришлось бы каждый кадр
        private IViewModelLayer[] _layers;

        private void Awake()
        {
            if (_root == null || _framing == null)
            {
                Debug.LogError($"Вьюмодели '{name}' не назначен корень модели или ассет кадрирования.", this);
                enabled = false;
                return;
            }

            if (_camera == null)
                Debug.LogWarning($"Вьюмодели '{name}' не назначена overlay-камера - " +
                                 "FOV останется тем, что стоит на камере вручную.", this);

            _layers = GetComponents<IViewModelLayer>();
        }

        /// <summary>
        /// Применяем покадрово, а не один раз в <c>Awake</c>: это числа под ручной подбор,
        /// и правят их в том числе на ходу в play mode - результат должен быть виден сразу.
        /// Аллокаций здесь нет, а вьюмодель под камерой всё равно одна
        /// </summary>
        private void LateUpdate()
        {
            var pose = new ViewModelPose(_framing.RootPosition, Quaternion.Euler(_framing.RootRotation));

            // Порядок наложения - порядок компонентов на объекте: слой видит позу,
            // которую собрали слои выше него
            for (int i = 0; i < _layers.Length; i++)
                pose = _layers[i].Evaluate().ApplyTo(pose);

            _root.SetLocalPositionAndRotation(pose.Position, pose.Rotation);

            if (_camera != null)
                _camera.fieldOfView = _framing.FieldOfView;
        }
    }
}
