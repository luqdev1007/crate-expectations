using CrateExpectations.Core.Services;
using UnityEngine;
using UnityEngine.AI;

namespace CrateExpectations.Guards
{
    /// <summary>
    /// Всё, что состояние стражника вправе трогать. Состояния - обычные C#-классы и про
    /// <see cref="MonoBehaviour"/> не знают: сцену и данные им выдаёт этот контекст,
    /// а реализует его <see cref="GuardAI"/>. Тот же приём, что у <c>IInspectorContext</c>.
    /// <para>
    /// Имя не <c>GuardContext</c> сознательно: так уже называется снимок входных данных
    /// для <see cref="GuardBrain"/>, и это разные вещи. Тот - неизменяемая структура
    /// без единой ссылки на Unity, чтобы решение можно было проверить тестом;
    /// этот - живые ссылки на сцену, которые тестом не проверишь
    /// </para>
    /// </summary>
    public interface IGuardStateContext
    {
        /// <summary>Кто везёт тело по навмешу</summary>
        NavMeshAgent Agent { get; }

        /// <summary>Чем это отыгрывается. Нужен состояниям только ради оглядывания</summary>
        Animator Animator { get; }

        /// <summary>Темп обхода: скорость, длина пауз и частота оглядываний</summary>
        GuardMovementDefinition Movement { get; }

        /// <summary>Боевые числа: радиус удара, скорость погони, фазы замаха</summary>
        GuardCombatDefinition Combat { get; }

        /// <summary>
        /// За кем гнаться. Интерфейсом из <c>Core</c>, а не типом игрока: <c>Guards</c>
        /// не ссылается на <c>Player</c> и не должен - см. <see cref="IPlayerTarget"/>
        /// </summary>
        IPlayerTarget Target { get; }

        /// <summary>
        /// Чем задевать. <c>null</c> - стражник машет впустую: детекцию можно снять
        /// с префаба, и он продолжит замахиваться, просто никого не задевая
        /// </summary>
        GuardMeleeAttack Melee { get; }

        /// <summary>
        /// Удар начат и обязан доиграть. <b>Единственное свойство контекста, которое
        /// состояние ПИШЕТ, а не читает</b>, и это осознанно: срок жизни коммита знает
        /// только сама атака - снаружи его пришлось бы дублировать таймером, который
        /// разъехался бы с фазами при первой же правке ассета.
        /// <para>
        /// Читает его <see cref="GuardBrain"/> через <see cref="GuardContext"/>:
        /// пока флаг взведён, намерение остаётся <c>Attack</c>, даже если игрок
        /// вышел из радиуса
        /// </para>
        /// </summary>
        bool IsAttackCommitted { get; set; }

        /// <summary>
        /// Стражника сейчас нельзя сбить встречным ударом. Взводится только на активную
        /// фазу - ту, в которой его собственный клинок уже идёт.
        /// <para>
        /// Живёт здесь, а не в <see cref="GuardContext"/>, намеренно: это не то, чего
        /// стражник ХОЧЕТ, а то, что с ним можно сделать. Мозг про гипер-армор не знает
        /// вовсе - удар отсекает <see cref="GuardHitReaction"/>, ещё до того, как
        /// вздрагивание взведётся
        /// </para>
        /// </summary>
        bool IsHyperArmored { get; set; }

        /// <summary>
        /// Маршрут обхода или <c>null</c>. Пусто - стражник стоит на посту:
        /// маршрут это свойство места, и его отсутствие - штатный случай, а не поломка
        /// </summary>
        PatrolRoute Route { get; }
    }
}
