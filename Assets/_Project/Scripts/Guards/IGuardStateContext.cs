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

        /// <summary>Темп обхода: длина пауз и частота оглядываний</summary>
        GuardMovementDefinition Movement { get; }

        /// <summary>
        /// Маршрут обхода или <c>null</c>. Пусто - стражник стоит на посту:
        /// маршрут это свойство места, и его отсутствие - штатный случай, а не поломка
        /// </summary>
        PatrolRoute Route { get; }
    }
}
