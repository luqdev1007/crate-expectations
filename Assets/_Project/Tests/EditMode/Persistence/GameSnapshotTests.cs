using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Persistence.Tests
{
    public sealed class GameSnapshotTests
    {
        private static GameSnapshot RoundTrip(GameSnapshot snapshot) =>
            JsonUtility.FromJson<GameSnapshot>(JsonUtility.ToJson(snapshot));

        [Test]
        public void A_saved_snapshot_survives_the_trip_through_json()
        {
            var saved = new GameSnapshot
            {
                Version = GameSnapshot.CurrentVersion,
                Economy = new EconomySnapshot { Balance = -320 },
                Contract = new ContractSnapshot
                {
                    ContractId = "Contract_RumRun",
                    Delivered = 2,
                    Seized = 1,
                },
                Cargo = new CargoSceneSnapshot(new[]
                {
                    new CargoCrateSnapshot
                    {
                        TypeKey = "Cargo/Type/Rum",
                        DeclaredTypeKey = "Cargo/Type/Spices",
                        PaintId = "Paint_Navy",
                        StampId = "Stamp_PortSeal",
                        Position = new Vector3(1f, 2f, 3f),
                        Rotation = Quaternion.Euler(0f, 90f, 0f),
                    },
                }),
            };

            GameSnapshot loaded = RoundTrip(saved);

            Assert.That(loaded.Version, Is.EqualTo(GameSnapshot.CurrentVersion));
            Assert.That(loaded.IsReadable, Is.True);
            Assert.That(loaded.Economy.Balance, Is.EqualTo(-320), "долг - тоже состояние");
            Assert.That(loaded.Contract.ContractId, Is.EqualTo("Contract_RumRun"));
            Assert.That(loaded.Contract.Delivered, Is.EqualTo(2));
            Assert.That(loaded.Contract.Seized, Is.EqualTo(1));
            Assert.That(loaded.Cargo.Crates.Length, Is.EqualTo(1));
        }

        [Test]
        public void A_disguised_crate_keeps_both_its_truth_and_its_disguise()
        {
            var disguised = new CargoCrateSnapshot
            {
                TypeKey = "Cargo/Type/Rum",
                DeclaredTypeKey = "Cargo/Type/Spices",
                PaintId = "Paint_Navy",
                StampId = "Stamp_PortSeal",
                Position = new Vector3(4f, 0.5f, -2f),
                Rotation = Quaternion.Euler(0f, 45f, 0f),
            };

            var snapshot = new GameSnapshot
            {
                Version = GameSnapshot.CurrentVersion,
                Cargo = new CargoSceneSnapshot(new[] { disguised }),
            };

            CargoCrateSnapshot loaded = RoundTrip(snapshot).Cargo.Crates[0];

            Assert.That(loaded.TypeKey, Is.EqualTo("Cargo/Type/Rum"));
            Assert.That(loaded.DeclaredTypeKey, Is.EqualTo("Cargo/Type/Spices"));
            Assert.That(loaded.PaintId, Is.EqualTo("Paint_Navy"));
            Assert.That(loaded.StampId, Is.EqualTo("Stamp_PortSeal"));
            Assert.That(loaded.Position, Is.EqualTo(disguised.Position));
        }

        [Test]
        public void An_empty_dock_reads_back_as_no_crates_and_not_as_null()
        {
            var snapshot = new GameSnapshot { Version = GameSnapshot.CurrentVersion };

            Assert.That(RoundTrip(snapshot).Cargo.Crates, Is.Empty);
        }

        [Test]
        public void A_file_from_before_versioning_is_not_readable()
        {
            var loaded = JsonUtility.FromJson<GameSnapshot>("{\"Economy\":{\"Balance\":700}}");

            Assert.That(loaded.Version, Is.Zero);
            Assert.That(loaded.IsReadable, Is.False);
        }

        [Test]
        public void A_file_from_a_newer_game_is_not_readable_either()
        {
            var snapshot = new GameSnapshot { Version = GameSnapshot.CurrentVersion + 1 };

            Assert.That(RoundTrip(snapshot).IsReadable, Is.False);
        }
    }
}
