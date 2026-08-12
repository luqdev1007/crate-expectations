using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CrateExpectations.EditorTools.Animation
{
    /// <summary>
    /// Общее для всех билдеров графов: как начать пересборку, не сломав сцену.
    /// <para>
    /// Правило одно и стоит дорого. Содержимое графа собирается с нуля - иначе в нём
    /// копились бы переходы, которых в коде билдера уже нет, - но САМ ФАЙЛ обязан
    /// пережить пересборку. <c>DeleteAsset</c> + <c>CreateAnimatorControllerAtPath</c>
    /// даёт файл с НОВЫМ GUID, и все аниматоры в сценах после этого остаются с пустым
    /// контроллером. Молча: ошибок в консоли нет, модель просто перестаёт шевелиться.
    /// </para>
    /// </summary>
    public static class AnimatorControllerRebuild
    {
        /// <summary>
        /// Пустой граф по этому пути: существующий - вычищенный, но тот же самый файл;
        /// отсутствующий - созданный
        /// </summary>
        public static AnimatorController LoadOrCreate(string controllerPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (controller == null)
                return AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            while (controller.parameters.Length > 0)
                controller.RemoveParameter(0);

            AnimatorStateMachine root = controller.layers[0].stateMachine;

            // Переходы из Any State живут на самой машине, а не на стейтах, и удалением
            // стейтов не подчищаются
            root.anyStateTransitions = new AnimatorStateTransition[0];

            foreach (ChildAnimatorState state in root.states)
                root.RemoveState(state.state);

            // Блендтри - подобъекты ассета, и RemoveState их не забирает: стейт уходит,
            // дерево остаётся лежать в файле. Без этой уборки каждая пересборка добавляла бы
            // в контроллер по мёртвому дереву, и заметно это стало бы только по размеру файла
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
                if (asset is BlendTree)
                    Object.DestroyImmediate(asset, true);

            return controller;
        }

        /// <summary>
        /// Float-параметр со значением по умолчанию. Отдельным методом, потому что
        /// <c>AddParameter(name, type)</c> значения по умолчанию не принимает вовсе,
        /// а параметр множителя скорости, стартующий с нуля, останавливает клип
        /// </summary>
        public static void AddFloat(AnimatorController controller, string name, float defaultValue)
        {
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = defaultValue,
            });
        }
    }
}
