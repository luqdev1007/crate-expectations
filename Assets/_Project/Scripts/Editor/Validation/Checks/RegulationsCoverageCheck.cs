using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Inspection;

namespace CrateExpectations.EditorTools.Validation
{
    public sealed class RegulationsCoverageCheck : IContentCheck
    {
        public string Title => "Регламент порта";

        public void Run(ContentCatalog catalog, List<ContentIssue> issues)
        {
            List<DisguiseRecipe> recipes = catalog.ReachableRecipes();

            for (int r = 0; r < catalog.Regulations.Count; r++)
            {
                PortRegulationsDefinition regulations = catalog.Regulations[r];

                if (regulations == null) 
                    continue;

                if (regulations.TryGetRequirement(null, out _))
                {
                    issues.Add(ContentIssue.Error(
                        Title,
                        $"В регламенте \"{regulations.name}\" есть строка без типа груза - " +
                        "она ни к чему не применится",
                        regulations));
                }

                for (int i = 0; i < catalog.CargoTypes.Count; i++)
                {
                    CargoTypeDefinition type = catalog.CargoTypes[i];

                    if (type == null) 
                        continue;

                    if (!regulations.TryGetRequirement(
                            type, out PortRegulationsDefinition.Requirement requirement))
                    {
                        continue;
                    }

                    if (requirement.Paint != null && !Produces(recipes, requirement.Paint))
                    {
                        issues.Add(ContentIssue.Error(
                            Title,
                            $"Регламент \"{regulations.name}\" требует для \"{type.DisplayName}\" " +
                            $"окраску \"{requirement.Paint.DisplayName}\", но её не наносит " +
                            "ни одна станция",
                            regulations));
                    }

                    if (requirement.Stamp != null && !Produces(recipes, requirement.Stamp))
                    {
                        issues.Add(ContentIssue.Error(
                            Title,
                            $"Регламент \"{regulations.name}\" требует для \"{type.DisplayName}\" " +
                            $"пломбу \"{requirement.Stamp.DisplayName}\", но её не ставит " +
                            "ни одна станция",
                            regulations));
                    }
                }
            }

            ReportOrphanRecipes(catalog, issues);
        }

        private void ReportOrphanRecipes(ContentCatalog catalog, List<ContentIssue> issues)
        {
            List<DisguiseRecipe> reachable = catalog.ReachableRecipes();

            for (int i = 0; i < catalog.Recipes.Count; i++)
            {
                DisguiseRecipe recipe = catalog.Recipes[i];

                if (recipe == null || reachable.Contains(recipe)) 
                    continue;

                issues.Add(ContentIssue.Warning(
                    Title,
                    $"Рецепт \"{recipe.name}\" не стоит ни на одной станции - применить его негде",
                    recipe));
            }
        }

        private static bool Produces(List<DisguiseRecipe> recipes, PaintDefinition paint)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                DisguiseRecipe recipe = recipes[i];

                if (recipe != null && recipe.Action == DisguiseAction.Paint && recipe.Paint == paint)
                    return true;
            }

            return false;
        }

        private static bool Produces(List<DisguiseRecipe> recipes, StampDefinition stamp)
        {
            for (int i = 0; i < recipes.Count; i++)
            {
                DisguiseRecipe recipe = recipes[i];

                if (recipe != null && recipe.Action == DisguiseAction.Stamp && recipe.Stamp == stamp)
                    return true;
            }

            return false;
        }
    }
}
