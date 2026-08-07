using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Доворачивает кости руки вьюмодели поверх того, что записал в них клип. Это основной
    /// инструмент кадрирования: TPS-клипы держат саблю у корпуса, и кисть оказывается позади
    /// плоскости глаз - её надо вывести вперёд, оставив плечо за ближней плоскостью отсечения.
    /// <para>
    /// Почему не разворотом всей модели: разворот корня меняет и дугу замаха. Клип, у которого
    /// сабля шла сверху вниз, после разворота на десятки градусов пойдёт куда угодно, и
    /// проверять композицию придётся заново на каждой фазе атаки. Доворот кости - это смещение
    /// стартовой позы: замах остаётся тем же замахом, просто из другого положения руки.
    /// </para>
    /// <para>
    /// Кости берутся через <see cref="Animator.GetBoneTransform"/>, а не по именам в иерархии:
    /// имена зависят от того, кто экспортировал модель, а карта гуманоидных костей - нет.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class ViewModelPoseOverride : MonoBehaviour
    {
        [Tooltip("Откуда берутся довороты. Ассет, а не поля на объекте: правки должны " +
                 "переживать выход из play mode")]
        [SerializeField] private ViewModelFramingDefinition _framing;

        private Animator _animator;
        private Transform[] _bones;

        // Поза, которую в кость записал клип, и та, которую после доворота записали мы.
        // Пара нужна, чтобы отличить "аниматор обновил позу" от "аниматор промолчал"
        private Quaternion[] _clipPose;
        private Quaternion[] _appliedPose;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_framing == null)
            {
                Debug.LogError($"Вьюмодели '{name}' не назначен ассет кадрирования - доворачивать нечем.", this);
                enabled = false;
                return;
            }

            if (!_animator.isHuman)
            {
                Debug.LogError($"Аватар вьюмодели '{name}' не гуманоидный - " +
                               "кости по HumanBodyBones не найти.", this);
                enabled = false;
                return;
            }

            ResolveBones();
        }

        /// <summary>
        /// Кости ищем один раз: <see cref="Animator.GetBoneTransform"/> лезет в карту аватара,
        /// и вызывать его покадрово на каждую кость незачем
        /// </summary>
        private void ResolveBones()
        {
            ViewModelFramingDefinition.BoneOverride[] overrides = _framing.Bones;
            _bones = new Transform[overrides.Length];
            _clipPose = new Quaternion[overrides.Length];
            _appliedPose = new Quaternion[overrides.Length];

            for (int i = 0; i < overrides.Length; i++)
            {
                _bones[i] = _animator.GetBoneTransform(overrides[i].Bone);

                if (_bones[i] == null)
                {
                    Debug.LogWarning($"У вьюмодели '{name}' нет кости {overrides[i].Bone} - " +
                                     "её доворот игнорируется.", this);
                    continue;
                }

                _clipPose[i] = _bones[i].localRotation;
                _appliedPose[i] = _clipPose[i];
            }
        }

        /// <summary>
        /// Именно <c>LateUpdate</c>: при <see cref="AnimatorUpdateMode"/> Normal аниматор пишет позу
        /// между <c>Update</c> и <c>LateUpdate</c>. Доворот, применённый раньше, был бы затёрт
        /// клипом в тот же кадр
        /// </summary>
        private void LateUpdate()
        {
            ViewModelFramingDefinition.BoneOverride[] overrides = _framing.Bones;

            // Список правят руками прямо в play mode - на добавленную строку отвечаем
            // пересборкой кэша, а не выходом за границы массива
            if (_bones.Length != overrides.Length)
                ResolveBones();

            for (int i = 0; i < _bones.Length; i++)
            {
                if (_bones[i] == null)
                    continue;

                Quaternion current = _bones[i].localRotation;

                // Аниматор пишет позу не каждый кадр: на Time.timeScale = 0 он молчит вовсе.
                // Наивное "localRotation *= доворот" в такие кадры домножалось бы на само себя,
                // и рука уезжала бы по спирали за одну паузу. Поэтому храним обе позы и
                // обновляем базу только тогда, когда в кости лежит НЕ наш прошлый результат
                if (!IsSameRotation(current, _appliedPose[i]))
                    _clipPose[i] = current;

                Quaternion result = _clipPose[i] * Quaternion.Euler(overrides[i].Euler);
                _bones[i].localRotation = result;
                _appliedPose[i] = result;
            }
        }

        /// <summary>
        /// Сравнение кватернионов по модулю знака: <c>q</c> и <c>-q</c> - один и тот же поворот
        /// </summary>
        private static bool IsSameRotation(Quaternion a, Quaternion b) =>
            Mathf.Abs(Quaternion.Dot(a, b)) > 0.999999f;
    }
}
