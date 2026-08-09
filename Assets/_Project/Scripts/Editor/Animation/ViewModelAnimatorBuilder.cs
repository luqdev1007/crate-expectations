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
    /// Клипы - из пака FPP_SnS (одноручная сабля со щитом). Разведка показала, что
    /// двуручный FPP_Longsword рубящих ударов в кадре не даёт вообще: остриё уходит
    /// за объектив на всей проводке. У SnS одноручный хват, и из 39 его ударов кадр
    /// держат четыре, и три из них - разные приёмы; они здесь и стоят. Боковых среди них
    /// нет: боковой замах в этом
    /// паке заносит клинок за спину, то есть буквально за камеру.
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

        // Клипы FPP-пака: один FBX - один клип. Держать их в отдельных файлах не наш выбор,
        // так пак собран; зато путь к клипу читается без знания имён тейков внутри
        private const string FppFolder = AnimationsFolder + "/FPP";
        private const string IdleModelPath = FppFolder + "/FPP_sns_Idle.fbx";

        /// <summary>Рубящий сверху-справа вниз. Единственный рубящий пака, который держит кадр</summary>
        private const string SlashModelPath = FppFolder + "/FPP_sns_Attack_RD_stop.fbx";

        /// <summary>Укол. Самый быстрый из трёх - ближе всех к <c>AttackDuration</c></summary>
        private const string ThrustModelPath = FppFolder + "/FPP_sns_Attack_F_fast.fbx";

        /// <summary>Тяжёлый укол, хвост цепочки Start -> Loop -> End</summary>
        private const string HeavyModelPath = FppFolder + "/FPP_sns_AttackHeavyF_End.fbx";

        // Имена параметров дублируются в PlayerAnimatorDriver: там они хешируются,
        // здесь - объявляются. Третьего места, где они встречаются, быть не должно
        private const string IsArmedParameter = "IsArmed";
        private const string AttackParameter = "Attack";
        private const string AttackSpeedParameter = "AttackSpeed";

        // TODO: временно. Сейчас значение приходит из поля в инспекторе
        // (PlayerWeaponController.DebugAttackIndex), дальше - из выбора удара по направлению
        private const string AttackIndexParameter = "AttackIndex";

        /// <summary>Длительность кроссфейда стойки, с. Держать равной DrawDuration оружия</summary>
        private const float StanceCrossfade = 0.25f;

        /// <summary>Вход во взмах, с. Короткий: удар должен начинаться по нажатию, а не после него</summary>
        private const float AttackBlendIn = 0.05f;

        /// <summary>Выход из взмаха: доля клипа, после которой начинается возврат в стойку</summary>
        private const float AttackExitTime = 0.85f;

        private const float AttackBlendOut = 0.15f;

        /// <summary>
        /// Один удар: как называется стейт, каким клипом играется, каким значением
        /// <c>AttackIndex</c> выбирается
        /// </summary>
        private struct Attack
        {
            public string State;
            public string ModelPath;
            public int Index;
            public float Y;
        }

        [MenuItem("Tools/Crate Expectations/Rebuild View Model Animator")]
        public static void Rebuild()
        {
            // Порядок задаёт значения AttackIndex: 0 - то, что играется, когда параметр
            // никто не выставил. Рубящий стоит нулевым намеренно - это основной удар.
            //
            // Зеркального Attack_LD здесь нет и не будет, пока сабля живёт в сокете на правой
            // кисти: галка Mirror на гуманоидном стейте меняет местами не кадр, а СТОРОНЫ ТЕЛА -
            // правая кисть начинает играть кривые левой. Замерено: в зеркальном рубящем кисть
            // с оружием уходит за линзу, а через кадр проносится пустая левая. Второе
            // направление - это либо второй сокет с переключением, либо другой клип
            Attack[] attacks =
            {
                new Attack { State = "Attack_RD", ModelPath = SlashModelPath, Index = 0, Y = 140f },
                new Attack { State = "Attack_F", ModelPath = ThrustModelPath, Index = 1, Y = 240f },
                new Attack { State = "Attack_Heavy", ModelPath = HeavyModelPath, Index = 2, Y = 340f },
            };

            AnimationClip idleClip = LoadClip(IdleModelPath);

            if (idleClip == null)
                return;

            var clips = new AnimationClip[attacks.Length];

            for (int i = 0; i < attacks.Length; i++)
            {
                clips[i] = LoadClip(attacks[i].ModelPath);

                if (clips[i] == null)
                    return;
            }

            // Пересобираем с нуля, а не правим существующий: иначе в графе копились бы
            // переходы, которых в этом коде уже нет
            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter(IsArmedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);

            // Множитель скорости стейта взмаха. Единственный способ подчинить длину клипа
            // числу из ассета оружия, не трогая сам клип
            AddFloat(controller, AttackSpeedParameter, 1f);

            // Каким из ударов отвечать на триггер. Числом, а не тремя триггерами:
            // триггеры взводятся независимо, и два взведённых означали бы, что удар
            // выбирает порядок переходов в графе, а не тот, кто нажал
            controller.AddParameter(AttackIndexParameter, AnimatorControllerParameterType.Int);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            root.entryPosition = new Vector3(-260f, 0f, 0f);
            root.anyStatePosition = new Vector3(-260f, 120f, 0f);
            root.exitPosition = new Vector3(420f, 120f, 0f);

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

            for (int i = 0; i < attacks.Length; i++)
                AddAttack(root, combatIdle, attacks[i], clips[i], clips[0]);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Контроллер вьюмодели пересобран: {ControllerPath}. Опорный клип темпа - " +
                      $"'{clips[0].name}' ({clips[0].length:F3} с), он и должен стоять в поле " +
                      "Attack Clip у вьюмодели в PlayerAnimatorDriver.",
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
        }

        /// <summary>
        /// Ставит один удар: стейт с клипом, вход из стойки по паре "триггер + индекс"
        /// и возврат обратно
        /// </summary>
        private static void AddAttack(
            AnimatorStateMachine root,
            AnimatorState stance,
            Attack attack,
            AnimationClip clip,
            AnimationClip reference)
        {
            AnimatorState state = root.AddState(attack.State, new Vector3(200f, attack.Y, 0f));
            state.motion = clip;

            SetSpeed(state, clip, reference);

            AnimatorStateTransition enter = Crossfade(stance, state, AttackBlendIn);
            enter.AddCondition(AnimatorConditionMode.If, 0f, AttackParameter);
            enter.AddCondition(AnimatorConditionMode.Equals, attack.Index, AttackIndexParameter);

            ReturnToStance(state, stance);
        }

        /// <summary>
        /// Темп удара задаёт <c>AttackDuration</c> из ассета оружия, а не длина клипа.
        /// Считает это <c>PlayerAnimatorDriver</c> и кладёт в <c>AttackSpeed</c> - но одно
        /// число на три клипа разной длины уложилось бы в тайминг только для одного
        /// из них. Поэтому параметр остаётся общим множителем, а разницу длин берёт
        /// на себя собственная скорость стейта: она нормирует клип к опорному.
        /// <para>
        /// Опорный - клип первого удара; у него скорость ровно 1, и именно он должен
        /// стоять в поле <c>Attack Clip</c> драйвера. Итог по всем стейтам одинаков:
        /// <c>clipLength / AttackDuration</c>, и правка числа в ассете видна сразу,
        /// без пересборки графа.
        /// </para>
        /// </summary>
        private static void SetSpeed(AnimatorState state, AnimationClip clip, AnimationClip reference)
        {
            state.speedParameterActive = true;
            state.speedParameter = AttackSpeedParameter;
            state.speed = clip.length / reference.length;
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
