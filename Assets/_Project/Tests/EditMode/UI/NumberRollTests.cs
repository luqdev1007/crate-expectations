using NUnit.Framework;

namespace CrateExpectations.UI.Tests
{
    public sealed class NumberRollTests
    {
        private const float Seconds = 1f;
        private const float Frame = 1f / 60f;

        [Test]
        public void FreshRoll_ShowsNothingAndDoesNotRun()
        {
            var roll = new NumberRoll();

            Assert.That(roll.Value, Is.Zero);
            Assert.That(roll.IsRolling, Is.False);
        }

        [Test]
        public void JumpTo_SetsValueWithoutRolling()
        {
            var roll = new NumberRoll();

            roll.JumpTo(500);

            Assert.That(roll.Value, Is.EqualTo(500), "загрузка не начисление - крутить нечего");
            Assert.That(roll.IsRolling, Is.False);
        }

        [Test]
        public void Roll_LandsExactlyOnTarget()
        {
            var roll = new NumberRoll();
            roll.JumpTo(100);

            roll.RollTo(350, Seconds);
            Advance(roll, Seconds + Frame);

            Assert.That(roll.Value, Is.EqualTo(350), "счётчик обязан встать на точную сумму");
            Assert.That(roll.IsRolling, Is.False);
        }

        [Test]
        public void Roll_NeverOvershootsAndNeverGoesBack()
        {
            var roll = new NumberRoll();
            roll.JumpTo(0);
            roll.RollTo(1000, Seconds);

            int previous = roll.Value;

            for (float elapsed = 0f; elapsed < Seconds; elapsed += Frame)
            {
                roll.Advance(Frame);

                Assert.That(roll.Value, Is.InRange(previous, 1000),
                    "прокрутка должна идти только вперёд и не перелетать цель");
                previous = roll.Value;
            }
        }

        [Test]
        public void Roll_Decreases_WhenTargetIsLower()
        {
            var roll = new NumberRoll();
            roll.JumpTo(400);
            roll.RollTo(-100, Seconds);

            Advance(roll, Seconds * 0.5f);
            Assert.That(roll.Value, Is.LessThan(400), "убыток крутится вниз");

            Advance(roll, Seconds);
            Assert.That(roll.Value, Is.EqualTo(-100), "долг показывается как есть, без обрезки в ноль");
        }

        [Test]
        public void Roll_SlowsDownTowardsTheEnd()
        {
            var roll = new NumberRoll();
            roll.JumpTo(0);
            roll.RollTo(1000, Seconds);

            Advance(roll, Seconds * 0.5f);
            int atHalfway = roll.Value;

            Assert.That(atHalfway, Is.GreaterThan(500),
                "к середине пройдено больше половины пути - иначе замедления к концу не видно");
        }

        [Test]
        public void RollStartedMidRoll_ContinuesFromWhatIsOnScreen()
        {
            var roll = new NumberRoll();
            roll.JumpTo(0);
            roll.RollTo(1000, Seconds);
            Advance(roll, Seconds * 0.5f);

            int shown = roll.Value;
            roll.RollTo(200, Seconds);
            roll.Advance(Frame);

            Assert.That(roll.Value, Is.LessThan(shown),
                "второе начисление подхватывает видимое число, а не прыгает к нему");

            Advance(roll, Seconds);
            Assert.That(roll.Value, Is.EqualTo(200));
        }

        [Test]
        public void RollWithoutTime_JumpsInstantly()
        {
            var roll = new NumberRoll();
            roll.JumpTo(10);

            roll.RollTo(90, 0f);

            Assert.That(roll.Value, Is.EqualTo(90));
            Assert.That(roll.IsRolling, Is.False);
        }

        [Test]
        public void Progress_RunsFromZeroToOne()
        {
            var roll = new NumberRoll();
            roll.JumpTo(0);
            roll.RollTo(100, Seconds);

            Assert.That(roll.Progress, Is.EqualTo(0f).Within(0.001f));

            Advance(roll, Seconds * 0.5f);
            Assert.That(roll.Progress, Is.EqualTo(0.5f).Within(0.05f));

            Advance(roll, Seconds);
            Assert.That(roll.Progress, Is.EqualTo(1f).Within(0.001f),
                "подсветка гаснет вместе с прокруткой, поэтому доля обязана дойти до единицы");
        }

        private static void Advance(NumberRoll roll, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Frame)
                roll.Advance(Frame);
        }
    }
}
