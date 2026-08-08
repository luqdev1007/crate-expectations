using NUnit.Framework;

namespace CrateExpectations.Combat.Tests
{
    public sealed class SweepScheduleTests
    {
        private const float Start = 0.35f;
        private const float End = 0.60f;
        private const int Samples = 6;

        private static SweepSchedule Schedule() => new(Start, End, Samples);

        /// <summary>Прогон всей атаки покадрово: возвращает, сколько сэмплов выпало всего</summary>
        private static int RunAttack(SweepSchedule schedule, int frames, float[] buffer)
        {
            float previous = -1f;
            int total = 0;

            for (int i = 1; i <= frames; i++)
            {
                float progress = i / (float)frames;
                total += schedule.Collect(previous, progress, buffer);
                previous = progress;
            }

            return total;
        }

        [Test]
        public void Edge_samples_sit_exactly_on_the_window_bounds()
        {
            SweepSchedule schedule = Schedule();

            Assert.That(schedule.SampleTime(0), Is.EqualTo(Start).Within(1e-5f));
            Assert.That(schedule.SampleTime(Samples - 1), Is.EqualTo(End).Within(1e-5f));
        }

        [Test]
        public void A_single_sample_lands_at_the_window_start()
        {
            var schedule = new SweepSchedule(Start, End, 1);

            Assert.That(schedule.SampleTime(0), Is.EqualTo(Start).Within(1e-5f));
        }

        [Test]
        public void Every_sample_fires_exactly_once_over_a_whole_attack()
        {
            var buffer = new float[Samples];

            Assert.That(RunAttack(Schedule(), 60, buffer), Is.EqualTo(Samples));
        }

        /// <summary>
        /// Ради этого расписание и заведено: на просадке частоты число проверок
        /// не должно падать вместе с ней
        /// </summary>
        [Test]
        public void The_number_of_checks_does_not_depend_on_the_frame_rate()
        {
            var buffer = new float[Samples];

            Assert.That(RunAttack(Schedule(), 4, buffer), Is.EqualTo(Samples), "мало кадров");
            Assert.That(RunAttack(Schedule(), 240, buffer), Is.EqualTo(Samples), "много кадров");
        }

        [Test]
        public void One_frame_swallowing_the_whole_window_collects_every_sample()
        {
            var buffer = new float[Samples];

            Assert.That(Schedule().Collect(-1f, 1f, buffer), Is.EqualTo(Samples));
        }

        [Test]
        public void Nothing_fires_before_the_window_opens()
        {
            var buffer = new float[Samples];

            Assert.That(Schedule().Collect(-1f, Start - 0.01f, buffer), Is.Zero);
        }

        [Test]
        public void Nothing_fires_after_the_window_closes()
        {
            var buffer = new float[Samples];

            Assert.That(Schedule().Collect(End, 1f, buffer), Is.Zero);
        }

        /// <summary>Соседние кадры не должны выстрелить одним и тем же сэмплом дважды</summary>
        [Test]
        public void A_sample_already_taken_is_not_taken_again()
        {
            var buffer = new float[Samples];
            SweepSchedule schedule = Schedule();

            int first = schedule.Collect(-1f, Start, buffer);
            int second = schedule.Collect(Start, Start, buffer);

            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.Zero);
        }

        [Test]
        public void Collected_times_stay_inside_the_window()
        {
            var buffer = new float[Samples];
            int count = Schedule().Collect(-1f, 1f, buffer);

            for (int i = 0; i < count; i++)
                Assert.That(buffer[i], Is.InRange(Start, End));
        }

        [Test]
        public void A_buffer_smaller_than_the_window_does_not_overflow()
        {
            var buffer = new float[2];

            Assert.That(Schedule().Collect(-1f, 1f, buffer), Is.EqualTo(2));
        }

        [Test]
        public void A_reversed_window_collapses_instead_of_running_backwards()
        {
            var schedule = new SweepSchedule(0.8f, 0.2f, Samples);

            Assert.That(schedule.Start, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(schedule.End, Is.EqualTo(0.8f).Within(1e-5f));
        }

        [Test]
        public void Fewer_than_one_sample_is_still_one_sample()
        {
            var schedule = new SweepSchedule(Start, End, 0);

            Assert.That(schedule.Samples, Is.EqualTo(1));
        }

        [Test]
        public void Time_running_backwards_collects_nothing()
        {
            var buffer = new float[Samples];

            Assert.That(Schedule().Collect(0.9f, 0.1f, buffer), Is.Zero);
        }

        [Test]
        public void A_null_buffer_is_survivable()
        {
            Assert.That(Schedule().Collect(-1f, 1f, null), Is.Zero);
        }
    }
}
