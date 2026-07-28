using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CrateExpectations.Inspection.Tests
{
    public sealed class CompositeInspectorVoiceTests
    {
        private sealed class VoiceSpy : IInspectorVoice
        {
            public readonly List<string> Said = new();

            public int Cleared { get; private set; }

            public VerdictReport LastReport { get; private set; }

            public void Say(string line) => Said.Add(line);

            public void ShowVerdict(in VerdictReport report) => LastReport = report;

            public void Clear() => Cleared++;
        }

        private static VerdictReport Report() => new(
            "Никуда вы это не повезёте.", Color.red, new Verdict(VerdictOutcome.Bust, 70f, 40f, null));

        [Test]
        public void EveryLine_ReachesEverySink()
        {
            var bubble = new VoiceSpy();
            var hud = new VoiceSpy();
            var voice = new CompositeInspectorVoice(bubble, hud);

            voice.Say("Так, посмотрим.");
            voice.Say("Окраска.");

            Assert.That(bubble.Said, Is.EqualTo(new[] { "Так, посмотрим.", "Окраска." }));
            Assert.That(hud.Said, Is.EqualTo(bubble.Said));
        }

        [Test]
        public void Verdict_ReachesEverySink()
        {
            var bubble = new VoiceSpy();
            var hud = new VoiceSpy();
            var voice = new CompositeInspectorVoice(bubble, hud);

            voice.ShowVerdict(Report());

            Assert.That(bubble.LastReport.Speech, Is.EqualTo("Никуда вы это не повезёте."));
            Assert.That(hud.LastReport.IsBust, Is.True);
        }

        [Test]
        public void Clear_ReachesEverySink()
        {
            var bubble = new VoiceSpy();
            var hud = new VoiceSpy();
            var voice = new CompositeInspectorVoice(bubble, hud);

            voice.Clear();
            voice.Clear();

            Assert.That(bubble.Cleared, Is.EqualTo(2));
            Assert.That(hud.Cleared, Is.EqualTo(2));
        }

        [Test]
        public void MissingSink_DoesNotBreakTheRest()
        {
            var bubble = new VoiceSpy();
            var voice = new CompositeInspectorVoice(bubble, null);

            Assert.DoesNotThrow(() =>
            {
                voice.Say("Пломба.");
                voice.ShowVerdict(Report());
                voice.Clear();
            });

            Assert.That(bubble.Said, Is.EqualTo(new[] { "Пломба." }));
            Assert.That(bubble.Cleared, Is.EqualTo(1));
        }
    }
}
