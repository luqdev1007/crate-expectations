using System.Collections.Generic;
using CrateExpectations.Combat;
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
    /// Список приёмов билдер НЕ ЗНАЕТ: он берёт его из <see cref="AttackSet"/> того оружия,
    /// которому граф принадлежит. Добавить удар - это добавить ассет в раскладку, а не
    /// править этот файл; номер стейта при этом совпадает с номером приёма в наборе,
    /// потому что берётся оттуда же, откуда его берёт рантайм.
    /// </para>
    /// <para>
    /// Клипы - из пака FPP_SnS (одноручная сабля со щитом). Двуручный FPP_Longsword рубящих
    /// в кадре не давал вообще: остриё уходило за объектив на всей проводке.
    /// </para>
    /// <para>
    /// Стойка в кадре одна на оба состояния: отдельной безоружной стойки от первого лица
    /// в паке нет, а руки без сабли всё равно спрятаны (<c>ViewModelBody</c>).
    /// Стейт <c>Idle</c> существует ради кроссфейда доставания - именно переход между
    /// стойками и читается как "достал" / "убрал".
    /// </para>
    /// </summary>
    public static class ViewModelAnimatorBuilder
    {
        private const string AnimationsFolder = "Assets/_Project/Art/Animations/ViewModel";
        private const string ControllerPath = AnimationsFolder + "/ViewModelCombat.controller";
        private const string IdleModelPath = AnimationsFolder + "/FPP/FPP_sns_Idle.fbx";

        /// <summary>Чей граф собираем. Приёмы берутся из раскладки этого оружия</summary>
        private const string WeaponPath = "Assets/_Project/Data/Combat/SabreDefinition.asset";

        // Имена параметров дублируются в PlayerAnimatorDriver: там они хешируются,
        // здесь - объявляются. Третьего места, где они встречаются, быть не должно
        private const string IsArmedParameter = "IsArmed";
        private const string AttackParameter = "Attack";
        private const string AttackSpeedParameter = "AttackSpeed";
        private const string AttackIndexParameter = "AttackIndex";

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
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WeaponPath);

            if (weapon == null || weapon.Attacks == null)
            {
                Debug.LogError($"В '{WeaponPath}' нет оружия или у него не назначена раскладка приёмов. " +
                               "Контроллер вьюмодели не пересобран.");
                return;
            }

            IReadOnlyList<AttackDefinition> attacks = weapon.Attacks.All;

            if (attacks.Count == 0)
            {
                Debug.LogError($"Раскладка '{weapon.Attacks.name}' пуста - собирать нечего.");
                return;
            }

            AnimationClip idleClip = LoadClip(IdleModelPath);

            if (idleClip == null)
                return;

            // Содержимое собирается с нуля - иначе в графе копились бы переходы, которых
            // в этом коде уже нет, - но САМ АССЕТ переживает пересборку. Удалять и создавать
            // заново нельзя: у нового файла новый GUID, и аниматор в сцене после этого
            // остаётся с пустым контроллером. Молча: ошибок нет, просто вьюмодель перестаёт
            // шевелиться, и ищется это долго
            AnimatorController controller = LoadOrCreate();

            controller.AddParameter(IsArmedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);

            // Множитель скорости стейта. Единственный способ подчинить длину клипа
            // длительности приёма, не трогая сам клип
            AddFloat(controller, AttackSpeedParameter, 1f);

            // Каким из приёмов отвечать на триггер. Числом, а не восемью триггерами:
            // триггеры взводятся независимо, и два взведённых означали бы, что удар
            // выбирает порядок переходов в графе, а не тот, кто нажал
            controller.AddParameter(AttackIndexParameter, AnimatorControllerParameterType.Int);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            root.entryPosition = new Vector3(-260f, 0f, 0f);
            root.anyStatePosition = new Vector3(-260f, 120f, 0f);
            root.exitPosition = new Vector3(-260f, 240f, 0f);

            AnimatorState idle = root.AddState("Idle", new Vector3(-40f, 0f, 0f));
            idle.motion = idleClip;

            AnimatorState combatIdle = root.AddState("CombatIdle", new Vector3(200f, 0f, 0f));
            combatIdle.motion = idleClip;

            root.defaultState = idle;

            // Стойка переключается сразу по флагу: ждать конца цикла idle означало бы
            // задержку до четырёх с лишним секунд между нажатием и реакцией
            Crossfade(idle, combatIdle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.If, 0f, IsArmedParameter);

            Crossfade(combatIdle, idle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.IfNot, 0f, IsArmedParameter);

            int built = 0;

            for (int i = 0; i < attacks.Count; i++)
                if (AddAttack(root, combatIdle, attacks[i], i))
                    built++;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Контроллер вьюмодели пересобран: {ControllerPath}. Приёмов в графе: {built} " +
                      $"из {attacks.Count} (раскладка '{weapon.Attacks.name}').",
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
        }

        /// <summary>
        /// Отдаёт контроллер по постоянному пути, вычищенный до пустого. Существующий -
        /// опустошённый, но тот же самый: ссылки на него из сцен и префабов должны пережить
        /// пересборку
        /// </summary>
        private static AnimatorController LoadOrCreate()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
                return AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            while (controller.parameters.Length > 0)
                controller.RemoveParameter(0);

            AnimatorStateMachine root = controller.layers[0].stateMachine;

            // Переходы из Any State живут на самой машине, а не на стейтах, и удалением
            // стейтов не подчищаются
            root.anyStateTransitions = new AnimatorStateTransition[0];

            foreach (ChildAnimatorState state in root.states)
                root.RemoveState(state.state);

            return controller;
        }

        /// <summary>
        /// Ставит один приём: стейт с его клипом, вход по паре "триггер + номер" и возврат
        /// в стойку.
        /// <para>
        /// Вход идёт ИЗ ANY STATE, а не из стойки, и это не лень. Удар в этой схеме можно
        /// начать не только из стойки, но и посреди предыдущего удара - в его окне отмены.
        /// Переходами из стойки такое не описать: пришлось бы соединять каждый приём
        /// с каждым, то есть держать в графе N*N переходов, которые все делают одно и то же.
        /// Переход в себя разрешён намеренно: два одинаковых удара подряд - это норма,
        /// и второй обязан начаться с начала, а не быть проглоченным.
        /// </para>
        /// </summary>
        private static bool AddAttack(
            AnimatorStateMachine root, AnimatorState stance, AttackDefinition attack, int index)
        {
            if (attack.Clip == null)
            {
                Debug.LogWarning($"У приёма '{attack.name}' не назначен клип - стейта в графе не будет.", attack);
                return false;
            }

            AnimatorState state = root.AddState(attack.name, new Vector3(200f, 140f + index * 70f, 0f));
            state.motion = attack.Clip;
            state.speedParameterActive = true;
            state.speedParameter = AttackSpeedParameter;

            AnimatorStateTransition enter = root.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.hasFixedDuration = true;
            enter.duration = AttackBlendIn;
            enter.canTransitionToSelf = true;
            enter.AddCondition(AnimatorConditionMode.If, 0f, AttackParameter);
            enter.AddCondition(AnimatorConditionMode.Equals, index, AttackIndexParameter);

            ReturnToStance(state, stance);

            return true;
        }

        /// <summary>
        /// Возврат в стойку начинается до конца клипа: последние кадры - это уже доводка
        /// руки, и досматривать её значит держать игрока в позе, из которой он не может
        /// ударить снова
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
        /// Достаёт единственный клип из FBX. В FPP-паке на файл приходится ровно одна
        /// анимация, и её имя повторяет имя файла
        /// </summary>
        private static AnimationClip LoadClip(string modelPath)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath))
            {
                if (asset is AnimationClip clip)
                    return clip;
            }

            Debug.LogError($"В '{modelPath}' нет анимации. Контроллер вьюмодели не пересобран.");
            return null;
        }
    }
}
