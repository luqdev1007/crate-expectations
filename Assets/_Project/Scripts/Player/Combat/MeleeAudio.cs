using CrateExpectations.Combat;
using UnityEngine;

namespace CrateExpectations.Player.Combat
{
    /// <summary>
    /// Звук взмаха и попадания.
    /// <para>
    /// Приём с разбросом высоты тона взят из прототипа и он не косметический: свист
    /// звучит каждый раз чуть иначе, поэтому серия ударов не превращается в стрекот
    /// автомата, а вот звук попадания играется ВСЕГДА одной высотой. Ровно этим он и
    /// читается как событие: ухо ловит повторяющийся, всегда одинаковый сигнал на фоне
    /// плавающего свиста.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MeleeAudio : MonoBehaviour
    {
        [Tooltip("Чей взмах озвучиваем. Клипы приходят из ассета его оружия")]
        [SerializeField] private PlayerMeleeAttack _attack;

        [Tooltip("Чем машем - отсюда берутся клипы")]
        [SerializeField] private PlayerWeaponController _weapon;

        [Tooltip("Разброс высоты тона свиста. Ноль - все взмахи звучат одинаково")]
        [SerializeField][Range(0f, 0.5f)] private float _swingPitchSpread = 0.1f;

        private AudioSource _source;

        // Одно попадание - один звук, даже если взмах задел троих. Два наложенных
        // удара звучат не вдвое сильнее, а как щелчок: фазы складываются и получается
        // артефакт вместо акцента
        private bool _hitSoundPlayed;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            if (_attack == null || _weapon == null)
            {
                Debug.LogError($"Звуку боя '{name}' не назначен удар или оружие - " +
                               "озвучивать нечего.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _attack.AttackStarted += OnAttackStarted;
            _attack.Hit += OnHit;
        }

        private void OnDisable()
        {
            _attack.AttackStarted -= OnAttackStarted;
            _attack.Hit -= OnHit;
        }

        private void OnAttackStarted()
        {
            _hitSoundPlayed = false;

            AudioClip clip = _weapon.Weapon == null ? null : _weapon.Weapon.SwingSound;

            if (clip == null)
                return;

            _source.pitch = 1f + Random.Range(-_swingPitchSpread, _swingPitchSpread);
            _source.PlayOneShot(clip);
        }

        private void OnHit(HitInfo hit, Collider collider)
        {
            if (_hitSoundPlayed)
                return;

            AudioClip clip = _weapon.Weapon == null ? null : _weapon.Weapon.HitSound;

            if (clip == null)
                return;

            _hitSoundPlayed = true;

            // Высота тона сбрасывается жёстко, а не оставляется от свиста: иначе удар
            // унаследовал бы случайный разброс и перестал быть одинаковым
            _source.pitch = 1f;
            _source.PlayOneShot(clip);
        }
    }
}
