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
    /// </summary>
    public static class PlayerAnimatorBuilder
    {
        private const string AnimationsFolder = "Assets/_Project/Art/Animations/Player";
        private const string ControllerPath = AnimationsFolder + "/PlayerCombat.controller";

        private const string IdleClipPath = AnimationsFolder + "/HumanM@Idle01.fbx";
        private const string CombatIdleClipPath = AnimationsFolder + "/HumanM@CombatIdle1H01.fbx";
        private const string AttackClipPath = AnimationsFolder + "/HumanM@Attack1H01_R.fbx";

        // Имена параметров дублируются в PlayerAnimatorDriver: там они хешируются,
        // здесь - объявляются. Третьего места, где они встречаются, быть не должно
        private const string IsArmedParameter = "IsArmed";
        private const string AttackParameter = "Attack";
        private const string SpeedParameter = "Speed";
        private const string AttackSpeedParameter = "AttackSpeed";

        /// <summary>Длительность кроссфейда стойки, с. Держать равной DrawDuration оружия</summary>
        private const float StanceCrossfade = 0.25f;

        /// <summary>Вход во взмах, с. Короткий: удар должен начинаться по нажатию, а не после него</summary>
        private const float AttackBlendIn = 0.05f;

        /// <summary>Выход из взмаха: доля клипа, после которой начинается возврат в стойку</summary>
        private const float AttackExitTime = 0.85f;

        private const float AttackBlendOut = 0.15f;

        [MenuItem("Tools/Crate Expectations/Rebuild Player Animator")]
        public static void Rebuild()
        {
            AnimationClip idleClip = LoadClip(IdleClipPath);
            AnimationClip combatIdleClip = LoadClip(CombatIdleClipPath);
            AnimationClip attackClip = LoadClip(AttackClipPath);

            if (idleClip == null || combatIdleClip == null || attackClip == null)
                return;

            // Пересобираем с нуля, а не правим существующий: иначе в графе копились бы
            // переходы, которых в этом коде уже нет
            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter(IsArmedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);

            // Speed на шаге 1 никто не пишет - это заготовка под локомоцию шага 2.
            // Объявлен сразу, чтобы блендтри ног встал в готовый контроллер, а не потребовал
            // переучивать драйвер
            AddFloat(controller, SpeedParameter, 0f);

            // Множитель скорости стейта Attack. Единственный способ подчинить длину клипа
            // числу из ассета, не трогая сам ассет контроллера в рантайме
            AddFloat(controller, AttackSpeedParameter, 1f);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            root.entryPosition = new Vector3(-260f, 0f, 0f);
            root.anyStatePosition = new Vector3(-260f, 120f, 0f);
            root.exitPosition = new Vector3(420f, 120f, 0f);

            AnimatorState idle = root.AddState("Idle", new Vector3(-40f, 0f, 0f));
            idle.motion = idleClip;

            AnimatorState combatIdle = root.AddState("CombatIdle", new Vector3(200f, 0f, 0f));
            combatIdle.motion = combatIdleClip;

            AnimatorState attack = root.AddState("Attack", new Vector3(200f, 140f, 0f));
            attack.motion = attackClip;
            attack.speedParameterActive = true;
            attack.speedParameter = AttackSpeedParameter;

            root.defaultState = idle;

            // Стойка переключается сразу по флагу: ждать конца цикла idle означало бы
            // задержку до двух с половиной секунд между нажатием и реакцией
            Crossfade(idle, combatIdle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.If, 0f, IsArmedParameter);

            Crossfade(combatIdle, idle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.IfNot, 0f, IsArmedParameter);

            Crossfade(combatIdle, attack, AttackBlendIn)
                .AddCondition(AnimatorConditionMode.If, 0f, AttackParameter);

            AnimatorStateTransition back = attack.AddTransition(combatIdle);
            back.hasExitTime = true;
            back.exitTime = AttackExitTime;
            back.hasFixedDuration = true;
            back.duration = AttackBlendOut;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Контроллер игрока пересобран: {ControllerPath}",
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
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

        private static void AddFloat(AnimatorController controller, string name, float defaultValue)
        {
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = defaultValue,
            });
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
