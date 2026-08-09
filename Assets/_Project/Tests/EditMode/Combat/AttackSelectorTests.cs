using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Combat.Tests
{
    /// <summary>
    /// Правила выбора удара: как нажатые клавиши превращаются в направление и как
    /// направление превращается в приём. Ровно то, ради чего <see cref="AttackSelector"/>
    /// сделан обычным C#-классом: проверять это тыканьем клавиш в play mode пришлось бы
    /// по восемь раз на каждую правку раскладки
    /// </summary>
    public sealed class AttackSelectorTests
    {
        /// <summary>
        /// Диагональ так и приходит из композита Input System - нормализованной.
        /// Порог нажатия обязан быть ниже этого числа, иначе диагональ читалась бы
        /// как "ничего не нажато"
        /// </summary>
        private const float Diagonal = 0.71f;

        private static Vector2 Keys(float x, float y) => new(x, y);

        private static AttackSelector Selector(IAttackTable table) => new(table);

        // --- направление ---

        [Test]
        public void Nothing_pressed_on_the_ground_is_the_standing_attack()
        {
            Assert.That(AttackSelector.ResolveDirection(Vector2.zero, false),
                Is.EqualTo(AttackDirection.Neutral));
        }

        [Test]
        public void Each_movement_key_has_its_own_direction()
        {
            Assert.That(AttackSelector.ResolveDirection(Keys(0f, 1f), false), Is.EqualTo(AttackDirection.Forward));
            Assert.That(AttackSelector.ResolveDirection(Keys(0f, -1f), false), Is.EqualTo(AttackDirection.Backward));
            Assert.That(AttackSelector.ResolveDirection(Keys(1f, 0f), false), Is.EqualTo(AttackDirection.Right));
            Assert.That(AttackSelector.ResolveDirection(Keys(-1f, 0f), false), Is.EqualTo(AttackDirection.Left));
        }

        [Test]
        public void A_diagonal_resolves_to_its_sideways_half()
        {
            Assert.That(AttackSelector.ResolveDirection(Keys(-Diagonal, Diagonal), false),
                Is.EqualTo(AttackDirection.Left), "W+A ушло не в боковой удар");

            Assert.That(AttackSelector.ResolveDirection(Keys(Diagonal, -Diagonal), false),
                Is.EqualTo(AttackDirection.Right), "S+D ушло не в боковой удар");
        }

        [Test]
        public void Being_airborne_outranks_every_key_that_is_held()
        {
            Assert.That(AttackSelector.ResolveDirection(Vector2.zero, true), Is.EqualTo(AttackDirection.Air));
            Assert.That(AttackSelector.ResolveDirection(Keys(-1f, 1f), true), Is.EqualTo(AttackDirection.Air));
        }

        [Test]
        public void A_stick_barely_off_centre_still_counts_as_standing_still()
        {
            // Порог существует ради дребезга оси, а не ради красоты: без него
            // удар стоя был бы недостижим на геймпаде
            Assert.That(AttackSelector.ResolveDirection(Keys(0.1f, -0.1f), false),
                Is.EqualTo(AttackDirection.Neutral));
        }

        // --- выбор приёма ---

        [Test]
        public void The_attack_comes_from_the_row_of_the_direction_that_was_asked_for()
        {
            AttackSelector selector = Selector(new FakeAttackTable()
                .With(AttackDirection.Neutral, "stand")
                .With(AttackDirection.Left, "left"));

            Assert.That(selector.Select(Keys(-1f, 0f), false).name, Is.EqualTo("left"));
            Assert.That(selector.Select(Vector2.zero, false).name, Is.EqualTo("stand"));
        }

        [Test]
        public void Repeating_a_direction_alternates_between_its_variations()
        {
            AttackSelector selector = Selector(new FakeAttackTable()
                .With(AttackDirection.Left, "left1", "left2"));

            Assert.That(selector.Select(AttackDirection.Left).name, Is.EqualTo("left1"));
            Assert.That(selector.Select(AttackDirection.Left).name, Is.EqualTo("left2"));
            Assert.That(selector.Select(AttackDirection.Left).name, Is.EqualTo("left1"), "чередование не закольцевалось");
        }

        [Test]
        public void Hitting_the_other_way_does_not_move_this_direction_along_its_variations()
        {
            AttackSelector selector = Selector(new FakeAttackTable()
                .With(AttackDirection.Left, "left1", "left2")
                .With(AttackDirection.Right, "right1", "right2"));

            selector.Select(AttackDirection.Left);
            selector.Select(AttackDirection.Right);
            selector.Select(AttackDirection.Right);

            // Влево бьют второй раз в жизни - значит вторая вариация, сколько бы
            // ударов вправо между ними ни случилось
            Assert.That(selector.Select(AttackDirection.Left).name, Is.EqualTo("left2"));
        }

        [Test]
        public void A_direction_nobody_filled_in_falls_back_to_the_standing_attack()
        {
            AttackSelector selector = Selector(new FakeAttackTable().With(AttackDirection.Neutral, "stand"));

            Assert.That(selector.Select(AttackDirection.Air).name, Is.EqualTo("stand"));
        }

        [Test]
        public void An_empty_layout_gives_nothing_rather_than_throwing()
        {
            AttackSelector selector = Selector(new FakeAttackTable());

            Assert.That(selector.Select(AttackDirection.Neutral), Is.Null);
            Assert.That(selector.Select(AttackDirection.Right), Is.Null);
        }

        [Test]
        public void A_hole_in_a_row_is_stepped_over_instead_of_being_hit_with_nothing()
        {
            AttackSelector selector = Selector(new FakeAttackTable()
                .With(AttackDirection.Right, "right1", null));

            Assert.That(selector.Select(AttackDirection.Right).name, Is.EqualTo("right1"));
            Assert.That(selector.Select(AttackDirection.Right).name, Is.EqualTo("right1"),
                "пустая ячейка отдала удар через раз");
        }

        [Test]
        public void Resetting_starts_the_series_from_its_first_variation_again()
        {
            AttackSelector selector = Selector(new FakeAttackTable()
                .With(AttackDirection.Left, "left1", "left2"));

            selector.Select(AttackDirection.Left);
            selector.ResetVariations();

            Assert.That(selector.Select(AttackDirection.Left).name, Is.EqualTo("left1"));
        }
    }
}
