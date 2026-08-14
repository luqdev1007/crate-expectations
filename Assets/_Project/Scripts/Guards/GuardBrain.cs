namespace CrateExpectations.Guards
{
    /// <summary>
    /// Решает, чем стражнику заниматься. Чистая логика по образцу <c>ClueEvaluator</c>
    /// и <c>PayoutCalculator</c>: ни сцены, ни <c>MonoBehaviour</c>, ни собственной
    /// случайности - один и тот же контекст всегда даёт одно и то же намерение.
    /// <para>
    /// Правил сейчас ровно два, и отдельный класс под них выглядит избыточно -
    /// это осознанно. Мозг расширяется дальше (агрессия, тревога, преследование)
    /// <b>не меняя интерфейс</b>: новыми полями <see cref="GuardContext"/> и новыми
    /// ветками в <see cref="Decide"/>. Ни <c>GuardAI</c>, ни состояния FSM при этом
    /// не трогаются вовсе - они как спрашивали намерение, так и спрашивают.
    /// Заводить эту границу задним числом, когда правил станет шесть, дороже
    /// </para>
    /// </summary>
    public sealed class GuardBrain
    {
        /// <summary>Чем заняться при таких вводных</summary>
        /// <param name="context">Снимок того, что о стражнике известно</param>
        public GuardIntent Decide(in GuardContext context)
        {
            // Удар бьёт всё остальное: стражник, которого рубят, не продолжает обход
            // как ни в чём не бывало. Проверка стоит первой, а не последней, - порядок
            // веток здесь и есть приоритет намерений, и когда их станет шесть,
            // читаться он должен сверху вниз
            if (context.IsStaggered)
                return GuardIntent.Stagger;

            return context.HasPatrolRoute ? GuardIntent.Patrol : GuardIntent.HoldPost;
        }
    }
}
