using System;
using NUnit.Framework;

namespace CrateExpectations.Inspection.Tests
{
    public sealed class InspectionAspectsTests
    {
        [Test]
        public void EveryClueType_ShowsUpOnSomeStep()
        {
            foreach (ClueType clue in Enum.GetValues(typeof(ClueType)))
            {
                InspectionAspect aspect = InspectionAspects.Of(clue);

                Assert.That(Enum.IsDefined(typeof(InspectionAspect), aspect), Is.True,
                    $"улике {clue} не сопоставлен шаг осмотра - игрок не увидит, за что его поймали");
            }
        }

        [Test]
        public void EveryAspect_IsBackedByAtLeastOneCheck()
        {
            foreach (InspectionAspect aspect in Enum.GetValues(typeof(InspectionAspect)))
            {
                Assert.That(InspectionAspects.ChecksOf(aspect), Is.Not.EqualTo(ClueChecks.None),
                    $"шаг {aspect} не привязан ни к одной проверке и потому не сыграет никогда");
            }
        }

        [Test]
        public void ClueStep_IsPerformedByAnInspectorWhoLooksForThatClue()
        {
            foreach (ClueType clue in Enum.GetValues(typeof(ClueType)))
            {
                ClueChecks required = InspectionAspects.ChecksOf(InspectionAspects.Of(clue));

                Assert.That(required & ClueChecks.All, Is.Not.EqualTo(ClueChecks.None),
                    $"шаг для улики {clue} пропустит даже дотошный инспектор");
            }
        }

        [Test]
        public void PaintStep_CoversBothWrongPaintAndMissingPaint()
        {
            Assert.That(InspectionAspects.Of(ClueType.PaintMismatch),
                Is.EqualTo(InspectionAspect.Paint));
            Assert.That(InspectionAspects.Of(ClueType.IncompleteDisguise),
                Is.EqualTo(InspectionAspect.Paint));

            ClueChecks paintChecks = InspectionAspects.ChecksOf(InspectionAspect.Paint);
            Assert.That(paintChecks.HasFlag(ClueChecks.Paint), Is.True);
            Assert.That(paintChecks.HasFlag(ClueChecks.Completeness), Is.True);
        }

        [Test]
        public void LazyInspector_SkipsStepsHeDoesNotPerform()
        {
            const ClueChecks lazyChecks = ClueChecks.Stamp;

            Assert.That(InspectionAspects.ChecksOf(InspectionAspect.Stamp) & lazyChecks,
                Is.Not.EqualTo(ClueChecks.None), "пломбу ленивый смотрит");
            Assert.That(InspectionAspects.ChecksOf(InspectionAspect.Contents) & lazyChecks,
                Is.EqualTo(ClueChecks.None), "внутрь ленивый не лезет");
            Assert.That(InspectionAspects.ChecksOf(InspectionAspect.Manifest) & lazyChecks,
                Is.EqualTo(ClueChecks.None), "бумаги ленивый не читает");
        }
    }
}
