using UnityEngine;

namespace CrateExpectations.Core.View
{
    /// <summary>
    /// Держит объект развёрнутым к камере. Один компонент на все мировые плашки:
    /// реплика инспектора, карточка груза и всё, что появится дальше.
    /// </summary>
    public sealed class CameraBillboard : MonoBehaviour
    {
        [Tooltip("Камера, к которой разворачиваемся. Пусто - возьмём Camera.main один раз в Awake")]
        [SerializeField] private Camera _camera;

        private Transform _transform;
        private Transform _eye;

        /// <summary>Камера, к которой развёрнута плашка: пригождается тем, кто считает её место в кадре.</summary>
        public Camera Eye => _camera;

        private void Awake()
        {
            _transform = transform;

            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
            {
                Debug.LogError(
                    $"Плашке '{name}' не назначена камера, и Camera.main тоже не нашлась. " +
                    "Разворачиваться к игроку она не будет.", this);

                enabled = false;
                return;
            }

            _eye = _camera.transform;
        }

        // Догоняем камеру именно в LateUpdate: к этому моменту Cinemachine уже подвинул её
        // за этот кадр, иначе плашка отставала бы на кадр и дрожала при повороте головы.
        private void LateUpdate() => FaceCamera();

        /// <summary>
        /// Разворачивает плашку к камере. Нужен тем, кто переставляет её вручную
        /// и не хочет ждать следующего кадра с чужой ориентацией.
        /// </summary>
        public void FaceCamera()
        {
            if (_eye == null)
                return;

            // Не LookAt: тот целится осью Z в точку и оставляет up мировым, поэтому при взгляде
            // сверху или снизу плашку кренит, а над объектом - переворачивает.
            // Копия ориентации камеры держит её строго параллельной экрану с любого ракурса.
            _transform.rotation = _eye.rotation;
        }
    }
}
