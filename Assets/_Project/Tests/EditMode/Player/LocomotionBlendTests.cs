using CrateExpectations.Player.View;
using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Player.Tests
{
    /// <summary>
    /// Перевод скорости в точку блендтри ног. Числа подбирают в игре, а вот правила -
    /// нет: "вбок - это X", "быстрее максимума не бывает", "стоящий не перебирает
    /// ногами" держатся здесь, а не в наблюдении за походкой.
    /// </summary>
    public sealed class LocomotionBlendTests
    {
        private const float MaxSpeed = 4.5f;
        private const float Deadband = 0.05f;

        private LocomotionBlend _blend;

        [SetUp]
        public void SetUp() => _blend = new LocomotionBlend();

        /// <summary>Без сглаживания: правило проверяем отдельно от разгона к нему</summary>
        private Vector2 Instant(Vector3 localVelocity) =>
            _blend.Tick(localVelocity, MaxSpeed, 0f, Deadband, 0.02f);

        [Test]
        public void A_standing_player_does_not_move_his_legs()
        {
            Assert.That(Instant(Vector3.zero), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Full_speed_forward_is_the_far_edge_of_the_tree()
        {
            Vector2 blend = Instant(new Vector3(0f, 0f, MaxSpeed));

            Assert.That(blend.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(blend.y, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Strafing_goes_sideways_and_not_forward()
        {
            // Ровно то, ради чего дерево двумерное: боком игрок бежит боком,
            // а не отыгрывает бег вперёд, скользя вбок
            Vector2 right = Instant(new Vector3(MaxSpeed, 0f, 0f));

            Assert.That(right.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(right.y, Is.EqualTo(0f).Within(1e-4f));

            Vector2 left = Instant(new Vector3(-MaxSpeed, 0f, 0f));

            Assert.That(left.x, Is.EqualTo(-1f).Within(1e-4f));
        }

        [Test]
        public void Running_backwards_is_the_near_edge_of_the_tree()
        {
            Assert.That(Instant(new Vector3(0f, 0f, -MaxSpeed)).y, Is.EqualTo(-1f).Within(1e-4f));
        }

        [Test]
        public void Vertical_speed_is_not_a_step()
        {
            // Прыжок и падение - это скорость по Y, и ногам она не адресована:
            // иначе взлетевший на месте игрок отыграл бы пробежку
            Assert.That(Instant(new Vector3(0f, 12f, 0f)), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void A_diagonal_run_stays_inside_the_tree()
        {
            // Покоординатный кламп дал бы здесь длину 1.41 - точку, за которой
            // в дереве нет ни одного клипа
            Vector2 blend = Instant(new Vector3(MaxSpeed, 0f, MaxSpeed));

            Assert.That(blend.magnitude, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(blend.x, Is.EqualTo(blend.y).Within(1e-4f));
        }

        [Test]
        public void Speed_above_the_maximum_does_not_run_faster_than_the_legs_can()
        {
            // Игрока может унести рывком или лифтом. Дерево на это не рассчитано,
            // и край у него один
            Assert.That(Instant(new Vector3(0f, 0f, MaxSpeed * 4f)).magnitude,
                Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Physics_jitter_under_a_standing_player_is_not_a_step()
        {
            // Стоящего игрока толкают ящики и сползание с уклона. Без мёртвой зоны
            // ноги перебирали бы на месте всё время, пока он просто стоит
            Vector3 jitter = new Vector3(0.03f, 0f, -0.02f) * MaxSpeed * Deadband;

            Assert.That(Instant(jitter), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void The_legs_do_not_reach_full_speed_in_a_single_frame()
        {
            // Смысл сглаживания: физика ставит скорость мгновенно, ноги - нет
            Vector2 afterOneFrame =
                _blend.Tick(new Vector3(0f, 0f, MaxSpeed), MaxSpeed, 0.12f, Deadband, 0.02f);

            Assert.That(afterOneFrame.y, Is.GreaterThan(0f));
            Assert.That(afterOneFrame.y, Is.LessThan(1f));
        }

        [Test]
        public void Smoothing_gets_there_eventually()
        {
            for (int frame = 0; frame < 120; frame++)
                _blend.Tick(new Vector3(0f, 0f, MaxSpeed), MaxSpeed, 0.12f, Deadband, 0.02f);

            Assert.That(_blend.Current.y, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void A_teleported_player_does_not_finish_his_run_at_the_new_place()
        {
            for (int frame = 0; frame < 120; frame++)
                _blend.Tick(new Vector3(0f, 0f, MaxSpeed), MaxSpeed, 0.12f, Deadband, 0.02f);

            _blend.Reset();

            Assert.That(_blend.Current, Is.EqualTo(Vector2.zero));

            // Сброс обязан гасить и накопленную скорость сглаживания, иначе первый же
            // кадр после него продолжил бы разгон с прежней инерцией
            Vector2 next = _blend.Tick(Vector3.zero, MaxSpeed, 0.12f, Deadband, 0.02f);

            Assert.That(next, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void A_definition_with_no_speed_at_all_cannot_divide_the_world_by_zero()
        {
            Assert.That(_blend.Tick(new Vector3(0f, 0f, 3f), 0f, 0f, Deadband, 0.02f),
                Is.EqualTo(Vector2.zero));
        }
    }
}
