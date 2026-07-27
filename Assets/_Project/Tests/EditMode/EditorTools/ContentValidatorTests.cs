using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Cargo.Catalog;
using CrateExpectations.Contracts;
using CrateExpectations.EditorTools.Validation;
using CrateExpectations.Inspection;
using NUnit.Framework;

namespace CrateExpectations.EditorTools.Tests
{
    public sealed class ContentValidatorTests
    {
        private ContentFixture _fixture;

        private CargoTypeDefinition _rum;
        private CargoTypeDefinition _spices;
        private PaintDefinition _navy;
        private StampDefinition _portSeal;
        private DisguiseRecipe _pourSpices;
        private DisguiseRecipe _paintNavy;
        private DisguiseRecipe _stampSeal;
        private PortRegulationsDefinition _regulations;

        [SetUp]
        public void SetUp()
        {
            _fixture = new ContentFixture();

            _rum = _fixture.CargoType("ром", "Cargo/Prefab/Rum");
            _spices = _fixture.CargoType("специи", "Cargo/Prefab/Spices");
            _navy = _fixture.Paint("синий");
            _portSeal = _fixture.Stamp("печать порта");

            _pourSpices = _fixture.PourRecipe(_spices);
            _paintNavy = _fixture.PaintRecipe(_navy);

            _stampSeal = _fixture.StampRecipe(_portSeal, requiredPaint: _navy);

            _regulations = _fixture.Regulations((_spices, _navy, _portSeal));
        }

        [TearDown]
        public void TearDown() => _fixture.Dispose();

        private ContentCatalog Catalog(
            IReadOnlyList<ContractDefinition> contracts = null,
            IReadOnlyList<DisguiseRecipe> recipes = null,
            IReadOnlyList<DisguiseStationDefinition> stations = null,
            IReadOnlyList<PortRegulationsDefinition> regulations = null,
            IReadOnlyList<InspectorProfile> profiles = null,
            IReadOnlyList<CargoManifestDefinition> manifests = null,
            CargoRegistryDefinition registry = null)
        {
            return new ContentCatalog(
                contracts,
                null,
                new[] { _rum, _spices },
                recipes ?? new[] { _pourSpices, _paintNavy, _stampSeal },
                stations ?? _fixture.Stations(_pourSpices, _paintNavy, _stampSeal),
                regulations ?? new[] { _regulations },
                profiles,
                manifests,
                addressableEntries: null,
                registries: registry != null ? new[] { registry } : null);
        }

        private static IReadOnlyList<ContentIssue> Run(IContentCheck check, ContentCatalog catalog) =>
            new ContentValidator(check).Validate(catalog);

        private static bool Mentions(IReadOnlyList<ContentIssue> issues, string fragment)
        {
            for (int i = 0; i < issues.Count; i++)
                if (issues[i].Message.Contains(fragment)) 
                    return true;

            return false;
        }


        [Test]
        public void Healthy_content_produces_no_complaints()
        {
            ContractDefinition contract = _fixture.Contract("Ром под видом специй", _rum, _spices, crates: 2);

            IReadOnlyList<ContentIssue> issues = Run(
                new ContractReachableCheck(), Catalog(new[] { contract }));

            Assert.That(issues, Is.Empty, "здоровый контент валидатор трогать не должен");
        }

