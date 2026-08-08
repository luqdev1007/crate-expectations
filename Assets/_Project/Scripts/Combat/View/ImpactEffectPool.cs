using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Пул эффектов попадания: искры и след на поверхности. Пул, а не
    /// <c>Instantiate</c>/<c>Destroy</c> в момент удара, - в бою эффект случается по
    /// нескольку раз в секунду, и создание с уничтожением объекта на каждое попадание
    /// это ровно тот мусор, из-за которого сборщик приходит посреди драки.
    /// <para>
    /// След цепляется к тому, во что попали, а не остаётся висеть в мире: ящик от удара
    /// улетает, и отметина, оставленная в мировых координатах, повисла бы в воздухе
    /// на том месте, где ящик был.
    /// </para>
    /// </summary>
    public sealed class ImpactEffectPool : MonoBehaviour
    {
        [Tooltip("Откуда берутся префабы эффектов и размер разогрева")]
        [SerializeField] private ImpactFeedbackDefinition _feedback;

        private readonly Stack<ParticleSystem> _freeSparks = new();
        private readonly Stack<ParticleSystem> _freeMarks = new();

        // Что сейчас в воздухе и куда это вернуть. Список, а не очередь: эффекты гаснут
        // не в том порядке, в каком запускались
        private readonly List<Active> _active = new();

        private readonly struct Active
        {
            public readonly ParticleSystem System;
            public readonly Stack<ParticleSystem> Home;

            public Active(ParticleSystem system, Stack<ParticleSystem> home)
            {
                System = system;
                Home = home;
            }
        }

        private void Awake()
        {
            if (_feedback == null)
            {
                Debug.LogError($"Пулу эффектов '{name}' не назначен ассет отдачи - " +
                               "искр на попадании не будет.", this);
                enabled = false;
                return;
            }

            Prewarm(_feedback.SparksPrefab, _freeSparks);
            Prewarm(_feedback.MarkPrefab, _freeMarks);
        }

        private void Prewarm(ParticleSystem prefab, Stack<ParticleSystem> pool)
        {
            if (prefab == null)
                return;

            for (int i = 0; i < _feedback.EffectPrewarm; i++)
                pool.Push(Create(prefab));
        }

        /// <summary>
        /// Показать попадание в точке <paramref name="point"/>. <paramref name="attachTo"/>
        /// может быть <c>null</c> - тогда след остаётся в мире (палуба, стена)
        /// </summary>
        public void Play(Vector3 point, Vector3 normal, Transform attachTo)
        {
            Spawn(_feedback.SparksPrefab, _freeSparks, point, normal, null);
            Spawn(_feedback.MarkPrefab, _freeMarks, point, normal, attachTo);
        }

        private void Spawn(
            ParticleSystem prefab, Stack<ParticleSystem> pool,
            Vector3 point, Vector3 normal, Transform attachTo)
        {
            if (prefab == null)
                return;

            ParticleSystem system = pool.Count > 0 ? pool.Pop() : Create(prefab);

            // Развернуть по нормали, а не поставить как есть: искры должны бить ОТ
            // поверхности, а плоский след - лежать на ней, а не торчать поперёк
            system.transform.SetParent(attachTo, false);
            system.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            system.gameObject.SetActive(true);
            system.Clear(true);
            system.Play(true);

            _active.Add(new Active(system, pool));
        }

        /// <summary>
        /// Возврат в пул по факту угасания, а не по таймеру: длительность частиц живёт
        /// в самом префабе, и второй её экземпляр здесь разъехался бы с ней при первой
        /// же правке эффекта
        /// </summary>
        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ParticleSystem system = _active[i].System;

                if (system == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                if (system.IsAlive(true))
                    continue;

                system.gameObject.SetActive(false);
                system.transform.SetParent(transform, false);
                _active[i].Home.Push(system);
                _active.RemoveAt(i);
            }
        }

        private ParticleSystem Create(ParticleSystem prefab)
        {
            ParticleSystem system = Instantiate(prefab, transform);
            system.gameObject.SetActive(false);

            return system;
        }
    }
}
