using System;
using System.Collections.Generic;

namespace CrateExpectations.Inspection
{
    /// <summary>
    /// Ядро досмотра: сравнивает заявленное с истиной по правилам конкретного инспектора
    /// и выдаёт <see cref="Verdict"/> с причинами. Чистая логика - ни сцены, ни MonoBehaviour,
    /// ни собственной случайности: всё, что влияет на результат, приходит аргументами.
    ///
    /// Главное правило модели: <b>инспектор не рентген</b>. Подмену содержимого
    /// (<see cref="ClueType.ContentMismatch"/>) он находит только тогда, когда внешность
    /// дала повод вскрыть ящик. Поэтому доведённая до конца маскировка проходит даже
    /// дотошного инспектора - иначе игру нельзя было бы выиграть
    /// </summary>
    public sealed class ClueEvaluator
    {
        // Улик по определению не больше, чем типов: буфер заводится один раз и переиспользуется
        private readonly List<Clue> _found = new(6);

        private readonly IChanceSource _chance;

        /// <param name="chance">
        /// Источник случайности для "невнимательности". Без него инспектор не пропускает
        /// ни одной улики - детерминированный режим, удобный для тестов
        /// </param>
        public ClueEvaluator(IChanceSource chance = null) => _chance = chance;

        /// <summary>
        /// Осмотреть груз по правилам инспектора
        /// </summary>
        /// <param name="subject">Заявленное, истина и требования порта. Только чтение</param>
        /// <param name="policy">Что инспектор проверяет и как оценивает найденное</param>
        public Verdict Evaluate(in InspectionSubject subject, in InspectionPolicy policy)
        {
            _found.Clear();
            float suspicion = 0f;

            suspicion += CollectAppearanceClues(subject, policy);

            // Внешность уже даёт повод придраться - считаем до сверки документов,
            // чтобы "вскрытие" зависело именно от того, что видно снаружи
            bool appearanceBetrayed = _found.Count > 0;

            if (policy.Performs(ClueChecks.Manifest) && IsDeclaredContraband(subject))
                suspicion += TryAdd(ClueType.DeclaredContraband, policy);

            if (policy.Performs(ClueChecks.Contents) && appearanceBetrayed &&
                !subject.Declared.MatchesTruth(subject.Truth))
            {
                suspicion += TryAdd(ClueType.ContentMismatch, policy);
            }

            VerdictOutcome outcome = suspicion >= policy.SuspicionThreshold
                ? VerdictOutcome.Bust
                : VerdictOutcome.Pass;

            // Наружу отдаётся снимок: вердикт живёт дольше вызова - его показывает UI
            // и разносит событие, а буфер к следующему досмотру уже перезаписан
            Clue[] clues = _found.Count > 0 ? _found.ToArray() : Array.Empty<Clue>();
            return new Verdict(outcome, suspicion, policy.SuspicionThreshold, clues);
        }

        /// <summary>Всё, что видно снаружи: окраска и пломба против требований порта</summary>
        private float CollectAppearanceClues(in InspectionSubject subject, in InspectionPolicy policy)
        {
            float suspicion = 0f;

            bool paintRequired = subject.ExpectedPaint != null;
            bool paintMissing = paintRequired && subject.Declared.Paint == null;
            bool paintWrong = paintRequired && subject.Declared.Paint != null &&
                              subject.Declared.Paint != subject.ExpectedPaint;

            if (paintMissing && policy.Performs(ClueChecks.Completeness))
                suspicion += TryAdd(ClueType.IncompleteDisguise, policy);

            if (paintWrong && policy.Performs(ClueChecks.Paint))
                suspicion += TryAdd(ClueType.PaintMismatch, policy);

            bool stampRequired = subject.RequiredStamp != null;
            if (stampRequired && policy.Performs(ClueChecks.Stamp))
            {
                if (subject.Declared.Stamp == null)
                    suspicion += TryAdd(ClueType.MissingStamp, policy);
                else if (subject.Declared.Stamp != subject.RequiredStamp)
                    suspicion += TryAdd(ClueType.WrongStamp, policy);
            }

            return suspicion;
        }

        private static bool IsDeclaredContraband(in InspectionSubject subject) =>
            subject.Declared.DeclaredType != null && subject.Declared.DeclaredType.IsContraband;

        /// <summary>
        /// Записать улику, если инспектор её не проморгал. Пропущенная улика не попадает
        /// в вердикт вообще: инспектор её не заметил, значит и озвучивать нечего
        /// </summary>
        private float TryAdd(ClueType type, in InspectionPolicy policy)
        {
            if (IsOverlooked(policy)) return 0f;

            float weight = policy.Weights.Of(type);
            _found.Add(new Clue(type, weight));
            return weight;
        }

        private bool IsOverlooked(in InspectionPolicy policy)
        {
            if (_chance == null || policy.OverlookChance <= 0f) return false;
            return _chance.NextUnit() < policy.OverlookChance;
        }
    }
}
