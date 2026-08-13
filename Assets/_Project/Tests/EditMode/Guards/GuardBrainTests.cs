using NUnit.Framework;

namespace CrateExpectations.Guards.Tests
{
    /// <summary>
    /// Единственное правило, которое <see cref="GuardBrain"/> считает сам, - откуда
    /// берётся намерение. Правил пока два, и тесты на них выглядят избыточно ровно
    /// настолько же, насколько сам класс: они здесь затем, чтобы расширение мозга
    /// (агрессия, тревога) не сломало базовый случай молча - "стражник без маршрута
    /// никуда не идёт" должно пережить все будущие ветки
    /// </summary>
    public sealed class GuardBrainTests
    {
        [Test]
        public void A_guard_with_a_route_walks_it()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(hasPatrolRoute: true)),
                        Is.EqualTo(GuardIntent.Patrol));
        }

        [Test]
        public void A_guard_without_a_route_stays_on_its_post()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(hasPatrolRoute: false)),
                        Is.EqualTo(GuardIntent.HoldPost));
        }
    }
}
