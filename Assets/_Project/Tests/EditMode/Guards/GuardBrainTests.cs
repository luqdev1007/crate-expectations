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

        /// <summary>
        /// Главное правило фазы E и причина, по которой коммит вообще заведён:
        /// начатый удар доигрывает, даже когда игрок вышел из радиуса. Без него
        /// стражник отменял бы замах в тот же кадр, когда игрок шагнул назад,
        /// и не ударил бы НИКОГДА - сближение возвращало бы его в Attack,
        /// а шаг назад снова отменял
        /// </summary>
        [Test]
        public void A_started_attack_plays_out_even_when_the_player_backs_out_of_range()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(
                            hasPatrolRoute: true,
                            isAggro: true,
                            isInAttackRange: false,
                            isAttackCommitted: true)),
                        Is.EqualTo(GuardIntent.Attack));
        }

        [Test]
        public void An_angry_guard_out_of_reach_runs_the_player_down()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(
                            hasPatrolRoute: true, isAggro: true, isInAttackRange: false)),
                        Is.EqualTo(GuardIntent.Chase));
        }

        [Test]
        public void An_angry_guard_within_reach_swings()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(
                            hasPatrolRoute: true, isAggro: true, isInAttackRange: true)),
                        Is.EqualTo(GuardIntent.Attack));
        }

        /// <summary>
        /// Обратная сторона коммита: пока удар НЕ начат, вздрагивание его перебивает.
        /// Иначе стражник в радиусе замахивался бы сквозь любые попадания, и окно
        /// на ответ у игрока пропало бы вовсе
        /// </summary>
        [Test]
        public void A_hit_beats_an_attack_that_has_not_committed_yet()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(
                            hasPatrolRoute: true,
                            isStaggered: true,
                            isAggro: true,
                            isInAttackRange: true,
                            isAttackCommitted: false)),
                        Is.EqualTo(GuardIntent.Stagger));
        }

        /// <summary>
        /// И самое неочевидное из пары: вздрагивание перебивает даже ЗАКОММИЧЕННУЮ атаку.
        /// Противоречия с гипер-армором здесь нет - он живёт не в мозге:
        /// в активной фазе <c>GuardHitReaction</c> просто не взводит вздрагивание,
        /// и до этой ветки дело не доходит. А вот удар в замах или в доводку
        /// обязан сбивать, и сбивает
        /// </summary>
        [Test]
        public void A_hit_outside_the_hyper_armour_window_breaks_even_a_committed_attack()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(
                            hasPatrolRoute: true,
                            isStaggered: true,
                            isAggro: true,
                            isInAttackRange: true,
                            isAttackCommitted: true)),
                        Is.EqualTo(GuardIntent.Stagger));
        }

        /// <summary>
        /// Труп не воскресает. Ветка недостижима, пока <c>GuardDeath</c> гасит
        /// <c>GuardAI</c>, но здоровье смерть переживает - и если объект зажгут
        /// обратно, погоня начаться не должна
        /// </summary>
        [Test]
        public void A_dead_guard_chases_nobody_however_angry_he_was()
        {
            var brain = new GuardBrain();

            Assert.That(brain.Decide(new GuardContext(
                            hasPatrolRoute: true,
                            isDead: true,
                            isAggro: true,
                            isInAttackRange: true,
                            isAttackCommitted: true)),
                        Is.EqualTo(GuardIntent.HoldPost));
        }
    }
}
