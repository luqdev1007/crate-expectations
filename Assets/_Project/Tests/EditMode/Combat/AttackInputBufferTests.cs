using NUnit.Framework;

namespace CrateExpectations.Combat.Tests
{
    /// <summary>
    /// Память на одно нажатие. Проверяется без сцены, потому что время в
    /// <see cref="AttackInputBuffer"/> приходит аргументом: истечение буфера - это
    /// арифметика, а не кадры
    /// </summary>
    public sealed class AttackInputBufferTests
    {
        private const float Duration = 0.15f;

        private static AttackInputBuffer Buffer() => new(Duration);

        [Test]
        public void A_fresh_buffer_has_nothing_to_answer_for()
        {
            Assert.That(Buffer().HasPending, Is.False);
        }

        [Test]
        public void A_press_is_remembered_until_the_buffer_runs_out()
        {
            AttackInputBuffer buffer = Buffer();
            buffer.Press();

            buffer.Tick(Duration - 0.01f);
            Assert.That(buffer.HasPending, Is.True, "нажатие забылось раньше срока");

            buffer.Tick(0.02f);
            Assert.That(buffer.HasPending, Is.False, "нажатие пережило свой буфер");
        }

        [Test]
        public void A_second_press_prolongs_the_memory_instead_of_queueing_a_second_attack()
        {
            AttackInputBuffer buffer = Buffer();

            buffer.Press();
            buffer.Tick(Duration * 0.9f);
            buffer.Press();
            buffer.Tick(Duration * 0.9f);

            // Зажатая кнопка не должна копить серию: помним всегда только последнее нажатие
            Assert.That(buffer.HasPending, Is.True);

            buffer.Consume();
            Assert.That(buffer.HasPending, Is.False, "после отработки осталось второе нажатие");
        }

        [Test]
        public void Consuming_and_clearing_both_empty_the_buffer()
        {
            AttackInputBuffer consumed = Buffer();
            consumed.Press();
            consumed.Consume();
            Assert.That(consumed.HasPending, Is.False);

            AttackInputBuffer cleared = Buffer();
            cleared.Press();
            cleared.Clear();
            Assert.That(cleared.HasPending, Is.False);
        }

        [Test]
        public void A_zero_length_buffer_swallows_the_press_it_was_given()
        {
            // Ноль в ассете - это "буфера нет", и он обязан работать как отключённый,
            // а не как бесконечный
            var buffer = new AttackInputBuffer(0f);
            buffer.Press();

            Assert.That(buffer.HasPending, Is.False);
        }

        [Test]
        public void Ticking_an_empty_buffer_never_drives_it_below_empty()
        {
            AttackInputBuffer buffer = Buffer();

            buffer.Tick(10f);
            buffer.Press();

            // Если бы Tick уводил остаток в минус, накопленный долг съел бы это нажатие
            Assert.That(buffer.HasPending, Is.True);
        }
    }
}
