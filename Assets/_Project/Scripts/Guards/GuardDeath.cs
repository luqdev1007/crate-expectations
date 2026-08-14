using CrateExpectations.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Стражник кончился: анимация уступает место физике, из руки выпадает клинок.
    /// Срабатывает ровно один раз - за это отвечает <c>HealthState</c>, который
    /// по мёртвому не бьёт и второй раз о смерти не объявляет.
    /// <para>
    /// Порядок операций здесь - не стилистика, а условие работоспособности, и он
    /// расписан по шагам в <see cref="Die"/>. Коротко: сперва замолкают все, кто
    /// двигает кости, и только потом просыпается регдол. Включи физику раньше, чем
    /// выключишь аниматор, - и они начнут переписывать одни и те же трансформы
    /// в одном кадре: тело задёргается на месте вместо падения.
    /// </para>
    /// <para>
    /// Состоянием FSM смерть НЕ сделана, в отличие от вздрагивания (блок 6), и это
    /// осознанно. Состояние - это то, из чего есть выход; смерть - это то, после чего
    /// машину состояний выключают целиком вместе с её владельцем. Заводить состояние,
    /// которое первым делом отключает собственный <c>Update</c>, значит описывать
    /// конец через механизм продолжения
    /// </para>
    /// </summary>
    [RequireComponent(typeof(HealthComponent))]
    public sealed class GuardDeath : MonoBehaviour
    {
        [Tooltip("Слой регдола, множитель импульса и масса выпавшего клинка. " +
                 "Тот же ассет, что держит вздрагивание: падение - это тоже ответ на удар")]
        [SerializeField] private GuardReactionDefinition _reaction;

        [Tooltip("Откуда выпадет оружие. Пусто - стражник умрёт с пустыми руками, " +
                 "и это не ошибка: безоружный NPC - штатный случай")]
        [SerializeField] private WeaponSocket _socket;

        private HealthComponent _health;

        // Всё, что двигает тело само по себе. Собирается в Awake: в момент смерти
        // искать компоненты по иерархии поздно и незачем
        private Animator _animator;
        private NavMeshAgent _agent;
        private Behaviour[] _drivers;

        private Collider _rootCollider;
        private Rigidbody _rootBody;

        // Кости регдола. Коллайдер и тело лежат ПАРАМИ на одном объекте, поэтому
        // хранятся тоже парами: включать их порознь - значит однажды включить
        // коллайдер без тела (см. про щит в Awake)
        private Rigidbody[] _bones;
        private Collider[] _boneColliders;

        private int _ragdollLayer;

        private void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _rootCollider = GetComponent<Collider>();
            _rootBody = GetComponent<Rigidbody>();

            if (_reaction == null)
            {
                Debug.LogError($"Смерти стражника '{name}' не назначен GuardReactionDefinition - " +
                               "падать он будет нечем.", this);
                enabled = false;
                return;
            }

            _ragdollLayer = LayerMask.NameToLayer(_reaction.RagdollLayer);

            if (_ragdollLayer < 0)
            {
                // Не молча: слой с опечаткой означал бы, что труп остался на слое стражника,
                // то есть по нему продолжает проходить свип сабли
                Debug.LogError($"Слоя '{_reaction.RagdollLayer}' нет в проекте - труп " +
                               $"стражника '{name}' останется на слое живого.", this);
                _ragdollLayer = gameObject.layer;
            }

            // Всё, кроме этого компонента и самого здоровья: здоровье обязано пережить
            // смерть (по трупу спрашивают IsDead), а этот компонент выключит себя сам
            _drivers = new Behaviour[]
            {
                GetComponent<GuardAI>(),
                GetComponent<GuardAnimatorDriver>(),
                GetComponent<GuardHitReaction>(),
            };

            CollectRagdoll();
        }

        private void OnEnable() => _health.Died += Die;

        private void OnDisable() => _health.Died -= Die;

        /// <summary>
        /// Собирает кости регдола - строго те, у которых коллайдер и <see cref="Rigidbody"/>
        /// лежат НА ОДНОМ объекте.
        /// <para>
        /// Отбор не формальность. У стражника есть <c>Shield</c> с невыпуклым
        /// <c>MeshCollider</c> и без собственного тела: включи его заодно со всеми -
        /// и он прицепится к телу предплечья, а невыпуклый меш на некинематическом
        /// <see cref="Rigidbody"/> Unity не поддерживает и скажет об этом ошибкой
        /// в момент смерти. Правило «коллайдер там же, где тело» отсекает его само,
        /// не зная про щит ничего
        /// </para>
        /// </summary>
        private void CollectRagdoll()
        {
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(includeInactive: true);

            var bones = new System.Collections.Generic.List<Rigidbody>(bodies.Length);
            var colliders = new System.Collections.Generic.List<Collider>(bodies.Length);

            foreach (Rigidbody body in bodies)
            {
                // Корень - не кость: это капсула, которой стражник ходит, и падать ей нечем
                if (body == _rootBody)
                    continue;

                Collider collider = body.GetComponent<Collider>();

                if (collider == null)
                    continue;

                bones.Add(body);
                colliders.Add(collider);
            }

            _bones = bones.ToArray();
            _boneColliders = colliders.ToArray();

            if (_bones.Length > 0)
                return;

            Debug.LogWarning($"У стражника '{name}' не нашлось ни одной кости регдола - " +
                             "он умрёт стоя.", this);
        }

        private void Die(HitInfo lastHit)
        {
            // 1. Замолкают все, кто двигает кости. Аниматор - последним из них
            //    и обязательно ДО физики
            foreach (Behaviour driver in _drivers)
            {
                if (driver != null)
                    driver.enabled = false;
            }

            StopAgent();

            if (_animator != null)
                _animator.enabled = false;

            // 2. Капсула живого больше не нужна: иначе труп лежал бы внутри стоящего
            //    невидимого цилиндра, а игрок упирался бы в воздух над телом
            if (_rootCollider != null)
                _rootCollider.enabled = false;

            // 3. И только теперь физика
            EnableRagdoll(lastHit);
            DropWeapon();

            // Делать здесь больше нечего: второй смерти не будет
            enabled = false;
        }

        /// <summary>
        /// Агента гасим в два приёма. Просто выключить компонент мало: агент, снятый
        /// на ходу, оставляет за собой накопленную скорость, и первый же кадр регдола
        /// получил бы её в виде рывка вбок
        /// </summary>
        private void StopAgent()
        {
            if (_agent == null)
                return;

            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }

            _agent.enabled = false;
        }

        private void EnableRagdoll(HitInfo lastHit)
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].isKinematic = false;
                _boneColliders[i].enabled = true;

                // Слой ставится на объект кости, а не на всю иерархию: слои здесь
                // нужны физике, а мешу рук и головы они безразличны
                _bones[i].gameObject.layer = _ragdollLayer;
            }

            PushCorpse(lastHit);
        }

        /// <summary>
        /// Толкает труп добившим ударом - в ту кость, по которой пришлось попадание,
        /// а не всегда в таз. Разница видна сразу: удар в голову разворачивает тело,
        /// удар по ногам подсекает его
        /// </summary>
        private void PushCorpse(HitInfo lastHit)
        {
            Rigidbody target = NearestBone(lastHit.Point);

            if (target == null)
                return;

            float impulse = lastHit.Impulse * _reaction.DeathImpulseScale;

            if (impulse <= 0f)
                return;

            target.AddForce(lastHit.Direction.normalized * impulse, ForceMode.Impulse);
        }

        private Rigidbody NearestBone(Vector3 point)
        {
            Rigidbody nearest = null;
            float best = float.MaxValue;

            foreach (Rigidbody bone in _bones)
            {
                // Квадрат расстояния: корень здесь ничего не решает, а стоит.
                // Перебор разовый, в момент смерти, - в кадре он не живёт
                float distance = (bone.worldCenterOfMass - point).sqrMagnitude;

                if (distance >= best)
                    continue;

                best = distance;
                nearest = bone;
            }

            return nearest;
        }

        /// <summary>
        /// Роняет клинок. Массы у оружия в руке нет вовсе - оно ездит за костью, -
        /// поэтому <see cref="Rigidbody"/> появляется ровно здесь, в момент падения
        /// </summary>
        private void DropWeapon()
        {
            if (_socket == null)
                return;

            GameObject dropped = _socket.Release();

            if (dropped == null)
                return;

            // На слой регдола вместе с трупом: выпавший клинок - такой же обломок сцены,
            // и высекать из него искры саблей незачем
            foreach (Transform part in dropped.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = _ragdollLayer;

            Rigidbody body = dropped.GetComponent<Rigidbody>();

            if (body == null)
                body = dropped.AddComponent<Rigidbody>();

            body.mass = _reaction.DroppedWeaponMass;

            // Скорость руки в момент смерти клинку не передаётся: он падает из
            // разжавшейся ладони, а не летит из неё
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
