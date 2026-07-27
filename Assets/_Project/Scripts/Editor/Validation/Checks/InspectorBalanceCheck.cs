using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Inspection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrateExpectations.EditorTools.Validation
{
    public sealed class InspectorBalanceCheck : IContentCheck
    {
        private readonly ClueEvaluator _evaluator = new();

        public string Title => "Профили инспекторов";
        public void Run(ContentCatalog catalog, List<ContentIssue> issues)
        {
            if (catalog.InspectorProfiles.Count == 0) 
                return;

            var props = new Props();

            try
            {
                for (int i = 0; i < catalog.InspectorProfiles.Count; i++)
                    CheckProfile(catalog.InspectorProfiles[i], props, issues);
            }
            finally
            {
                props.Dispose();
            }
        }

        private void CheckProfile(InspectorProfile profile, Props props, List<ContentIssue> issues)
        {
            if (profile == null) 
                return;

            InspectionPolicy policy = profile.Policy;
            float threshold = policy.SuspicionThreshold;

            float worst = 0f;

            for (int i = 0; i < props.WorstCases.Count; i++)
                worst = Mathf.Max(worst, _evaluator.Evaluate(props.WorstCases[i], policy).Suspicion);

            if (worst < threshold)
            {
                issues.Add(ContentIssue.Error(
                    Title,
                    $"Профиль \"{profile.DisplayName}\": порог {threshold:0.#}, а больше " +
                    $"{worst:0.#} подозрения ему не набрать при самых грубых уликах. " +
                    "Такой инспектор не задержит никого и никогда",
                    profile));

                return;
            }

            float smallest = float.MaxValue;
            string smallestName = string.Empty;

            for (int i = 0; i < props.MinorFlaws.Count; i++)
            {
                MinorFlaw flaw = props.MinorFlaws[i];
                float suspicion = _evaluator.Evaluate(flaw.Subject, policy).Suspicion;

                if (suspicion <= 0f || suspicion >= smallest) 
                    continue;

                smallest = suspicion;
                smallestName = flaw.Name;
            }

            if (smallest < float.MaxValue && smallest >= threshold)
            {
                issues.Add(ContentIssue.Warning(
                    Title,
                    $"Профиль \"{profile.DisplayName}\": порог {threshold:0.#} берётся одной " +
                    $"мелочью - \"{smallestName}\" даёт {smallest:0.#}. Маскировка перестаёт " +
                    "быть игрой: любая единственная ошибка сразу вердикт",
                    profile));
            }
        }

        private readonly struct MinorFlaw
        {
            public MinorFlaw(string name, in InspectionSubject subject)
            {
                Name = name;
                Subject = subject;
            }

            public string Name { get; }

            public InspectionSubject Subject { get; }
        }

        private sealed class Props
        {
            private readonly List<Object> _created = new(8);

            private readonly CargoTypeDefinition _legal;
            private readonly CargoTypeDefinition _contraband;
            private readonly CargoTypeDefinition _other;
            private readonly PaintDefinition _expectedPaint;
            private readonly PaintDefinition _wrongPaint;
            private readonly StampDefinition _requiredStamp;
            private readonly StampDefinition _wrongStamp;

            public Props()
            {
                _legal = Create<CargoTypeDefinition>();
                _contraband = Create<CargoTypeDefinition>();
                _other = Create<CargoTypeDefinition>();
                _expectedPaint = Create<PaintDefinition>();
                _wrongPaint = Create<PaintDefinition>();
                _requiredStamp = Create<StampDefinition>();
                _wrongStamp = Create<StampDefinition>();

                var so = new SerializedObject(_contraband);
                so.FindProperty("<IsContraband>k__BackingField").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();

                WorstCases = new List<InspectionSubject>
                {
                    Subject(_expectedPaint, _requiredStamp,
                        declared: _contraband, paint: _wrongPaint, stamp: _wrongStamp, truth: _other),
                    Subject(_expectedPaint, _requiredStamp,
                        declared: _contraband, paint: _wrongPaint, stamp: null, truth: _other),
                    Subject(_expectedPaint, _requiredStamp,
                        declared: _contraband, paint: null, stamp: _wrongStamp, truth: _other),
                    Subject(_expectedPaint, _requiredStamp,
                        declared: _contraband, paint: null, stamp: null, truth: _other),
                };

                MinorFlaws = new List<MinorFlaw>
                {
                    new("не та окраска", Subject(_expectedPaint, _requiredStamp,
                        declared: _legal, paint: _wrongPaint, stamp: _requiredStamp, truth: _legal)),
                    new("окраска не нанесена", Subject(_expectedPaint, _requiredStamp,
                        declared: _legal, paint: null, stamp: _requiredStamp, truth: _legal)),
                    new("нет пломбы", Subject(_expectedPaint, _requiredStamp,
                        declared: _legal, paint: _expectedPaint, stamp: null, truth: _legal)),
                    new("не та пломба", Subject(_expectedPaint, _requiredStamp,
                        declared: _legal, paint: _expectedPaint, stamp: _wrongStamp, truth: _legal)),
                };
            }

            public List<MinorFlaw> MinorFlaws { get; }

            public List<InspectionSubject> WorstCases { get; }

            public void Dispose()
            {
                for (int i = 0; i < _created.Count; i++)
                    if (_created[i] != null) 
                        Object.DestroyImmediate(_created[i]);

                _created.Clear();
            }

            private static InspectionSubject Subject(
                PaintDefinition expectedPaint,
                StampDefinition requiredStamp,
                CargoTypeDefinition declared,
                PaintDefinition paint,
                StampDefinition stamp,
                CargoTypeDefinition truth)
            {
                var state = new CargoState(paint, stamp, declared);
                var identity = new CargoIdentity(truth);

                return new InspectionSubject(state, identity, expectedPaint, requiredStamp);
            }

            private T Create<T>() where T : ScriptableObject
            {
                T instance = ScriptableObject.CreateInstance<T>();
                _created.Add(instance);

                return instance;
            }
        }
    }
}
