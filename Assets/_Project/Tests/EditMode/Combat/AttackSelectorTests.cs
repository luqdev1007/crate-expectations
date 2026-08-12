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

        // --- заряд ---

        private const float Threshold = 0.25f;

        private static FakeAttackTable ChargedForward() => new FakeAttackTable()
            .WithCharged(AttackDirection.Forward, ("tap", 0f), ("charged", Threshold));

        [Test]
        public void A_short_press_gets_the_uncharged_attack()
        {
            AttackSelector selector = Selector(ChargedForward());

            Assert.That(selector.Select(AttackDirection.Forward, 0f).name, Is.EqualTo("tap"));
            Assert.That(selector.Select(AttackDirection.Forward, Threshold - 0.01f).name, Is.EqualTo("tap"),
                "чуть-чуть не додержали, а приём уже заряженный");
        }

        [Test]
        public void Holding_to_the_threshold_gets_the_charged_attack()
        {
            AttackSelector selector = Selector(ChargedForward());

            Assert.That(selector.Select(AttackDirection.Forward, Threshold).name, Is.EqualTo("charged"),
                "ровно на пороге заряженный приём обязан быть доступен");
            Assert.That(selector.Select(AttackDirection.Forward, Threshold * 4f).name, Is.EqualTo("charged"),
                "передержали - должен остаться тот же заряженный, а не откатиться");
        }

        [Test]
        public void The_charged_attack_never_shows_up_in_the_uncharged_rotation()
        {
            // Ступень заряда - не вариация. Три коротких нажатия подряд обязаны дать
            // три коротких удара, а не подсунуть заряженный вторым по кругу
            AttackSelector selector = Selector(new FakeAttackTable()
                .WithCharged(AttackDirection.Forward, ("tap1", 0f), ("charged", Threshold), ("tap2", 0f)));

            Assert.That(selector.Select(AttackDirection.Forward, 0f).name, Is.EqualTo("tap1"));
            Assert.That(selector.Select(AttackDirection.Forward, 0f).name, Is.EqualTo("tap2"));
            Assert.That(selector.Select(AttackDirection.Forward, 0f).name, Is.EqualTo("tap1"),
                "чередование внутри ступени не закольцевалось");
        }

        [Test]
        public void A_row_that_is_charged_all_the_way_through_falls_back_instead_of_firing_early()
        {
            // Раскладка, где у направления есть ТОЛЬКО заряженный приём, - валидная.
            // Короткое нажатие в ней обязано уйти в удар стоя, а не выдать заряженный даром
            AttackSelector selector = Selector(new FakeAttackTable()
                .With(AttackDirection.Neutral, "stand")
                .WithCharged(AttackDirection.Forward, ("charged", Threshold)));

            Assert.That(selector.Select(AttackDirection.Forward, 0f).name, Is.EqualTo("stand"));
            Assert.That(selector.Select(AttackDirection.Forward, Threshold).name, Is.EqualTo("charged"));
        }

        [Test]
        public void The_charge_time_of_a_direction_is_the_longest_hold_in_its_row()
        {
            AttackSelector selector = Selector(ChargedForward()
                .With(AttackDirection.Left, "left1", "left2"));

            Assert.That(selector.ChargeTime(AttackDirection.Forward), Is.EqualTo(Threshold).Within(1e-5f));

            // Ноль здесь - это команда "бей сразу", и она обязана приходить и для
            // незаряженного направления, и для нерасписанного
            Assert.That(selector.ChargeTime(AttackDirection.Left), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(selector.ChargeTime(AttackDirection.Air), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void A_direction_without_charge_ignores_however_long_the_button_was_held()
        {
            // Зажатая кнопка не должна менять удар там, где заряжаться нечему:
            // иначе долгое нажатие молча ломало бы чередование вариаций
            AttackSelector selector = Selector(new FakeAttackTable()
                .With(AttackDirection.Left, "left1", "left2"));

            Assert.That(selector.Select(AttackDirection.Left, 5f).name, Is.EqualTo("left1"));
            Assert.That(selector.Select(AttackDirection.Left, 5f).name, Is.EqualTo("left2"));
        }

        // --- боевая раскладка сабли ---
        //
        // Всё, что выше, проверяет ПРАВИЛА выбора и потому идёт на FakeAttackTable:
        // от перестановки двух приёмов местами те тесты падать не должны.
        // Всё, что ниже, наоборот, проверяет СОДЕРЖИМОЕ отгружаемого ассета - то,
        // что игрок реально получит на кнопку. Такой тест обязан падать при правке
        // раскладки: ступени заряда у Left/Right/Neutral - это игровое обещание,
        // а не деталь реализации. Держим их рядом, но не путаем.

        private const string SabreAttackSetPath =
            "Assets/_Project/Data/Combat/SabreAttackSet.asset";

        private static AttackSelector SabreSelector()
        {
            var set = UnityEditor.AssetDatabase.LoadAssetAtPath<AttackSet>(SabreAttackSetPath);

            Assert.That(set, Is.Not.Null, $"не нашлась боевая раскладка '{SabreAttackSetPath}'");

            return new AttackSelector(set);
        }

        /// <summary>Порог заряда, общий для всех заряженных приёмов сабли</summary>
        private const float SabreHold = 0.25f;

        /// <summary>Чуть меньше порога: самый долгий тап, который обязан остаться тапом</summary>
        private const float SabreTap = SabreHold - 0.01f;

        [Test]
        public void Holding_a_sideways_or_standing_attack_gets_the_sabres_heavy_tier()
        {
            AttackSelector selector = SabreSelector();

            Assert.That(selector.Select(AttackDirection.Left, SabreHold).name,
                Is.EqualTo("Sabre_Left_Heavy"));

            Assert.That(selector.Select(AttackDirection.Right, SabreHold).name,
                Is.EqualTo("Sabre_Right_Heavy"));

            Assert.That(selector.Select(AttackDirection.Neutral, SabreHold).name,
                Is.EqualTo("Sabre_Neutral_Heavy"));
        }

        [Test]
        public void Tapping_the_same_directions_stays_on_the_light_tier()
        {
            AttackSelector selector = SabreSelector();

            Assert.That(selector.Select(AttackDirection.Left, SabreTap).name,
                Is.EqualTo("Attack_SlashLD"));

            Assert.That(selector.Select(AttackDirection.Right, SabreTap).name,
                Is.EqualTo("Attack_SlashRU"));

            // Neutral - единственное направление с двумя лёгкими вариациями,
            // поэтому проверяем обе: заряженный приём не должен влезть между ними
            Assert.That(selector.Select(AttackDirection.Neutral, SabreTap).name,
                Is.EqualTo("Attack_SlashLD"));

            Assert.That(selector.Select(AttackDirection.Neutral, SabreTap).name,
                Is.EqualTo("Attack_SlashRD"));
        }

        [Test]
        public void The_standing_attack_alternates_between_exactly_two_light_variations()
        {
            AttackSelector selector = SabreSelector();

            // Третьей ячейкой в ряду стоит заряженный приём, и чередование обязано
            // перешагнуть её, а не выдать заряженный удар за короткое нажатие
            Assert.That(selector.Select(AttackDirection.Neutral, 0f).name, Is.EqualTo("Attack_SlashLD"));
            Assert.That(selector.Select(AttackDirection.Neutral, 0f).name, Is.EqualTo("Attack_SlashRD"));
            Assert.That(selector.Select(AttackDirection.Neutral, 0f).name, Is.EqualTo("Attack_SlashLD"),
                "чередование удара стоя не закольцевалось на двух вариациях");
            Assert.That(selector.Select(AttackDirection.Neutral, 0f).name, Is.EqualTo("Attack_SlashRD"));
        }

        [Test]
        public void Stepping_back_has_no_charged_tier_and_ignores_the_hold_entirely()
        {
            AttackSelector selector = SabreSelector();

            Assert.That(selector.ChargeTime(AttackDirection.Backward), Is.EqualTo(0f).Within(1e-5f),
                "у отступления завёлся заряд - нажатие перестанет уходить в удар сразу");

            Assert.That(selector.Select(AttackDirection.Backward, 0f).name, Is.EqualTo("Attack_ThrustF"));
            Assert.That(selector.Select(AttackDirection.Backward, SabreHold).name, Is.EqualTo("Attack_ThrustF"));
            Assert.That(selector.Select(AttackDirection.Backward, 5f).name, Is.EqualTo("Attack_ThrustF"),
                "передержали - и отступление отдало не тот приём");
        }

        [Test]
        public void One_attack_standing_in_two_rows_keeps_a_counter_for_each_of_them()
        {
            // Attack_SlashLD стоит и в Neutral, и в Left - намеренно, одним ассетом.
            // Счётчик вариаций при этом свой у каждого направления, и удары вбок
            // не должны сдвигать очередь удара стоя
            AttackSelector selector = SabreSelector();

            Assert.That(selector.Select(AttackDirection.Neutral, 0f).name, Is.EqualTo("Attack_SlashLD"));

            selector.Select(AttackDirection.Left, 0f);
            selector.Select(AttackDirection.Left, SabreHold);

            Assert.That(selector.Select(AttackDirection.Neutral, 0f).name, Is.EqualTo("Attack_SlashRD"),
                "удары влево сбили очередь удара стоя - счётчик вариаций оказался общим");
        }
    }
}
