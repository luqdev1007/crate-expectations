using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Отдаёт телу игрока его собственную скорость - двумя числами, которые читает
    /// блендтри ног. Единственное место, которое вообще знает, что у ног есть анимация:
    /// снимите компонент - контроллер продолжит ходить, просто тело будет стоять
    /// в покое.
    /// <para>
    /// Скорость берётся у <c>Rigidbody</c>, а не у ввода, и это не мелочь: игрок
    /// упирается в стены, его толкают, он едет по инерции после рывка - во всех этих
    /// случаях ввод говорит одно, а тело едет другое. Ноги обязаны отыгрывать
    /// то, что происходит, а не то, что нажато.
    /// </para>
    /// <para>
    /// Правило перевода живёт в <see cref="LocomotionBlend"/> - обычном классе, покрытом
    /// тестами. Здесь остаётся только ввод-вывод: снять скорость, перевести её в оси
    /// тела, записать в аниматор.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerLocomotionAnimator : MonoBehaviour
    {
        // Имена параметров объявлены в PlayerAnimatorBuilder - там граф и собирается.
        // Здесь они только хешируются, как и во всех остальных водителях аниматора
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveZId = Animator.StringToHash("MoveZ");

        [Tooltip("Аниматор физического тела. Вьюмодели рук эти параметры не адресованы: " +
                 "её граф про ноги ничего не знает, и запись в несуществующий параметр - " +
                 "это варнинг в консоль на каждый кадр")]
        [SerializeField] private Animator _body;

        [Tooltip("Откуда максимальная скорость: ровно тот же ассет, по которому " +
                 "контроллер и разгоняется. Второе число здесь означало бы, что " +
                 "ноги бегут не с той скоростью, с которой едет игрок")]
        [SerializeField] private PlayerMovementDefinition _definition;

        private readonly LocomotionBlend _blend = new LocomotionBlend();

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            if (_body == null || _definition == null)
            {
                Debug.LogError(
                    $"Локомоции игрока '{name}' не назначены аниматор тела или ассет движения - " +
                    "ноги останутся в покое.", this);

                enabled = false;
            }
        }

        /// <summary>
        /// Покадрово, а не в <c>FixedUpdate</c>: аниматор считает позу в кадре, и
        /// параметры, записанные с частотой физики, доехали бы до него ступеньками
        /// </summary>
        private void Update()
        {
            // Из мировых осей - в оси тела. Тело всегда развёрнуто по взгляду, поэтому
            // «вперёд» для ног - это туда, куда смотрит игрок, а не куда он идёт
            Vector3 local = transform.InverseTransformDirection(_rigidbody.linearVelocity);

            Vector2 blend = _blend.Tick(
                local,
                _definition.MoveSpeed,
                _definition.LocomotionSmoothTime,
                _definition.LocomotionDeadband,
                Time.deltaTime);

            _body.SetFloat(MoveXId, blend.x);
            _body.SetFloat(MoveZId, blend.y);
        }
    }
}
