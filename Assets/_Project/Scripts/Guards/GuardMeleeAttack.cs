using System.Collections.Generic;
using CrateExpectations.Combat;
using UnityEngine;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Детекция попаданий стражника: пока идёт активная фаза удара, вокруг клинка
    /// проверяется объём, и всё, что в него попало, получает <see cref="HitInfo"/>.
    /// Тот же расклад, что у <c>PlayerMeleeAttack</c>: единственное место, которое решает,
    /// во что попали, а фидбек про физику не знает и подписывается отдельно.
    /// <para>
    /// <b>Геометрия отличается от игрока, и отличается вынужденно.</b> У игрока свип идёт
    /// из КАМЕРЫ вперёд, потому что вьюмодель врёт про положение в мире и детекция обязана
    /// совпадать с прицелом. У стражника камеры нет, зато есть настоящий клинок в руке,
    /// который анимация честно проносит через дугу, - поэтому объём строится вокруг него.
    /// </para>
    /// <para>
    /// Клинок <c>Ulfberht</c> направлен вдоль <b>локальной оси +Y</b> смонтированного
    /// оружия, а не +Z: капсула кладётся от кисти вдоль <c>up</c>, и перепутать это
    /// с <c>forward</c> означало бы проверять объём поперёк меча.
    /// </para>
    /// </summary>
    public sealed class GuardMeleeAttack : MonoBehaviour
    {
        /// <summary>Сколько целей максимум засчитываем за одну проверку</summary>
        private const int MaxHitsPerCast = 8;

        [Tooltip("Откуда берутся объём, урон и маска. Тот же ассет, что у погони и фаз")]
        [SerializeField] private GuardCombatDefinition _combat;

        [Tooltip("Сокет, в котором висит оружие. Ссылка ведёт ДО сокета, а не до клинка: " +
                 "сам клинок создаётся в рантайме (WeaponSocket.Mount), и сериализовать " +
                 "ссылку на него невозможно")]
        [SerializeField] private WeaponSocket _socket;

        // Буфер под каждую проверку. Размер фиксирован потолком, а не берётся из ассета:
        // ассет правят слайдером в play mode, и пересоздавать массив в момент удара -
        // это аллокация ровно там, где её не должно быть
        private readonly Collider[] _hits = new Collider[MaxHitsPerCast];

        // По одному попаданию на коллайдер за взмах. Без этого проверка, идущая каждый
        // кадр активной фазы, засчитала бы двадцать ударов по одному игроку
        private readonly HashSet<Collider> _alreadyHit = new();

        private bool _sweeping;

        private void Awake()
        {
            if (_combat != null && _socket != null)
                return;

            Debug.LogError($"Удару стражника '{name}' не назначен боевой ассет или сокет - " +
                           "попадания считать неоткуда.", this);
            enabled = false;
        }

        /// <summary>
        /// Открыть активное окно. Зовётся <see cref="GuardAttackState"/> на переходе
        /// Windup → Active, ровно там же, где включается гипер-армор
        /// </summary>
        public void BeginSwing()
        {
            _sweeping = true;
            _alreadyHit.Clear();
        }

        /// <summary>
        /// Закрыть активное окно. Зовётся на переходе Active → Recovery, а также
        /// из <c>Exit</c> состояния - удар может оборваться и не доиграв
        /// </summary>
        public void EndSwing() => _sweeping = false;

        /// <summary>
        /// Проверка идёт КАЖДЫЙ кадр активной фазы, а не один раз в её начале.
        /// Единственная проверка в момент открытия окна оставляла бы мёртвую зону
        /// длиной во всю активную фазу: игрок, шагнувший под удар на её середине,
        /// прошёл бы сквозь клинок и не понял, почему.
        /// <para>
        /// Именно <c>LateUpdate</c>, как и у игрока: поза скелета применяется между
        /// <c>Update</c> и <c>LateUpdate</c>, и проверять объём вокруг клинка раньше
        /// значит проверять его вокруг позы прошлого кадра
        /// </para>
        /// </summary>
        private void LateUpdate()
        {
            if (!_sweeping)
                return;

            GameObject mounted = _socket.Mounted;

            if (mounted == null)
                return;

            Transform blade = mounted.transform;

            // Капсула вдоль клинка: от кисти до кончика по ЛОКАЛЬНОЙ +Y оружия
            Vector3 hilt = blade.position;
            Vector3 tip = hilt + blade.up * _combat.BladeLength;

            int count = Physics.OverlapCapsuleNonAlloc(
                hilt, tip, _combat.SweepRadius, _hits,
                _combat.AttackMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
                Resolve(_hits[i], hilt, tip);
        }

        private void Resolve(Collider collider, Vector3 hilt, Vector3 tip)
        {
            if (collider == null || !_alreadyHit.Add(collider))
                return;

            Vector3 bladeCenter = (hilt + tip) * 0.5f;

            // Куда полетит цель: ОТ стражника по горизонтали. По взгляду клинка считать
            // нельзя - в середине дуги он смотрит вбок, и удар отбрасывал бы игрока
            // мимо того, кто его нанёс
            Vector3 push = collider.transform.position - transform.position;
            push.y = 0f;
            push = push.sqrMagnitude > 1e-4f ? push.normalized : transform.forward;

            // Точка касания - ближайшая к клинку точка на цели; нормаль смотрит от цели
            // обратно к клинку, чтобы искры били навстречу удару, а не внутрь игрока
            Vector3 point = collider.ClosestPoint(bladeCenter);
            Vector3 toBlade = bladeCenter - point;
            Vector3 normal = toBlade.sqrMagnitude > 1e-4f ? toBlade.normalized : -push;

            // Тир жёстко Heavy, и это решение, а не недосмотр: удар стражника не заряжается,
            // выбора между лёгким и тяжёлым у него нет вовсе, а половинчатый тир означал бы,
            // что игрок иногда не понимает, что его задели. Появится заряд - появится и выбор
            var info = new HitInfo(
                point, normal, push, _combat.AttackImpulse, _combat.AttackDamage, AttackTier.Heavy);

            // Тот же поиск вверх по иерархии, что у игрока: коллайдер может лежать
            // и на корне цели, и на её части
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();

            damageable?.TakeHit(info);
        }
    }
}
