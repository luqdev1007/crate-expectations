using CrateExpectations.Combat;
using UnityEngine;

namespace CrateExpectations.Player.Combat
{
    /// <summary>
    /// Переводит попадание в искры и след на поверхности. Сам ничего не создаёт -
    /// просит пул, а тот переиспользует уже готовые системы частиц.
    /// <para>
    /// След цепляется к цели только тогда, когда цель способна уехать: у неподвижной
    /// палубы родитель не нужен, а вот отметина на ящике обязана лететь вместе с ним.
    /// Отличаем по наличию нестатичного <see cref="Rigidbody"/> - именно он и означает
    /// "эта штука может оказаться в другом месте".
    /// </para>
    /// </summary>
    public sealed class MeleeImpactEffects : MonoBehaviour
    {
        [Tooltip("Чьи попадания показываем")]
        [SerializeField] private PlayerMeleeAttack _attack;

        [Tooltip("Кто выдаёт готовые системы частиц")]
        [SerializeField] private ImpactEffectPool _pool;

        private void Awake()
        {
            if (_attack == null || _pool == null)
            {
                Debug.LogError($"Эффектам удара '{name}' не назначен удар или пул - " +
                               "искр не будет.", this);
                enabled = false;
            }
        }

        private void OnEnable() => _attack.Hit += OnHit;

        private void OnDisable() => _attack.Hit -= OnHit;

        private void OnHit(HitInfo hit, Collider collider)
        {
            Rigidbody body = collider == null ? null : collider.attachedRigidbody;
            Transform anchor = body != null && !body.isKinematic ? body.transform : null;

            _pool.Play(hit.Point, hit.Normal, anchor);
        }
    }
}
