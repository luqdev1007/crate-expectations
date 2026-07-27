using System.Collections.Generic;
using System.Text;
using CrateExpectations.Cargo;
using CrateExpectations.Contracts;
using CrateExpectations.Inspection;

namespace CrateExpectations.EditorTools.Validation
{
    public sealed class ContractReachableCheck : IContentCheck
    {
        private static readonly InspectionPolicy Appearance = new(
            ClueChecks.Paint | ClueChecks.Stamp | ClueChecks.Completeness,
            new ClueWeights(0f, 0f, 1f, 1f, 1f, 1f),
            suspicionThreshold: float.MaxValue);

        private readonly DisguiseProcessor _processor = new();
        private readonly ClueEvaluator _evaluator = new();

        private readonly HashSet<CargoState> _visited = new();
        private readonly Queue<CargoState> _frontier = new();
        private readonly StringBuilder _builder = new(160);

        public string Title => "Выполнимость заказов";

        public void Run(ContentCatalog catalog, List<ContentIssue> issues)
        {
            if (catalog.Regulations.Count == 0)
            {
                issues.Add(ContentIssue.Error(
                    Title,
                    "В проекте нет ни одного регламента порта - сверять внешний вид груза не с чем"));
                return;
            }

            List<DisguiseRecipe> recipes = catalog.ReachableRecipes();

            for (int i = 0; i < catalog.Contracts.Count; i++)
            {
                ContractDefinition contract = catalog.Contracts[i];

                if (contract == null || contract.Cargo == null) 
                    continue;

                for (int r = 0; r < catalog.Regulations.Count; r++)
                    Check(contract, catalog.Regulations[r], recipes, issues);
            }
        }

        private void Check(
            ContractDefinition contract,
            PortRegulationsDefinition regulations,
            List<DisguiseRecipe> recipes,
            List<ContentIssue> issues)
        {
            if (regulations == null) 
                return;

            CargoTypeDefinition declaredAs = contract.DeclaredAs != null
                ? contract.DeclaredAs
                : contract.Cargo;

            var identity = new CargoIdentity(contract.Cargo);
            Explore(identity, recipes);

            CargoState best = default;
            int fewestClues = int.MaxValue;
            bool declarationReachable = false;

            foreach (CargoState state in _visited)
            {
                if (state.DeclaredType != declaredAs) 
                    continue;

                declarationReachable = true;

                InspectionSubject subject = regulations.CreateSubject(state, identity);
                int clues = _evaluator.Evaluate(subject, Appearance).Clues.Count;

                if (clues >= fewestClues) 
                    continue;

                fewestClues = clues;
                best = state;
            }

            if (fewestClues == 0) 
                return;

            if (!declarationReachable)
            {
                issues.Add(ContentIssue.Error(
                    Title,
                    $"Заказ \"{contract.DisplayName}\": груз надо выдать за \"{Name(declaredAs)}\", " +
                    "но ни одна станция не переливает содержимое в этот тип. Заказ невыполним",
                    contract));
                return;
            }

            issues.Add(ContentIssue.Error(
                Title,
                $"Заказ \"{contract.DisplayName}\": даже в лучшем состоянии груз не соответствует " +
                $"регламенту \"{regulations.name}\" - {Describe(regulations, declaredAs, best)}. " +
                "Заказ невыполним начисто",
                contract));
        }

        private void Explore(in CargoIdentity identity, List<DisguiseRecipe> recipes)
        {
            _visited.Clear();
            _frontier.Clear();

            CargoState start = CargoState.Undisguised(identity);
            _visited.Add(start);
            _frontier.Enqueue(start);

            while (_frontier.Count > 0)
            {
                CargoState state = _frontier.Dequeue();

                for (int i = 0; i < recipes.Count; i++)
                {
                    DisguiseRecipe recipe = recipes[i];

                    if (recipe == null) 
                        continue;

                    DisguiseResult result = _processor.Apply(state, identity, recipe.Operation);

                    if (!result.Changed) 
                        continue;

                    if (_visited.Add(result.State)) 
                        _frontier.Enqueue(result.State);
                }
            }
        }

        private string Describe(
            PortRegulationsDefinition regulations, CargoTypeDefinition declaredAs, in CargoState best)
        {
            _builder.Clear();

            if (!regulations.TryGetRequirement(
                    declaredAs, out PortRegulationsDefinition.Requirement requirement))
            {
                return "требований к этому типу в регламенте нет, но улики всё равно нашлись";
            }

            if (requirement.Paint != null && best.Paint != requirement.Paint)
            {
                _builder.Append("нужна окраска \"").Append(requirement.Paint.DisplayName)
                    .Append("\", а на доступных станциях её не получить");
            }

            if (requirement.Stamp != null && best.Stamp != requirement.Stamp)
            {
                if (_builder.Length > 0) _builder.Append("; ");
                _builder.Append("нужна пломба \"").Append(requirement.Stamp.DisplayName)
                    .Append("\", а на доступных станциях её не поставить");
            }

            return _builder.Length > 0 ? _builder.ToString() : "внешний вид не сходится с регламентом";
        }

        private static string Name(CargoTypeDefinition type) =>
            type != null ? type.DisplayName : "- тип не задан -";
    }
}
