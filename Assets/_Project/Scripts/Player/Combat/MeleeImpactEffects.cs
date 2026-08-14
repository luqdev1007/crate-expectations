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
    /// Признак «может оказаться в другом месте» - <b>наличие</b> <see cref="Rigidbody"/>,
    /// а не его динамичность: кинематическое тело двигают не силы, а чужой код
    /// (у живого стражника - <c>NavMeshAgent</c>), и уезжает оно ничуть не хуже.
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

        private void OnHit(HitInfo hit, Collider collider) =>
            _pool.Play(hit.Point, hit.Normal, AnchorFor(collider));

        /// <summary>
        /// К чему привязать след, чтобы он остался на том месте цели, куда пришёлся удар.
        /// <c>null</c> - остаться в мире.
        /// <para>
        /// Кинематическое тело раньше приравнивалось к неподвижному, и на живом стражнике
        /// это было видно: корень у него кинематический (его везёт <c>NavMeshAgent</c>,
        /// а не физика), поэтому отметина оставалась висеть в воздухе там, где стражник
        /// был в момент удара, и он уходил из-под неё.
        /// </para>
        /// <para>
        /// Отсутствие <see cref="Rigidbody"/> - <b>не</b> тот же случай, и уравнивать их
        /// нельзя. Палуба и стены тела не имеют и сдвинуться не могут, а привязка к ним
        /// не бесплатна: след наследует масштаб родителя, и у стены с
        /// <c>lossyScale = (21, 3, 0.5)</c> его растянуло бы в сорок два раза по одной
        /// оси. Поэтому статика по-прежнему остаётся без якоря
        /// </para>
        /// </summary>
        private static Transform AnchorFor(Collider collider)
        {
            if (collider == null)
                return null;

            Rigidbody body = collider.attachedRigidbody;

            if (body == null)
                return null;

            // От тела, а не от коллайдера: у составной цели задет может быть дочерний
            // коллайдер, а уезжает - тело целиком, и цепляться надо к тому, что уезжает
            return body.transform;
        }
    }
}
