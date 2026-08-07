using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CrateExpectations.EditorTools.Animation
{
    /// <summary>
    /// Собирает контроллер вьюмодели с нуля - тем же способом и по тем же причинам, что и
    /// <see cref="PlayerAnimatorBuilder"/>: граф руками не кликается, пересборка воспроизводима,
    /// изменения структуры читаются в диффе как код.
    /// <para>
    /// Контроллер отдельный от тела, потому что скелеты разные. У тела - полный гуманоид
    /// JC с TPS-клипами; у вьюмодели - Generic-риг из одних рук, и его клипы авторены сразу
    /// под первое лицо. Общий контроллер тут невозможен физически: клип с одного рига на
    /// другой не ложится. Одинаковым остаётся только набор параметров - их раздаёт
    /// <c>PlayerAnimatorDriver</c>, и знать про два разных графа он не должен.
    /// </para>
    /// <para>
    /// Стойка в кадре одна на оба состояния: отдельной безоружной стойки от первого лица
    /// в исходном паке нет, а руки без сабли всё равно спрятаны (<c>ViewModelBody</c>).
    /// Стейт <c>Idle</c> существует ради кроссфейда доставания - именно переход между
    /// стойками и читается как "достал" / "убрал".
    /// </para>
    /// </summary>
    public static class ViewModelAnimatorBuilder
    {
        private const string AnimationsFolder = "Assets/_Project/Art/Animations/ViewModel";
        private const string ControllerPath = AnimationsFolder + "/ViewModelCombat.controller";
        private const string ArmsModelPath = AnimationsFolder + "/Arms.fbx";

        // Имена тейков внутри FBX. Префикс - имя арматуры в Blender, менять его переименованием
        // клипа при импорте не стали: так имя в проекте совпадает с именем в исходнике,
        // и найти клип в чужом паке можно поиском по одной строке
        private const string IdleClipName = "CharacterArmature|Sword_Idle";
        private const string FirstSlashClipName = "CharacterArmature|Sword_Slash1";
        private const string SecondSlashClipName = "CharacterArmature|Sword_Slash2";

        // Имена параметров дублируются в PlayerAnimatorDriver: там они хешируются,
        // здесь - объявляются. Третьего места, где они встречаются, быть не должно
        private const string IsArmedParameter = "IsArmed";
        private const string AttackParameter = "Attack";
        private const string AttackSpeedParameter = "AttackSpeed";
        private const string SlashAlternateParameter = "SlashAlternate";

        /// <summary>Длительность кроссфейда стойки, с. Держать равной DrawDuration оружия</summary>
        private const float StanceCrossfade = 0.25f;

        /// <summary>Вход во взмах, с. Короткий: удар должен начинаться по нажатию, а не после него</summary>
        private const float AttackBlendIn = 0.05f;

        /// <summary>Выход из взмаха: доля клипа, после которой начинается возврат в стойку</summary>
        private const float AttackExitTime = 0.85f;

        private const float AttackBlendOut = 0.15f;

        [MenuItem("Tools/Crate Expectations/Rebuild View Model Animator")]
        public static void Rebuild()
        {
            AnimationClip idleClip = LoadClip(ArmsModelPath, IdleClipName);
            AnimationClip firstSlashClip = LoadClip(ArmsModelPath, FirstSlashClipName);
            AnimationClip secondSlashClip = LoadClip(ArmsModelPath, SecondSlashClipName);

            if (idleClip == null || firstSlashClip == null || secondSlashClip == null)
                return;

            // Пересобираем с нуля, а не правим существующий: иначе в графе копились бы
            // переходы, которых в этом коде уже нет
            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter(IsArmedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);

            // Какой из двух клипов взмаха играть. Переключает драйвер перед тем, как дёрнуть
            // триггер: чередование - это решение о том, чем отыграть удар, и принимать его
            // должен тот, кто про удар узнаёт, а не граф. Сам граф чередовать не умеет -
            // два перехода по одному триггеру он разрешает всегда в пользу первого
            AddBool(controller, SlashAlternateParameter, false);

            // Множитель скорости стейтов взмаха. Единственный способ подчинить длину клипа
            // числу из ассета оружия, не трогая сам клип
            AddFloat(controller, AttackSpeedParameter, 1f);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            root.entryPosition = new Vector3(-260f, 0f, 0f);
            root.anyStatePosition = new Vector3(-260f, 120f, 0f);
            root.exitPosition = new Vector3(420f, 120f, 0f);

            AnimatorState idle = root.AddState("Idle", new Vector3(-40f, 0f, 0f));
            idle.motion = idleClip;

            AnimatorState combatIdle = root.AddState("CombatIdle", new Vector3(200f, 0f, 0f));
            combatIdle.motion = idleClip;

            AnimatorState firstSlash = AddSlashState(root, "Attack1", firstSlashClip, new Vector3(200f, 140f, 0f));
            AnimatorState secondSlash = AddSlashState(root, "Attack2", secondSlashClip, new Vector3(200f, 240f, 0f));

            root.defaultState = idle;

            // Стойка переключается сразу по флагу: ждать конца цикла idle означало бы
            // задержку до трёх с лишним секунд между нажатием и реакцией
            Crossfade(idle, combatIdle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.If, 0f, IsArmedParameter);

            Crossfade(combatIdle, idle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.IfNot, 0f, IsArmedParameter);

            // Оба перехода висят на одном триггере и разведены только флагом. Триггер
            // потребит тот из них, чьё условие сошлось, - второй не выстрелит
            AnimatorStateTransition toFirst = Crossfade(combatIdle, firstSlash, AttackBlendIn);
            toFirst.AddCondition(AnimatorConditionMode.If, 0f, AttackParameter);
            toFirst.AddCondition(AnimatorConditionMode.IfNot, 0f, SlashAlternateParameter);

            AnimatorStateTransition toSecond = Crossfade(combatIdle, secondSlash, AttackBlendIn);
            toSecond.AddCondition(AnimatorConditionMode.If, 0f, AttackParameter);
            toSecond.AddCondition(AnimatorConditionMode.If, 0f, SlashAlternateParameter);

            ReturnToStance(firstSlash, combatIdle);
            ReturnToStance(secondSlash, combatIdle);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Контроллер вьюмодели пересобран: {ControllerPath}",
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
        }

        private static AnimatorState AddSlashState(
            AnimatorStateMachine root, string name, AnimationClip clip, Vector3 position)
        {
            AnimatorState state = root.AddState(name, position);
            state.motion = clip;
            state.speedParameterActive = true;
            state.speedParameter = AttackSpeedParameter;
            return state;
        }

        /// <summary>
        /// Возврат в стойку начинается до конца клипа: последние кадры замаха - это уже
        /// доводка руки, и досматривать её значит держать игрока в позе, из которой он
        /// не может ударить снова
        /// </summary>
        private static void ReturnToStance(AnimatorState from, AnimatorState stance)
        {
            AnimatorStateTransition back = from.AddTransition(stance);
            back.hasExitTime = true;
            back.exitTime = AttackExitTime;
            back.hasFixedDuration = true;
            back.duration = AttackBlendOut;
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

        private static void AddBool(AnimatorController controller, string name, bool defaultValue)
        {
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = defaultValue,
            });
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
        /// Достаёт клип из FBX по имени тейка. Клипов в этом файле девять, поэтому "первый
        /// попавшийся", как в контроллере тела, здесь не годится - нужен именно названный
        /// </summary>
        private static AnimationClip LoadClip(string modelPath, string clipName)
        {
            Object[] contents = AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath);

            foreach (Object asset in contents)
            {
                if (asset is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            Debug.LogError($"В '{modelPath}' нет клипа '{clipName}'. " +
                           "Контроллер вьюмодели не пересобран.");
            return null;
        }
    }
}
