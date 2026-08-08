using System;
using System.Collections.Generic;
using CrateExpectations.Combat;
using UnityEngine;

namespace CrateExpectations.Player.Combat
{
    /// <summary>
    /// Детекция попаданий взмаха: свип капсулой из камеры вперёд на активной фазе.
    /// Единственное место, которое решает, во что попали; всё остальное - звук, искры,
    /// заморозка, тычок камеры - подписывается на его события и про физику не знает.
    /// <para>
    /// Свип идёт ИЗ КАМЕРЫ, а не по клинку вьюмодели. Вьюмодель врёт про положение
    /// в мире: она подвешена под камеру, снимается своим объективом и подобрана под то,
    /// как красиво лежит в кадре. Её клинок физически находится в паре десятков
    /// сантиметров от глаза, а не там, где игрок его видит. Детекция обязана совпадать
    /// с прицелом, иначе попадание расходится с намерением ровно на величину подгонки кадра.
    /// </para>
    /// <para>
    /// Прогресс взмаха считается своим таймером, а не спрашивается у
    /// <see cref="WeaponStateMachine"/>: машина - чистая логика с покрытыми тестами,
    /// и заводить в ней поле ради детекции значило бы менять её контракт. Источник темпа
    /// при этом один и тот же - <c>AttackDuration</c> из ассета оружия.
    /// </para>
    /// </summary>
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        /// <summary>Потолок числа сэмплов - тот же, что стоит ограничением в ассете оружия</summary>
        private const int MaxSamples = 16;

        /// <summary>Сколько целей максимум засчитываем за один свип</summary>
        private const int MaxHitsPerCast = 8;

        [Tooltip("Чей взмах отслеживаем. Отсюда же приходят все числа свипа")]
        [SerializeField] private PlayerWeaponController _weapon;

        [Tooltip("Откуда пускать свип. Камера игрока: удар должен идти туда, куда смотрят")]
        [SerializeField] private Transform _origin;

        // Буферы под каждый кадр атаки. Размеры фиксированы потолками, а не берутся
        // из ассета: ассет правят слайдером в play mode, и пересоздавать массивы
        // в момент удара - это аллокация ровно там, где её не должно быть
        private readonly float[] _sampleTimes = new float[MaxSamples];
        private readonly RaycastHit[] _hits = new RaycastHit[MaxHitsPerCast];

        // По одному попаданию на коллайдер за взмах. Без этого свип, идущий шесть раз
        // за окно, засчитал бы шесть ударов по одному ящику
        private readonly HashSet<Collider> _alreadyHit = new();

        private bool _swinging;
        private float _elapsed;
        private float _previousProgress;

        private Vector3 _previousOrigin;
        private Vector3 _previousForward;

        /// <summary>Взмах начался. Момент для свиста, а не для удара</summary>
        public event Action AttackStarted;

        /// <summary>
        /// Свип во что-то попал. Второй аргумент - задетый коллайдер: он нужен тем,
        /// кто цепляет к цели эффект, и тем, кто различает живую цель и стену
        /// </summary>
        public event Action<HitInfo, Collider> Hit;

        private void Awake()
        {
            if (_weapon == null || _origin == null)
            {
                Debug.LogError($"Удару игрока '{name}' не назначено оружие или камера - " +
                               "попадания считать неоткуда.", this);
                enabled = false;
            }
        }

        private void OnEnable() => _weapon.StateChanged += OnWeaponStateChanged;

        private void OnDisable() => _weapon.StateChanged -= OnWeaponStateChanged;

        private void OnWeaponStateChanged(WeaponState state)
        {
            if (state != WeaponState.Attacking)
            {
                // Взмах прервали убиранием оружия - активное окно закрываем, а не
                // доигрываем: сабли в руке к этому моменту уже нет
                _swinging = false;
                return;
            }

            _elapsed = 0f;

            // Строго меньше начала окна, иначе самый первый сэмпл потеряется:
            // расписание отдаёт интервал, открытый слева
            _previousProgress = -1f;
            _swinging = true;
            _alreadyHit.Clear();

            _previousOrigin = _origin.position;
            _previousForward = _origin.forward;

            AttackStarted?.Invoke();
        }

