using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CrateExpectations.EditorTools.Animation
{
    /// <summary>
    /// Собирает контроллер анимаций игрока с нуля. Граф руками не кликается: пересборка
    /// должна давать один и тот же результат при любом состоянии проекта, а изменения
    /// в структуре стейтов - читаться в диффе как код, а не как перемешанный YAML
    /// <para>
    /// Анимаций доставания и убирания в проекте нет вообще, поэтому их роль играет кроссфейд
    /// между обычной стойкой и боевой. Его длительность обязана совпадать с
    /// <c>DrawDuration</c> / <c>SheatheDuration</c> в ассете оружия - иначе оружие появится
    /// в руке раньше или позже, чем рука дойдёт до стойки.
    /// </para>
    /// <para>
    /// Стоек две, и каждая - не поза, а ДЕРЕВО: покой в центре и бег по кругу. С тех пор,
    /// как тело видно из-под собственного подбородка, ноги перестали быть чужой заботой -
    /// игрок смотрит на них сам. Двумерное дерево, а не одномерное «стоит-бежит»:
    /// в шутере ходят боком не реже, чем вперёд, и бег вперёд, отыгранный при движении
    /// вбок, читается как скольжение.
    /// </para>
    /// <para>
    /// Боевые клипы бега в паке отсутствуют, поэтому обе стойки бегут одними и теми же
    /// клипами и различаются только позой покоя в центре. В кадре это не видно вовсе:
    /// оружие живёт на вьюмодели, у физического тела руки всегда пустые.
    /// </para>
    /// <para>
    /// УДАРА В ЭТОМ ГРАФЕ НЕТ, и это не забыли. TPS-взмах заносил телесные руки прямо
    /// в кадр от первого лица - поверх вьюмодельных, которые тот же удар в это время
    /// и отыгрывают. Тело осталось со стойками и бегом; удар живёт там, где он для кадра
    /// и сделан. Плата: тень на земле саблей больше не машет.
    /// </para>
    /// </summary>
    public static class PlayerAnimatorBuilder
    {
        private const string AnimationsFolder = "Assets/_Project/Art/Animations/Player";
        private const string ControllerPath = AnimationsFolder + "/PlayerCombat.controller";

        private const string IdleClipPath = AnimationsFolder + "/HumanM@Idle01.fbx";
        private const string CombatIdleClipPath = AnimationsFolder + "/HumanM@CombatIdle1H01.fbx";

        // Имена параметров дублируются в драйверах: там они хешируются, здесь -
        // объявляются. Третьего места, где они встречаются, быть не должно
        private const string IsArmedParameter = "IsArmed";

        // Скорость в осях тела, в долях от максимальной: X - вбок, Z - вперёд.
        // Двумя числами, а не одним «Speed»: одно число знает, КАК БЫСТРО едет игрок,
        // но не знает, КУДА, - и на нём разворот боком отыгрывался бы бегом вперёд
        private const string MoveXParameter = "MoveX";
        private const string MoveZParameter = "MoveZ";

        /// <summary>Длительность кроссфейда стойки, с. Держать равной DrawDuration оружия</summary>
        private const float StanceCrossfade = 0.25f;

        /// <summary>
        /// Диагональ единичного круга. Диагональные клипы стоят именно на нём, а не
        /// в углах квадрата: дерево смешивает по НАПРАВЛЕНИЮ, и точка (1,1) означала бы
        /// то же направление, но вдвое дальше от центра, чем прямые
        /// </summary>
        private const float Diagonal = 0.70710678f;

        /// <summary>
        /// Клип бега и направление, которому он соответствует. Пара, а не два массива:
        /// разъехавшись на один элемент, они молча поставили бы бег вбок на движение вперёд
        /// </summary>
        private readonly struct RunClip
        {
            public readonly string Path;
            public readonly Vector2 Direction;

            public RunClip(string fileName, float x, float z)
            {
                Path = AnimationsFolder + "/" + fileName + ".fbx";
                Direction = new Vector2(x, z);
            }
        }

        /// <summary>
        /// Круг бега. Вперёд и назад - обычный бег, вбок и по диагонали - строевой:
        /// в обычном теле разворачивается по ходу движения, а тело игрока всегда
        /// развёрнуто по взгляду, и разворот из клипа увёл бы его от направления взгляда
        /// </summary>
        private static readonly RunClip[] Run =
        {
            new RunClip("HumanM@Run01_Forward", 0f, 1f),
            new RunClip("HumanM@Run01_Backward", 0f, -1f),
            new RunClip("HumanM@StrafeRun01_Left", -1f, 0f),
            new RunClip("HumanM@StrafeRun01_Right", 1f, 0f),
            new RunClip("HumanM@StrafeRun01_ForwardLeft", -Diagonal, Diagonal),
            new RunClip("HumanM@StrafeRun01_ForwardRight", Diagonal, Diagonal),
            new RunClip("HumanM@StrafeRun01_BackwardLeft", -Diagonal, -Diagonal),
            new RunClip("HumanM@StrafeRun01_BackwardRight", Diagonal, -Diagonal),
        };

        [MenuItem("Tools/Crate Expectations/Rebuild Player Animator")]
        public static void Rebuild()
        {
            AnimationClip idleClip = LoadClip(IdleClipPath);
            AnimationClip combatIdleClip = LoadClip(CombatIdleClipPath);

            if (idleClip == null || combatIdleClip == null)
                return;

            AnimationClip[] runClips = LoadRunClips();

            if (runClips == null)
                return;

            // Содержимое собирается с нуля, но сам ассет переживает пересборку -
            // см. AnimatorControllerRebuild о том, чем это заканчивается иначе
            AnimatorController controller = AnimatorControllerRebuild.LoadOrCreate(ControllerPath);

            controller.AddParameter(IsArmedParameter, AnimatorControllerParameterType.Bool);

            // Ноль по умолчанию для обоих: граф обязан стартовать в покое, а не
            // на полном ходу в случайную сторону
            AnimatorControllerRebuild.AddFloat(controller, MoveXParameter, 0f);
            AnimatorControllerRebuild.AddFloat(controller, MoveZParameter, 0f);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            root.entryPosition = new Vector3(-260f, 0f, 0f);
            root.anyStatePosition = new Vector3(-260f, 120f, 0f);
            root.exitPosition = new Vector3(420f, 120f, 0f);

            AnimatorState locomotion = root.AddState("Locomotion", new Vector3(-40f, 0f, 0f));
            locomotion.motion = RunTree(controller, "Locomotion", idleClip, runClips);

            AnimatorState combatLocomotion =
                root.AddState("CombatLocomotion", new Vector3(200f, 0f, 0f));
            combatLocomotion.motion = RunTree(controller, "CombatLocomotion", combatIdleClip, runClips);

            root.defaultState = locomotion;

            // Стойка переключается сразу по флагу: ждать конца цикла покоя означало бы
            // задержку до двух с половиной секунд между нажатием и реакцией
            Crossfade(locomotion, combatLocomotion, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.If, 0f, IsArmedParameter);

            Crossfade(combatLocomotion, locomotion, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.IfNot, 0f, IsArmedParameter);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Контроллер игрока пересобран: {ControllerPath}. " +
                      $"Клипов бега в каждой стойке: {runClips.Length}.",
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
        }

        /// <summary>
        /// Дерево локомоции: поза покоя в центре, бег по кругу единичного радиуса.
        /// <para>
        /// Тип - Freeform Directional, а не Simple Directional: у первого центральная
        /// поза - штатная часть раскладки, и переход от покоя к бегу он смешивает
        /// по радиусу, а не только по направлению. Это ровно тот случай, ради которого
        /// он и сделан.
        /// </para>
        /// <para>
        /// Дерево кладётся подобъектом в ассет контроллера - иначе оно не переживёт
        /// перезагрузку, и стейт останется с пустым motion. Молча
        /// </para>
        /// </summary>
        private static BlendTree RunTree(
            AnimatorController controller, string name, AnimationClip standing, AnimationClip[] runClips)
        {
            var tree = new BlendTree
            {
                name = name,
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = MoveXParameter,
                blendParameterY = MoveZParameter,

                // Пороги задаём руками: автоматические разложили бы клипы равномерно
                // по числу детей, и направления перестали бы совпадать с клипами
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };

            tree.AddChild(standing, Vector2.zero);

            for (int i = 0; i < runClips.Length; i++)
                tree.AddChild(runClips[i], Run[i].Direction);

            AssetDatabase.AddObjectToAsset(tree, controller);

            return tree;
        }

        /// <summary>
        /// Клипы бега в том же порядке, в каком объявлены направления. Отсутствие
        /// ЛЮБОГО из них отменяет пересборку целиком: дерево с дырой в круге - это
        /// направление, в котором игрок скользит, не перебирая ногами, и найдётся
        /// такая дыра уже в игре
        /// </summary>
        private static AnimationClip[] LoadRunClips()
        {
            var clips = new AnimationClip[Run.Length];

            for (int i = 0; i < Run.Length; i++)
            {
                clips[i] = LoadClip(Run[i].Path);

                if (clips[i] == null)
                    return null;
            }

            return clips;
        }

        private static AnimatorStateTransition Crossfade(AnimatorState from, AnimatorState to, float seconds)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;

            // Длительность в секундах, а не в долях клипа: она должна совпадать с таймингом
            // из ассета оружия, а тот измерен в секундах
            transition.hasFixedDuration = true;
            transition.duration = seconds;

            return transition;
        }

        /// <summary>
        /// Достаёт клип из FBX. Именно из FBX, а не из отдельного .anim: клип - подассет
        /// импортированной модели, копировать его наружу значило бы завести вторую копию,
        /// которая молча разъедется с настройками импорта
        /// </summary>
        private static AnimationClip LoadClip(string modelPath)
        {
            Object[] contents = AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath);

            foreach (Object asset in contents)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            Debug.LogError($"Не найден клип анимации в '{modelPath}'. " +
                           "Контроллер игрока не пересобран.");
            return null;
        }
    }
}
