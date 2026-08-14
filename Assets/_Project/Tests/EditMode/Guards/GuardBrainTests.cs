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

        /// <summary>
        /// Главное правило удара: он ПЕРЕБИВАЕТ обход, а не ждёт своей очереди.
        /// Стражник с маршрутом - это тот случай, в котором приоритет и проверяется:
        /// без него оба намерения совпали бы и тест ничего не доказывал
        /// </summary>
        [Test]
        public void A_hit_interrupts_the_patrol_instead_of_waiting_its_turn()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(hasPatrolRoute: true, isStaggered: true)),
                        Is.EqualTo(GuardIntent.Stagger));
        }

        [Test]
        public void A_hit_stops_a_guard_who_was_standing_his_post_too()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(hasPatrolRoute: false, isStaggered: true)),
                        Is.EqualTo(GuardIntent.Stagger));
        }

        /// <summary>
        /// Обратная сторона того же правила: как только вздрагивание кончилось,
        /// стражник обязан вернуться к обходу САМ. Оглушение, из которого нет выхода, -
        /// это молчаливо зависший NPC, а не поломка, которую видно в консоли
        /// </summary>
        [Test]
        public void When_the_flinch_is_over_the_guard_goes_back_to_walking()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(hasPatrolRoute: true, isStaggered: false)),
                        Is.EqualTo(GuardIntent.Patrol));
        }
    }
}
