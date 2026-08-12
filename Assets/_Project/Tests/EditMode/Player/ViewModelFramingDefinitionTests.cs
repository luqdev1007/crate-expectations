using System;
using CrateExpectations.Core.Hands;
using CrateExpectations.Player.View;
using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Player.Tests
{
    /// <summary>
    /// Выбор стойки вьюмодели по занятости рук. Числа подбирает автор, а вот САМ ВЫБОР -
    /// это правило: за ним стоит обещание, что боевая компоновка не поедет от того, что
    /// в кадрирование добавили состояния.
    /// <para>
    /// Проверять это в play mode пришлось бы, поднимая ящик и глядя на руки, - то есть
    /// на глаз и без возможности заметить регресс. Здесь оно проверяется таблицей случаев
    /// </para>
    /// </summary>
    public sealed class ViewModelFramingDefinitionTests
    {
        private ViewModelFramingDefinition _framing;

        [SetUp]
        public void SetUp() =>
            _framing = ScriptableObject.CreateInstance<ViewModelFramingDefinition>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_framing);

        [Test]
        public void Combat_keeps_the_base_framing_untouched()
        {
            // Главное обещание всей затеи: дуги ударов, посадка клинка и блок подобраны
            // относительно базового корня, и добавка состояний обязана обходить бой стороной
            Assert.That(_framing.StanceOffsetFor(HandsOccupancy.Combat), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Free_and_reading_keep_the_base_framing_untouched()
        {
            Assert.That(_framing.StanceOffsetFor(HandsOccupancy.Free), Is.EqualTo(Vector3.zero));
            Assert.That(_framing.StanceOffsetFor(HandsOccupancy.Reading), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Carrying_lifts_the_stance()
        {
            // Знак, а не величина: 0.30 автор подберёт сам, но клипы переноски приехали
            // из анимации от третьего лица и держат кисти НИЖЕ кромки кадра - подъём
            // обязан быть подъёмом
            Vector3 carrying = _framing.StanceOffsetFor(HandsOccupancy.Carrying);

            Assert.That(carrying.y, Is.GreaterThan(0f), "переноска не поднимает стойку");
        }

        [Test]
        public void Carrying_lifts_the_stance_straight_up()
        {
            // Вбок и вперёд не двигаем: подъём должен менять только высоту, иначе
            // подобранная ширина хвата и глубина выноса рук поедут вместе с ним
            Vector3 carrying = _framing.StanceOffsetFor(HandsOccupancy.Carrying);

            Assert.That(carrying.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(carrying.z, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Every_occupancy_has_an_answer()
        {
            // Не «все значения перечислены в switch» - это компилятор не проверяет, -
            // а «ни одно не падает и не возвращает мусор». Новое состояние рук должно
            // означать «стойка не подобрана», а не уронить кадр
            foreach (HandsOccupancy occupancy in Enum.GetValues(typeof(HandsOccupancy)))
                Assert.DoesNotThrow(() => _framing.StanceOffsetFor(occupancy));
        }

        [Test]
        public void An_unknown_occupancy_falls_back_to_the_base_framing()
        {
            Assert.That(_framing.StanceOffsetFor((HandsOccupancy)999), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void The_stance_transition_takes_time()
        {
            // Ноль дал бы скачок кадра ровно в тот момент, когда груз прилипает к рукам:
            // занятость меняется мгновенно, а композиция обязана доехать
            Assert.That(_framing.StanceBlend, Is.GreaterThan(0f));
        }
    }
}
