using System.Text.RegularExpressions;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Core.Events;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrateExpectations.Persistence.Tests
{
    public sealed class CargoRestoreTests
    {
        private FakeCargoCatalog _catalog;
        private CargoRegistryDefinition _registry;
        private EventBus _bus;
        private CargoSceneKeeper _keeper;

        [SetUp]
        public void SetUp()
        {
            _catalog = new FakeCargoCatalog();
            _registry = ScriptableObject.CreateInstance<CargoRegistryDefinition>();
            _bus = new EventBus();
            _keeper = new CargoSceneKeeper(_catalog, _registry, _bus);
        }

        [TearDown]
        public void TearDown()
        {
            _keeper.Dispose();
            Object.DestroyImmediate(_registry);
        }

        [Test]
        public void An_empty_dock_is_captured_as_an_empty_dock()
        {
            Assert.That(_keeper.Capture().Crates, Is.Empty);
        }

        [Test]
        public void Restored_crates_are_asked_of_the_catalog_by_their_content_keys()
        {
            var snapshot = new CargoSceneSnapshot(new[]
            {
                Crate("Cargo/Type/Rum", new Vector3(1f, 0f, 0f)),
                Crate("Cargo/Type/Spices", new Vector3(2f, 0f, 0f)),
            });

            LogAssert.Expect(LogType.Warning, new Regex("Cargo/Type/Rum"));
            LogAssert.Expect(LogType.Warning, new Regex("Cargo/Type/Spices"));

            Run(_keeper.RestoreAsync(snapshot));

            Assert.That(_catalog.Requested, Is.EqualTo(new[] { "Cargo/Type/Rum", "Cargo/Type/Spices" }));
            Assert.That(_catalog.Positions[0], Is.EqualTo(new Vector3(1f, 0f, 0f)));
            Assert.That(_catalog.Positions[1], Is.EqualTo(new Vector3(2f, 0f, 0f)));
        }

        [Test]
        public void A_save_with_no_cargo_restores_quietly()
        {
            Run(_keeper.RestoreAsync(default));

            Assert.That(_catalog.Requested, Is.Empty);
        }

        private static CargoCrateSnapshot Crate(string typeKey, Vector3 position) => new()
        {
            TypeKey = typeKey,
            DeclaredTypeKey = typeKey,
            Position = position,
            Rotation = Quaternion.identity,
        };

        private static void Run(UniTask task) => task.GetAwaiter().GetResult();
    }
}