        /// <summary>
        /// Именно <c>LateUpdate</c>: поза вьюмодели и её слои применяются здесь же, и
        /// детекция должна видеть тот кадр, который игрок в итоге увидит, а не
        /// промежуточный. Плюс камеру к этому моменту уже довернул Cinemachine -
        /// свип из <c>Update</c> уходил бы по прицелу прошлого кадра
        /// </summary>
        private void LateUpdate()
        {
            if (!_swinging || _weapon.Weapon == null)
            {
                CacheOrigin();
                return;
            }

            WeaponDefinition weapon = _weapon.Weapon;
            float duration = Mathf.Max(weapon.AttackDuration, 0.01f);

            _elapsed += UnityEngine.Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / duration);

            int count = weapon.Sweep.Collect(_previousProgress, progress, _sampleTimes);

            for (int i = 0; i < count; i++)
                CastAt(weapon, SampleFraction(_sampleTimes[i], progress));

            _previousProgress = progress;

            if (_elapsed >= duration)
                _swinging = false;

            CacheOrigin();
        }

        /// <summary>
        /// Доля пути между прошлым кадром и текущим, на которую приходится сэмпл.
        /// Нужна, чтобы свип шёл из той позы камеры, в которой игрок был в этот момент,
        /// а не из той, куда он успел довернуться к концу кадра. На быстром развороте
        /// разница между ними - десятки градусов
        /// </summary>
        private float SampleFraction(float sampleTime, float progress)
        {
            float span = progress - _previousProgress;

            return span <= 0f ? 1f : Mathf.Clamp01((sampleTime - _previousProgress) / span);
        }

        private void CastAt(WeaponDefinition weapon, float fraction)
        {
            Vector3 origin = Vector3.Lerp(_previousOrigin, _origin.position, fraction);
            Vector3 forward = Vector3.Slerp(_previousForward, _origin.forward, fraction).normalized;

            // Капсула стоит вертикально относительно взгляда. При нулевой высоте оба конца
            // совпадают и капсула вырождается в сферу - ровно тот объём, что задан радиусом
            Vector3 up = _origin.up * (weapon.SweepHeight * 0.5f);
            Vector3 top = origin + up;
            Vector3 bottom = origin - up;

            int count = Physics.CapsuleCastNonAlloc(
                top, bottom, weapon.SweepRadius, forward, _hits,
                weapon.SweepDistance, weapon.HitMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
                Resolve(weapon, _hits[i], forward);
        }

        private void Resolve(WeaponDefinition weapon, RaycastHit hit, Vector3 forward)
        {
            Collider collider = hit.collider;

            if (collider == null || !_alreadyHit.Add(collider))
                return;

            // Свип, начавшийся уже внутри коллайдера, отдаёт нулевую дистанцию и мусорную
            // нормаль. Подставляем нормаль навстречу удару - искры всё равно должны быть
            Vector3 normal = hit.distance > 0f ? hit.normal : -forward;
            Vector3 point = hit.distance > 0f ? hit.point : collider.ClosestPoint(_origin.position);

            var info = new HitInfo(point, normal, forward, weapon.Impulse, weapon.Damage);

            // Ищем вверх по иерархии, а не только на самом коллайдере: у ящика коллайдер
            // лежит на корне, а у составной цели окажется на отдельной части.
            // GetComponentInParent начинает с самого объекта, поэтому оба случая покрыты
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();

            damageable?.TakeHit(info);

            // Событие поднимается ВСЕГДА, даже если бить было некого. Удар по стене и
            // по палубе обязан давать искры и звук: беззвучный промах по геометрии
            // читается как "меня не засчитали", а не как "я задел камень"
            Hit?.Invoke(info, collider);
        }

        private void CacheOrigin()
        {
            _previousOrigin = _origin.position;
            _previousForward = _origin.forward;
        }
    }
}
