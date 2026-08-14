using CrateExpectations.Combat;
using UnityEngine;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Чем стражник отвечает на попадание: вздрагивание от лёгкого удара, вздрагивание
    /// от тяжёлого и падение от последнего.
    /// <para>
    /// Отдельный ассет, а не поля в <see cref="GuardMovementDefinition"/>: тот описывает
    /// темп обхода - как стражник ходит, когда его никто не трогает. Реакция на удар
    /// к обходу отношения не имеет, и класть их в один ассет значило бы связать
    /// две настройки, которые правят в разные дни и по разным поводам.
    /// </para>
    /// <para>
    /// Смерть лежит здесь же, а не в третьем ассете, потому что это тоже ответ на удар,
    /// только последний: вздрагивание и падение крутят в одну посадку - «насколько
    /// сильно стражника мотает от сабли», - и разносить их по файлам значило бы
    /// подбирать одно вслепую относительно другого.
    /// </para>
    /// <para>
    /// Тир приходит из <see cref="AttackTier"/> - того самого поля, которое блок 2 завёл
    /// в <c>HitInfo</c>. Различать удары по <c>Damage</c> здесь было бы неверно:
    /// сколько сняли и как это тряхнуло - разные вопросы.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "GuardReactionDefinition",
        menuName = "CrateExpectations/Guards/Guard Reaction Definition")]
    public sealed class GuardReactionDefinition : ScriptableObject
    {
        [Header("Лёгкий удар")]
        [Tooltip("Значение HitIndex, которым граф Guard.controller выбирает клип вздрагивания. " +
                 "Число живёт здесь, а не константой в коде: набор реакций правят в графе, " +
                 "и код о том, сколько их и какая под каким номером, знать не должен")]
        [field: SerializeField] public int LightHitIndex { get; private set; }

        // Ссылка на клип, а не длина числом - ровно по той же причине, что у
        // LookAroundClip: подменили клип в графе, а число в ассете осталось прежним,
        // и стражник или зашагал бы посреди вздрагивания, или простоял бы лишнее
        [Tooltip("Клип лёгкого вздрагивания. Нужен не как анимация - её ставит граф, - " +
                 "а как источник длины: столько стражник стоит, оправляясь от удара. " +
                 "Пусто - реакция мгновенная, стражник продолжит обход без паузы")]
        [field: SerializeField] public AnimationClip LightHitClip { get; private set; }

        [Header("Тяжёлый удар")]
        [Tooltip("Значение HitIndex под тяжёлый удар")]
        [field: SerializeField] public int HeavyHitIndex { get; private set; } = 1;

        [Tooltip("Клип тяжёлого вздрагивания. Источник длины паузы, как и у лёгкого")]
        [field: SerializeField] public AnimationClip HeavyHitClip { get; private set; }

        [Header("Смерть")]
        [Tooltip("Слой, на который уходит регдол. Отдельный слой нужен не ради красоты: " +
                 "на нём труп выпадает из HitMask сабли, и добить лежащего физически " +
                 "нельзя - свип по нему просто не проходит")]
        [field: SerializeField] public string RagdollLayer { get; private set; } = "Ragdoll";

        [Tooltip("Множитель к импульсу добившего удара. Единица - труп улетает ровно " +
                 "с той силой, что записана в приёме; больше - смерть кинематографичнее, " +
                 "меньше - тело оседает на месте")]
        [field: SerializeField][Min(0f)] public float DeathImpulseScale { get; private set; } = 1f;

        [Tooltip("Масса выпавшего из руки оружия, кг. У клинка в руке массы нет вовсе - " +
                 "он ездит за костью, - и она появляется ровно в момент падения")]
        [field: SerializeField][Min(0.01f)] public float DroppedWeaponMass { get; private set; } = 2f;

        /// <summary>Каким номером граф выбирает реакцию на удар этого тира</summary>
        public int IndexFor(AttackTier tier) =>
            tier == AttackTier.Heavy ? HeavyHitIndex : LightHitIndex;

        /// <summary>
        /// Сколько стражник приходит в себя после удара этого тира, с.
        /// Ноль - клип не назначен, паузы не будет
        /// </summary>
        public float RecoveryFor(AttackTier tier)
        {
            AnimationClip clip = tier == AttackTier.Heavy ? HeavyHitClip : LightHitClip;

            return clip == null ? 0f : clip.length;
        }
    }
}
