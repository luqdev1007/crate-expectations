namespace CrateExpectations.Cargo
{
    /// <summary>
    /// Ядро маскировки: единственное место, где решается, как рецепт меняет заявленное
    /// состояние груза. Чистая функция - ничего не хранит, ничего не мутирует, не знает про
    /// сцену и MonoBehaviour. Отсюда два следствия: логика покрывается edit-mode тестами
    /// без запуска сцены, а станция может "примерить" рецепт для подсказки тем же вызовом,
    /// каким потом применит его по-настоящему
    /// </summary>
    public sealed class DisguiseProcessor
    {
        /// <summary>
        /// Применить рецепт к состоянию груза
        /// </summary>
        /// <param name="state">Текущее заявленное состояние</param>
        /// <param name="identity">Истина о грузе. Передаётся <c>in</c> и является неизменяемым
        /// значением - процессор может её только прочитать.</param>
        /// <param name="operation">Что делаем</param>
        /// <returns>Новое состояние с исходом; при отказе состояние возвращается нетронутым</returns>
        public DisguiseResult Apply(
            in CargoState state,
            in CargoIdentity identity,
            in DisguiseOperation operation)
        {
            if (operation.RequiredPaint != null && operation.RequiredPaint != state.Paint)
                return Reject(state, identity, DisguiseRejection.PaintPrerequisite);

            CargoState next;
            switch (operation.Action)
            {
                case DisguiseAction.Paint:
                    if (operation.Paint == null)
                        return Reject(state, identity, DisguiseRejection.IncompleteRecipe);
                    next = state.WithPaint(operation.Paint);
                    break;

                case DisguiseAction.Stamp:
                    if (operation.Stamp == null)
                        return Reject(state, identity, DisguiseRejection.IncompleteRecipe);
                    // Печать не накапливается: новая пломба заменяет прежнюю
                    next = state.WithStamp(operation.Stamp);
                    break;

                case DisguiseAction.Pour:
                    if (operation.DeclaredType == null)
                        return Reject(state, identity, DisguiseRejection.IncompleteRecipe);
                    // Меняется только "витрина". CargoIdentity сюда не попадает даже случайно:
                    // у неё нет ни одного мутирующего члена
                    next = state.WithDeclaredType(operation.DeclaredType);
                    break;

                default:
                    return Reject(state, identity, DisguiseRejection.IncompleteRecipe);
            }

            // Повторное применение того же рецепта - осознанный no-op, а не ошибка:
            // игрок может жать E сколько угодно, состояние от этого не портится
            bool diverges = !next.MatchesTruth(identity);
            return next.Equals(state)
                ? DisguiseResult.AlreadyApplied(state, diverges)
                : DisguiseResult.Applied(next, diverges);
        }

        private static DisguiseResult Reject(
            in CargoState state, in CargoIdentity identity, DisguiseRejection rejection) =>
            DisguiseResult.Rejected(state, !state.MatchesTruth(identity), rejection);
    }
}
