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
    /// Контроллер отдельный от тела, потому что клипы авторены под разные задачи. У тела -
    /// TPS-пак, снятый с камеры за спиной; у вьюмодели - FPP-пак, снятый из глаза. Скелет
    /// теперь общий (оба гуманоидные), но подставить клип из одного пака в другой всё равно
    /// нельзя: композиция кадра в FPP-клипе зашита в саму анимацию. Одинаковым остаётся
    /// только набор параметров - их раздаёт <c>PlayerAnimatorDriver</c>, и знать про два
    /// разных графа он не должен.
    /// </para>
    /// <para>
    /// Стойка в кадре одна на оба состояния: отдельной безоружной стойки от первого лица
    /// в паке нет, а руки без сабли всё равно спрятаны (<c>ViewModelBody</c>).
    /// Стейт <c>Idle</c> существует ради кроссфейда доставания - именно переход между
    /// стойками и читается как "достал" / "убрал".
    /// </para>
    /// <para>
    /// Взмах один. Чередование двух ударов (<c>SlashAlternate</c>) сюда не переехало: в паке
    /// есть и второй замах, <c>FPP_Longs_Attack_R</c>, но пока он в граф не поставлен, а
    /// параметр без второго стейта переключал бы удар сам в себя. Драйвер отсутствие
    /// параметра переживает - он спрашивает контроллер, есть ли такой, и молчит, если нет.
    /// </para>
    /// </summary>
    public static class ViewModelAnimatorBuilder
    {
        private const string AnimationsFolder = "Assets/_Project/Art/Animations/ViewModel";
        private const string ControllerPath = AnimationsFolder + "/ViewModelCombat.controller";

        // Клипы FPP-пака: один FBX - один клип. Держать их в отдельных файлах не наш выбор,
        // так пак собран; зато путь к клипу читается без знания имён тейков внутри
        private const string FppFolder = AnimationsFolder + "/FPP";
        private const string IdleModelPath = FppFolder + "/FPP_Longs_Idle.fbx";
        private const string AttackModelPath = FppFolder + "/FPP_Longs_Attack_D.fbx";

        // Имена параметров дублируются в PlayerAnimatorDriver: там они хешируются,
        // здесь - объявляются. Третьего места, где они встречаются, быть не должно
        private const string IsArmedParameter = "IsArmed";
        private const string AttackParameter = "Attack";
        private const string AttackSpeedParameter = "AttackSpeed";

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
            AnimationClip idleClip = LoadClip(IdleModelPath);
            AnimationClip attackClip = LoadClip(AttackModelPath);

            if (idleClip == null || attackClip == null)
                return;

            // Пересобираем с нуля, а не правим существующий: иначе в графе копились бы
            // переходы, которых в этом коде уже нет
            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter(IsArmedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);

            // Множитель скорости стейта взмаха. Единственный способ подчинить длину клипа
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

            AnimatorState attack = root.AddState("Attack", new Vector3(200f, 140f, 0f));
            attack.motion = attackClip;
            attack.speedParameterActive = true;
            attack.speedParameter = AttackSpeedParameter;

            root.defaultState = idle;

            // Стойка переключается сразу по флагу: ждать конца цикла idle означало бы
            // задержку до четырёх с лишним секунд между нажатием и реакцией
            Crossfade(idle, combatIdle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.If, 0f, IsArmedParameter);

            Crossfade(combatIdle, idle, StanceCrossfade)
                .AddCondition(AnimatorConditionMode.IfNot, 0f, IsArmedParameter);

            Crossfade(combatIdle, attack, AttackBlendIn)
                .AddCondition(AnimatorConditionMode.If, 0f, AttackParameter);

            ReturnToStance(attack, combatIdle);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Контроллер вьюмодели пересобран: {ControllerPath}",
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
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
        /// Достаёт единственный клип из FBX. Искать по имени тейка, как раньше, больше незачем:
        /// в FPP-паке на файл приходится ровно одна анимация, и её имя повторяет имя файла
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
