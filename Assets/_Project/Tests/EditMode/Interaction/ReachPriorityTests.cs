using NUnit.Framework;

namespace CrateExpectations.Interaction.Tests
{
    /// <summary>
    /// Выбор занятия для одной кнопки руки. Ровно та таблица случаев, которую в play mode
    /// пришлось бы перебирать ногами: встать перед станцией с ящиком, без ящика, с ящиком
    /// В ЗОНЕ станции, с саблей в руке - и каждый раз смотреть, совпала ли подсказка
    /// с тем, что случилось по нажатию.
    /// </summary>
    public sealed class ReachPriorityTests
    {
        private const float Near = 1f;
        private const float Far = 2.5f;

        // Так Interactor сообщает «цели этого рода под прицелом нет»
        private const float Nothing = float.MaxValue;

        [Test]
        public void An_empty_crosshair_offers_nothing()
        {
            Assert.That(
                ReachPriority.Resolve(false, false, Nothing, false, Nothing),
                Is.EqualTo(ReachAction.None));
        }

        [Test]
        public void A_lone_station_is_pressed()
        {
            Assert.That(
                ReachPriority.Resolve(false, false, Nothing, true, Near),
                Is.EqualTo(ReachAction.Interact));
        }

        [Test]
        public void A_lone_crate_is_picked_up()
        {
            Assert.That(
                ReachPriority.Resolve(false, true, Near, false, Nothing),
                Is.EqualTo(ReachAction.Grab));
        }

        [Test]
        public void The_nearer_of_the_two_wins()
        {
            // Ящик стоит в зоне станции - главный случай всего правила: смотришь на ящик,
            // берёшь ящик; смотришь на станцию поверх него, жмёшь станцию
            Assert.That(
                ReachPriority.Resolve(false, true, Near, true, Far),
                Is.EqualTo(ReachAction.Grab), "ящик ближе, а взяли не его");

            Assert.That(
                ReachPriority.Resolve(false, true, Far, true, Near),
                Is.EqualTo(ReachAction.Interact), "станция ближе, а нажали не её");
        }

        [Test]
        public void A_tie_goes_to_the_crate()
        {
            Assert.That(
                ReachPriority.Resolve(false, true, Near, true, Near),
                Is.EqualTo(ReachAction.Grab));
        }

        [Test]
        public void A_crate_in_hand_beats_everything_under_the_crosshair()
        {
            // Иначе станция отобрала бы у игрока единственный способ поставить ношу
            Assert.That(
                ReachPriority.Resolve(true, false, Nothing, true, Near),
                Is.EqualTo(ReachAction.Drop), "с грузом в руках станция перехватила кнопку");

            Assert.That(
                ReachPriority.Resolve(true, true, Near, true, Near),
                Is.EqualTo(ReachAction.Drop));
        }

        [Test]
        public void Hands_busy_with_something_else_leave_the_crate_alone()
        {
            // canGrab здесь уже учитывает занятость рук: с саблей или с листком подсказка
            // «Взять» не появится вовсе - обещать то, чего кнопка не сделает, нельзя
            Assert.That(
                ReachPriority.Resolve(false, false, Near, false, Nothing),
                Is.EqualTo(ReachAction.None));

            // ...но станция при этом остаётся доступной: ткнуть её можно и с саблей
            Assert.That(
                ReachPriority.Resolve(false, false, Near, true, Far),
                Is.EqualTo(ReachAction.Interact));
        }
    }
}