        [Test]
        public void A_contract_without_a_pouring_station_is_impossible()
        {
            ContractDefinition contract = _fixture.Contract("Ром под видом специй", _rum, _spices);

            IReadOnlyList<ContentIssue> issues = Run(
                new ContractReachableCheck(),
                Catalog(new[] { contract }, stations: _fixture.Stations(_paintNavy, _stampSeal)));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].IsError, Is.True);
            Assert.That(issues[0].Asset, Is.EqualTo(contract));
            Assert.That(Mentions(issues, "не переливает"), Is.True);
        }

        [Test]
        public void A_contract_is_impossible_when_the_required_paint_cannot_be_applied()
        {
            ContractDefinition contract = _fixture.Contract("Ром под видом специй", _rum, _spices);

            IReadOnlyList<ContentIssue> issues = Run(
                new ContractReachableCheck(),
                Catalog(new[] { contract }, stations: _fixture.Stations(_pourSpices, _stampSeal)));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(Mentions(issues, "окраска"), Is.True);
        }

        [Test]
        public void A_contract_is_impossible_when_the_required_stamp_cannot_be_applied()
        {
            ContractDefinition contract = _fixture.Contract("Ром под видом специй", _rum, _spices);

            IReadOnlyList<ContentIssue> issues = Run(
                new ContractReachableCheck(),
                Catalog(new[] { contract }, stations: _fixture.Stations(_pourSpices, _paintNavy)));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(Mentions(issues, "пломба"), Is.True);
        }

        [Test]
        public void A_recipe_that_stands_on_no_station_does_not_make_a_contract_possible()
        {
            ContractDefinition contract = _fixture.Contract("Ром под видом специй", _rum, _spices);

            IReadOnlyList<ContentIssue> issues = Run(
                new ContractReachableCheck(),
                Catalog(new[] { contract }, stations: new List<DisguiseStationDefinition>()));

            Assert.That(issues, Is.Not.Empty);
        }

        [Test]
        public void A_contract_without_disguise_still_has_to_satisfy_the_regulations()
        {
            ContractDefinition contract = _fixture.Contract("Пряности на экспорт", _spices);

            IReadOnlyList<ContentIssue> healthy = Run(
                new ContractReachableCheck(), Catalog(new[] { contract }));
            Assert.That(healthy, Is.Empty);

            IReadOnlyList<ContentIssue> broken = Run(
                new ContractReachableCheck(),
                Catalog(new[] { contract }, stations: _fixture.Stations(_pourSpices)));
            Assert.That(broken, Is.Not.Empty);
        }

        [Test]
        public void Without_any_regulations_the_check_says_so_instead_of_staying_silent()
        {
            ContractDefinition contract = _fixture.Contract("Ром под видом специй", _rum, _spices);

            IReadOnlyList<ContentIssue> issues = Run(
                new ContractReachableCheck(),
                Catalog(new[] { contract }, regulations: new List<PortRegulationsDefinition>()));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].IsError, Is.True);
        }

        [Test]
        public void Regulations_demanding_paint_nobody_applies_are_an_error()
        {
            PaintDefinition unobtainable = _fixture.Paint("пурпурный");
            PortRegulationsDefinition regulations =
                _fixture.Regulations((_spices, unobtainable, _portSeal));

            IReadOnlyList<ContentIssue> issues = Run(
                new RegulationsCoverageCheck(), Catalog(regulations: new[] { regulations }));

            Assert.That(Mentions(issues, "пурпурный"), Is.True);
            Assert.That(issues[0].Asset, Is.EqualTo(regulations));
        }

        [Test]
        public void A_recipe_left_off_every_station_is_reported_as_a_warning()
        {
            IReadOnlyList<ContentIssue> issues = Run(
                new RegulationsCoverageCheck(),
                Catalog(stations: _fixture.Stations(_pourSpices, _paintNavy)));

            ContentIssue orphan = default;

            for (int i = 0; i < issues.Count; i++)
                if (issues[i].Asset == _stampSeal) orphan = issues[i];

            Assert.That(orphan.Asset, Is.EqualTo(_stampSeal), "рецепт без станции должен быть замечен");
            Assert.That(orphan.Level, Is.EqualTo(IssueLevel.Warning));
            Assert.That(orphan.Message, Does.Contain("не стоит ни на одной станции"));
        }


        [Test]
        public void A_contract_asking_for_more_crates_than_the_dock_holds_is_an_error()
        {
            ContractDefinition contract = _fixture.Contract("Ром под видом специй", _rum, _spices, crates: 2);
            CargoManifestDefinition manifest = _fixture.Manifest("rum", "spices");

            IReadOnlyList<ContentIssue> issues = Run(
                new DockSupplyCheck(),
                Catalog(
                    new[] { contract },
                    manifests: new[] { manifest },
                    registry: _fixture.Registry(("rum", _rum), ("spices", _spices))));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].IsError, Is.True);
            Assert.That(Mentions(issues, "Закрыть заказ нечем"), Is.True);
        }

        [Test]
        public void A_contract_with_no_spare_crates_is_a_warning()
        {
            ContractDefinition contract = _fixture.Contract(
                "Ром под видом специй", _rum, _spices, crates: 2, allowedSeizures: 1);
            CargoManifestDefinition manifest = _fixture.Manifest("rum", "rum");

            IReadOnlyList<ContentIssue> issues = Run(
                new DockSupplyCheck(),
                Catalog(
                    new[] { contract },
                    manifests: new[] { manifest },
                    registry: _fixture.Registry(("rum", _rum))));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Level, Is.EqualTo(IssueLevel.Warning));
        }

        [Test]
        public void Enough_crates_on_the_dock_produce_no_complaints()
        {
            ContractDefinition contract = _fixture.Contract(
                "Ром под видом специй", _rum, _spices, crates: 2, allowedSeizures: 1);
            CargoManifestDefinition manifest = _fixture.Manifest("rum", "rum", "rum");

            IReadOnlyList<ContentIssue> issues = Run(
                new DockSupplyCheck(),
                Catalog(
                    new[] { contract },
                    manifests: new[] { manifest },
                    registry: _fixture.Registry(("rum", _rum))));

            Assert.That(issues, Is.Empty);
        }


        [Test]
        public void A_contract_without_cargo_is_an_error()
        {
            ContractDefinition contract = _fixture.Contract("Без груза", cargo: null);

            IReadOnlyList<ContentIssue> issues = Run(
                new BrokenReferencesCheck(), Catalog(new[] { contract }));

            Assert.That(Mentions(issues, "не задан истинный тип груза"), Is.True);
            Assert.That(issues[0].Asset, Is.EqualTo(contract));
        }

        [Test]
        public void A_recipe_without_a_target_is_an_error()
        {
            DisguiseRecipe broken = _fixture.EmptyRecipe(DisguiseAction.Paint);

            IReadOnlyList<ContentIssue> issues = Run(
                new BrokenReferencesCheck(), Catalog(recipes: new[] { broken }));

            Assert.That(Mentions(issues, "цель не задана"), Is.True);
        }


        [Test]
        public void An_inspector_whose_threshold_is_out_of_reach_is_an_error()
        {
            InspectorProfile profile = _fixture.Profile(
                "Ленивый", ClueChecks.Stamp, threshold: 60f, missingStamp: 20f, wrongStamp: 15f);

            IReadOnlyList<ContentIssue> issues = Run(
                new InspectorBalanceCheck(), Catalog(profiles: new[] { profile }));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].IsError, Is.True);
            Assert.That(Mentions(issues, "не задержит никого"), Is.True);
        }

        [Test]
        public void An_inspector_busting_on_a_single_small_flaw_is_a_warning()
        {
            InspectorProfile profile = _fixture.Profile(
                "Нервный", ClueChecks.All, threshold: 15f);

            IReadOnlyList<ContentIssue> issues = Run(
                new InspectorBalanceCheck(), Catalog(profiles: new[] { profile }));

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Level, Is.EqualTo(IssueLevel.Warning));
            Assert.That(Mentions(issues, "одной"), Is.True);
        }

        [Test]
        public void A_balanced_inspector_produces_no_complaints()
        {
            InspectorProfile profile = _fixture.Profile("Дотошный", ClueChecks.All, threshold: 40f);

            IReadOnlyList<ContentIssue> issues = Run(
                new InspectorBalanceCheck(), Catalog(profiles: new[] { profile }));

            Assert.That(issues, Is.Empty);
        }


        [Test]
        public void Errors_are_listed_before_warnings()
        {
            ContractDefinition broken = _fixture.Contract("Без груза", cargo: null, description: string.Empty);

            IReadOnlyList<ContentIssue> issues = new ContentValidator(new BrokenReferencesCheck())
                .Validate(Catalog(new[] { broken }));

            Assert.That(issues.Count, Is.GreaterThan(1));
            Assert.That(issues[0].IsError, Is.True);
            Assert.That(issues[issues.Count - 1].IsError, Is.False);
        }

        [Test]
        public void The_full_validator_runs_every_check()
        {
            Assert.That(new ContentValidator().Checks.Count, Is.EqualTo(6));
        }
    }
}
