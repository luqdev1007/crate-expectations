namespace CrateExpectations.Cargo
{
    public sealed class DisguiseProcessor
    {
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

                    next = state.WithStamp(operation.Stamp);
                    break;

                case DisguiseAction.Pour:
                    if (operation.DeclaredType == null)
                        return Reject(state, identity, DisguiseRejection.IncompleteRecipe);

                    next = state.WithDeclaredType(operation.DeclaredType);
                    break;

                default:
                    return Reject(state, identity, DisguiseRejection.IncompleteRecipe);
            }

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
