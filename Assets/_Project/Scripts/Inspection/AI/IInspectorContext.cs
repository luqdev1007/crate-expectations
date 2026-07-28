using System.Collections.Generic;
using System.Threading;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Stations;
using UnityEngine;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Всё, что состояние инспектора вправе трогать. Состояния - обычные C#-классы и про
    /// <see cref="MonoBehaviour"/> не знают: сцену, данные и вывод им выдаёт этот контекст,
    /// а реализует его <see cref="InspectorAI"/>. Ссылок друг на друга у состояний нет -
    /// переход заказывается через <see cref="GoTo"/> по имени фазы
    /// </summary>
    public interface IInspectorContext
    {
        /// <summary>Тайминги и шаги осмотра</summary>
        InspectionDefinition Definition { get; }

        /// <summary>Строгость: что инспектор проверяет и как оценивает найденное</summary>
        InspectorProfile Profile { get; }

        /// <summary>Тексты реплик - голос этого инспектора</summary>
        InspectorLinesDefinition Lines { get; }

        /// <summary>Зона досмотра: что в ней стоит и как её подсветить</summary>
        CargoPlacementZone Zone { get; }

        /// <summary>Перемещение тела</summary>
        InspectorMotor Motor { get; }

        /// <summary>Куда инспектор говорит</summary>
        IInspectorVoice Voice { get; }

        /// <summary>Указка, которой он показывает осматриваемую грань</summary>
        ExamineFocusMarker Focus { get; }

        /// <summary>Пост: место и поза, в которые инспектор возвращается между досмотрами</summary>
        Transform Post { get; }

        /// <summary>Точка, с которой он осматривает груз</summary>
        Transform ExaminePoint { get; }

        /// <summary>
        /// Точки обхода поста по порядку. Пусто - инспектор просто стоит на посту:
        /// патруль это украшение, и его отсутствие не должно ломать досмотр
        /// </summary>
        IReadOnlyList<Transform> PatrolPoints { get; }

        /// <summary>
        /// Токен текущего состояния. Отменяется при любом выходе из него - и по смене фазы,
        /// и вместе с объектом. Асинхронные шаги обязаны его слушать
        /// </summary>
        CancellationToken StateToken { get; }

        /// <summary>В зоне стоит груз, которого этот инспектор ещё не досматривал</summary>
        bool AwaitsInspection { get; }

        /// <summary>
        /// Ближайший груз, который прямо сейчас несут в руках мимо поста, или <c>null</c>.
        /// Повод обернуться, а не начать досмотр: поставленный на стол ящик сюда не попадает
        /// </summary>
        CargoBox ApproachingCargo { get; }

        /// <summary>Разбираемый случай: ящик и вынесенный по нему вердикт</summary>
        InspectionCase Case { get; }

        /// <summary>
        /// Оценить груз и завести случай. Вызывается ровно один раз за досмотр - в начале
        /// осмотра; шаги только отыгрывают уже готовый результат
        /// </summary>
        Verdict OpenCase(CargoBox cargo);

        /// <summary>Закрыть случай: огласить его в шину событий и больше к этому ящику не возвращаться</summary>
        void CloseCase();

        /// <summary>Перейти в другую фазу. Токен текущего состояния при этом отменяется</summary>
        void GoTo(InspectorPhase phase);
    }
}
