using System.Collections.Generic;
using CrateExpectations.Combat;
using CrateExpectations.Player.View;
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

        // Блок - три клипа одной позы: поставил, держит, снял. Отдельно от приёмов:
        // это не удар, у него нет ни окна, ни объёма, ни импульса, и в AttackSet
        // ему делать нечего
        private const string BlockRaiseModelPath = AnimationsFolder + "/FPP/FPP_sns_SwordBlockStart.fbx";
        private const string BlockLoopModelPath = AnimationsFolder + "/FPP/FPP_sns_SwordBlockLoop.fbx";
        private const string BlockLowerModelPath = AnimationsFolder + "/FPP/FPP_sns_SwordBlockStop.fbx";

        /// <summary>Чей граф собираем. Приёмы берутся из раскладки этого оружия</summary>
        private const string WeaponPath = "Assets/_Project/Data/Combat/SabreDefinition.asset";

        /// <summary>
        /// Второй ассет-вход билдера: небоевые позы рук. Клипы приходят из него, а не
        /// из путей-констант, как стойка и блок, - те остались по старому и переезжают
        /// отдельным долгом
        /// </summary>
        private const string HandsPosePath = "Assets/_Project/Data/HandsPoseDefinition.asset";

        // Имена параметров дублируются в драйверах: там они хешируются, здесь -
        // объявляются. Третьего места, где они встречаются, быть не должно
        private const string IsArmedParameter = "IsArmed";
        private const string AttackParameter = "Attack";
        private const string AttackSpeedParameter = "AttackSpeed";
        private const string AttackIndexParameter = "AttackIndex";
        private const string BlockPhaseParameter = "BlockPhase";
        private const string EquipPhaseParameter = "EquipPhase";
        private const string EquipSpeedParameter = "EquipSpeed";

        // Эти - от HandsAnimatorDriver, а не от PlayerAnimatorDriver: занятость рук,
        // заряд броска и оба разовых момента переноски приходят не от машины
        // состояний оружия
        private const string HandsModeParameter = "HandsMode";
        private const string ChargeTParameter = "ChargeT";
        private const string GrabParameter = "Grab";
        private const string ThrowParameter = "Throw";
        private const string CarryGrabSpeedParameter = "CarryGrabSpeed";

        /// <summary>
        /// Длительность кроссфейда стойки, с. Остался для случая, когда клипов доставания
        /// у оружия нет: тогда стойка по-прежнему переключается одним кроссфейдом,
        /// и он же читается как «достал»
        /// </summary>
        private const float StanceCrossfade = 0.25f;

        /// <summary>
        /// Вход в доставание/убирание и выход из них, с. Короче кроссфейда стойки: клип
        /// начинается с той же позы, в которой стоит стойка, и размазывать этот стык нечего
        /// </summary>
        private const float EquipBlend = 0.1f;

        /// <summary>Вход во взмах, с. Короткий: удар должен начинаться по нажатию, а не после него</summary>
        private const float AttackBlendIn = 0.05f;

        /// <summary>Выход из взмаха: доля клипа, после которой начинается возврат в стойку</summary>
        private const float AttackExitTime = 0.85f;

        private const float AttackBlendOut = 0.15f;

        /// <summary>Вход и выход из блока, с. Короче кроссфейда стойки: блок ставят резко</summary>
        private const float BlockBlend = 0.08f;

        /// <summary>
        /// Смена позы рук, с. Между кроссфейдом стойки и резкостью блока: взять ящик -
        /// это не выпад, но и не смена стойки
        /// </summary>
        private const float HandsBlend = 0.12f;

        /// <summary>
        /// Вход в выброс, с. Коротко, как у удара: бросок обязан начинаться по отпусканию
        /// кнопки, а не после него
        /// </summary>
        private const float ThrowBlendIn = 0.06f;

        /// <summary>
        /// Доля проводки взятия, после которой руки уходят в удержание. Не единица:
        /// последние кадры проводки - это уже доводка кисти, и досматривать её значит
        /// держать паузу между «взял» и «несу»
        /// </summary>
        private const float GrabExitTime = 0.9f;

        private const float GrabBlendOut = 0.1f;

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
            AnimatorController controller = AnimatorControllerRebuild.LoadOrCreate(ControllerPath);

            controller.AddParameter(IsArmedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);

            // Множитель скорости стейта. Единственный способ подчинить длину клипа
            // длительности приёма, не трогая сам клип
            AddFloat(controller, AttackSpeedParameter, 1f);

            // Каким из приёмов отвечать на триггер. Числом, а не восемью триггерами:
            // триггеры взводятся независимо, и два взведённых означали бы, что удар
            // выбирает порядок переходов в графе, а не тот, кто нажал
            controller.AddParameter(AttackIndexParameter, AnimatorControllerParameterType.Int);

            // Фаза блока числом, а не тремя булями: фазы взаимоисключающие, и два
            // поднятых флага означали бы, что позу выбирает порядок переходов в графе
            controller.AddParameter(BlockPhaseParameter, AnimatorControllerParameterType.Int);

            // Фаза доставания/убирания - тем же приёмом и по той же причине
            controller.AddParameter(EquipPhaseParameter, AnimatorControllerParameterType.Int);

            // Множитель скорости доставания и убирания. Единица по умолчанию: клип,
            // которому забыли назначить скорость, должен идти как есть, а не стоять
            AddFloat(controller, EquipSpeedParameter, 1f);

            // Занятость рук - тоже числом и по той же причине: занятость одна за раз,
            // и числа для неё объявлены в HandsAnimatorMode, откуда их берёт и рантайм
            controller.AddParameter(HandsModeParameter, AnimatorControllerParameterType.Int);

            // Заряд броска. Ноль по умолчанию: незаряженная поза - это поза покоя,
            // и стартовать граф обязан именно с неё
            AddFloat(controller, ChargeTParameter, 0f);

            // Взятие и выброс - разовые МОМЕНТЫ, а не состояния, поэтому триггеры.
            // Занятость рук во время броска не меняется вовсе (её держит хвост
            // переноски), и вычислить из неё момент выброса нельзя в принципе
            controller.AddParameter(GrabParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(ThrowParameter, AnimatorControllerParameterType.Trigger);

            // Множитель скорости проводки взятия. Единица по умолчанию: клип, которому
            // забыли назначить скорость, должен идти как есть, а не стоять на месте
            AddFloat(controller, CarryGrabSpeedParameter, 1f);

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
            // задержку до четырёх с лишним секунд между нажатием и реакцией.
            //
            // Условие по фазе - обязательное, и вот почему. IsArmed поднимается уже
            // в Drawing, то есть в тот же кадр, в который стартует стейт доставания.
            // Без него кроссфейд стойки и вход в Draw сработали бы одновременно и
            // подрались бы за одну и ту же модель. Пока идёт переход, стойки молчат
            AnimatorStateTransition draw = Crossfade(idle, combatIdle, StanceCrossfade);
            draw.AddCondition(AnimatorConditionMode.If, 0f, IsArmedParameter);
            draw.AddCondition(AnimatorConditionMode.Equals, EquipAnimatorPhase.None, EquipPhaseParameter);

            AnimatorStateTransition sheathe = Crossfade(combatIdle, idle, StanceCrossfade);
            sheathe.AddCondition(AnimatorConditionMode.IfNot, 0f, IsArmedParameter);
            sheathe.AddCondition(AnimatorConditionMode.Equals, EquipAnimatorPhase.None, EquipPhaseParameter);

            int built = 0;

            for (int i = 0; i < attacks.Count; i++)
                if (AddAttack(root, combatIdle, attacks[i], i))
                    built++;

            AddBlock(root, combatIdle);

            bool equipBuilt = AddEquip(root, weapon, idle, combatIdle);

            bool handsBuilt = AddHandsPoses(controller, root, idle, combatIdle);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Контроллер вьюмодели пересобран: {ControllerPath}. Приёмов в графе: {built} " +
                      $"из {attacks.Count} (раскладка '{weapon.Attacks.name}'). " +
                      $"Небоевые позы рук: {(handsBuilt ? "собраны" : "НЕ собраны")}. " +
                      $"Доставание и убирание: {(equipBuilt ? "собраны" : "НЕ собраны, стойка переключается кроссфейдом")}.",
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath));
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
        /// Ставит блок: постановка - удержание - снятие.
        /// <para>
        /// Вход в постановку идёт из Any State по той же причине, что и у ударов: блок
        /// можно поставить не только из стойки, а перечислять источники переходами значило бы
        /// держать в графе связь от каждого состояния. Дальше цепочка идёт по фазе, которую
        /// пишет драйвер: сам граф ничего не решает и время не считает - фазы переключает
        /// машина состояний, она же владелец таймингов.
        /// </para>
        /// </summary>
        private static void AddBlock(AnimatorStateMachine root, AnimatorState stance)
        {
            AnimationClip raiseClip = LoadClip(BlockRaiseModelPath);
            AnimationClip loopClip = LoadClip(BlockLoopModelPath);
            AnimationClip lowerClip = LoadClip(BlockLowerModelPath);

            if (raiseClip == null || loopClip == null || lowerClip == null)
                return;

            AnimatorState raise = root.AddState("BlockRaise", new Vector3(-40f, 140f, 0f));
            raise.motion = raiseClip;

            AnimatorState hold = root.AddState("Blocking", new Vector3(-40f, 210f, 0f));
            hold.motion = loopClip;

            AnimatorState lower = root.AddState("BlockLower", new Vector3(-40f, 280f, 0f));
            lower.motion = lowerClip;

            Phase(root.AddAnyStateTransition(raise), 1);

            // Между фазами переходим по параметру, а не по exit time: клип постановки
            // и число в ассете оружия могут разойтись, и хозяином тайминга должна остаться
            // машина состояний, иначе поза и логика разъедутся
            Phase(raise.AddTransition(hold), 2);
            Phase(raise.AddTransition(lower), 3);
            Phase(hold.AddTransition(lower), 3);

            AnimatorStateTransition back = lower.AddTransition(stance);
            back.hasExitTime = false;
            back.hasFixedDuration = true;
            back.duration = BlockBlend;
            back.AddCondition(AnimatorConditionMode.Equals, 0, BlockPhaseParameter);
        }

        /// <summary>
        /// Ставит доставание и убирание: два стейта между стойками.
        /// <para>
        /// Вход и выход описываются ОДНИМ параметром, и это принципиально отличается
        /// от переноски, где вход пришлось увести на триггер. Там занятость <c>Carrying</c>
        /// истинна во всех трёх фазах сразу, и переход из Any State по ней бил бы
        /// из соседних стейтов. Здесь такой петли нет: пока <c>EquipPhase</c> равна
        /// единице, мы находимся ровно в <c>Draw</c> и больше нигде - ни ударить,
        /// ни поставить блок, ни взять груз в это время нельзя, это гарантирует
        /// <c>IsBusy</c>. Стала нулём - вышли, и вернуться уже некуда: условие входа
        /// перестало быть истинным. Условие по Int из Any State здесь корректно.
        /// </para>
        /// <para>
        /// Клипы приходят из ассета оружия, а не из путей-констант: рядом с ними лежат
        /// длительности обеих фаз, и разъехаться этим двум числам негде. Оружие без
        /// клипов - не ошибка, а прежнее поведение: стойка переключается кроссфейдом,
        /// и он же читается как «достал».
        /// </para>
        /// </summary>
        private static bool AddEquip(
            AnimatorStateMachine root, WeaponDefinition weapon,
            AnimatorState idle, AnimatorState combatIdle)
        {
            if (weapon.DrawClip == null || weapon.SheatheClip == null)
                return false;

            AnimatorState draw = root.AddState("Draw", new Vector3(-40f, -140f, 0f));
            draw.motion = weapon.DrawClip;
            draw.speedParameterActive = true;
            draw.speedParameter = EquipSpeedParameter;

            AnimatorState sheathe = root.AddState("Sheathe", new Vector3(200f, -140f, 0f));
            sheathe.motion = weapon.SheatheClip;
            sheathe.speedParameterActive = true;
            sheathe.speedParameter = EquipSpeedParameter;

            EquipPhase(root.AddAnyStateTransition(draw), EquipAnimatorPhase.Drawing);
            EquipPhase(root.AddAnyStateTransition(sheathe), EquipAnimatorPhase.Sheathing);

            // Выход - по той же фазе, что и вход. Не по времени клипа: хозяин тайминга -
            // машина состояний, и клип обязан уложиться в её длительность, а не наоборот
            EquipPhase(draw.AddTransition(combatIdle), EquipAnimatorPhase.None);
            EquipPhase(sheathe.AddTransition(idle), EquipAnimatorPhase.None);

            return true;
        }

        private static void EquipPhase(AnimatorStateTransition transition, int phase)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = EquipBlend;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, phase, EquipPhaseParameter);
        }

        /// <summary>
        /// Ставит небоевые позы рук: переноску и листок заказа.
        /// <para>
        /// Вход - из Any State по занятости, как у блока: занятость меняется откуда угодно
        /// (груз может выпасть сам, листок опуститься по событию шины), и перечислять
        /// источники переходами значило бы держать связь от каждого состояния графа.
        /// А вот ВЫХОД - явными переходами из самих поз, и это не симметрия ради симметрии:
        /// переход Any State → Idle по "руки свободны" дрался бы с парой Idle ↔ CombatIdle,
        /// которая живёт по IsArmed, и стойка дёргалась бы между ними.
        /// </para>
        /// <para>
        /// Переход в себя у входов ЗАПРЕЩЁН. Условие здесь - равенство числа, истинное
        /// каждый кадр, пока руки заняты; с разрешённым переходом в себя стейт
        /// перезапускался бы вечно и анимация стояла бы на первом кадре. У ударов
        /// переход в себя разрешён, и это не противоречие: там вход по триггеру,
        /// который гаснет сам.
        /// </para>
        /// <para>
        /// А вот переноска этим правилом уже НЕ описывается, и вход в неё по занятости
        /// пришлось убрать. Причина: у переноски теперь три стейта - проводка взятия,
        /// удержание и выброс, - и занятость <c>Carrying</c> истинна во всех трёх.
        /// Переход из Any State по этому числу срабатывал бы и из удержания, и из
        /// выброса, то есть проводка взятия перезапускалась бы вечно (запрет перехода
        /// в себя ловит только сам стейт, но не соседей). Поэтому вход в проводку - по
        /// триггеру, а дальше цепочка идёт своими переходами: проводка → удержание
        /// по времени клипа, удержание → выброс по второму триггеру. Занятость осталась
        /// только на ВЫХОДЕ, где она однозначна.
        /// </para>
        /// </summary>
        private static bool AddHandsPoses(
            AnimatorController controller, AnimatorStateMachine root,
            AnimatorState idle, AnimatorState combatIdle)
        {
            var poses = AssetDatabase.LoadAssetAtPath<HandsPoseDefinition>(HandsPosePath);

            if (poses == null)
            {
                Debug.LogError($"Нет ассета небоевых поз рук: '{HandsPosePath}'. " +
                               "Стейтов переноски и листка в графе не будет.");
                return false;
            }

            if (!poses.IsComplete)
            {
                Debug.LogError($"В '{poses.name}' назначены не все клипы. " +
                               "Стейтов переноски и листка в графе не будет.", poses);
                return false;
            }

            AnimatorState grab = root.AddState("Carry_Grab", new Vector3(680f, -140f, 0f));
            grab.motion = poses.CarryGrab;
            grab.speedParameterActive = true;
            grab.speedParameter = CarryGrabSpeedParameter;

            AnimatorState carry = root.AddState("Carry", new Vector3(440f, 0f, 0f));
            carry.motion = CarryBlendTree(controller, poses);

            AnimatorState throwState = root.AddState("Carry_Throw", new Vector3(680f, 0f, 0f));
            throwState.motion = poses.CarryThrow;

            AnimatorState contract = root.AddState("ContractHold", new Vector3(440f, 140f, 0f));
            contract.motion = poses.ContractHold;

            // Вход в переноску - через проводку взятия, по триггеру. Прямого входа
            // в удержание нет намеренно: он перебивал бы проводку в тот же кадр,
            // потому что занятость Carrying истинна уже на её первом кадре
            ByTrigger(root.AddAnyStateTransition(grab), GrabParameter, HandsBlend);

            // Проводка отыграла - руки переходят в удержание. По времени клипа, а не по
            // занятости: занятость всё это время не менялась и сообщить об окончании
            // проводки не может
            AnimatorStateTransition settled = grab.AddTransition(carry);
            settled.hasExitTime = true;
            settled.exitTime = GrabExitTime;
            settled.hasFixedDuration = true;
            settled.duration = GrabBlendOut;

            // Выброс - из Any State, а не из удержания: бросок можно начать и посреди
            // проводки взятия, если игрок зажал кнопку сразу же
            ByTrigger(root.AddAnyStateTransition(throwState), ThrowParameter, ThrowBlendIn);

            ByHandsMode(root.AddAnyStateTransition(contract), HandsAnimatorMode.Reading);

            // Возврат в ту стойку, которая соответствует состоянию оружия. Обе ветки нужны:
            // руки освобождаются и в безоружную стойку, и в боевую - смотря что игрок
            // держал до груза.
            // Выход у всех трёх стейтов переноски одинаковый: занятость на выходе
            // однозначна, и груз может пропасть из рук в любой из фаз - выпасть
            // по BreakDistance прямо посреди проводки в том числе
            ByHandsMode(grab.AddTransition(idle), HandsAnimatorMode.Free);
            ByHandsMode(grab.AddTransition(combatIdle), HandsAnimatorMode.Combat);
            ByHandsMode(carry.AddTransition(idle), HandsAnimatorMode.Free);
            ByHandsMode(carry.AddTransition(combatIdle), HandsAnimatorMode.Combat);
            ByHandsMode(throwState.AddTransition(idle), HandsAnimatorMode.Free);
            ByHandsMode(throwState.AddTransition(combatIdle), HandsAnimatorMode.Combat);
            ByHandsMode(contract.AddTransition(idle), HandsAnimatorMode.Free);
            ByHandsMode(contract.AddTransition(combatIdle), HandsAnimatorMode.Combat);

            return true;
        }

        /// <summary>
        /// Вход по разовому триггеру. Переход в себя РАЗРЕШЁН, в отличие от входов
        /// по занятости: триггер гаснет сам, вечного перезапуска он не даёт, а два
        /// броска подряд обязаны начинаться каждый с начала
        /// </summary>
        private static void ByTrigger(AnimatorStateTransition transition, string parameter, float blend)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = blend;
            transition.canTransitionToSelf = true;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        /// <summary>
        /// Переноска - это не одна поза, а отрезок между покоем и концом замаха, поэтому
        /// одномерное дерево по заряду: игрок должен видеть, СКОЛЬКО он накопил, а не
        /// «копит или нет».
        /// <para>
        /// Дерево кладётся подобъектом в ассет контроллера - иначе оно не переживёт
        /// перезагрузку, и стейт останется с пустым motion. Молча
        /// </para>
        /// </summary>
        private static BlendTree CarryBlendTree(AnimatorController controller, HandsPoseDefinition poses)
        {
            var tree = new BlendTree
            {
                name = "CarryCharge",
                blendType = BlendTreeType.Simple1D,
                blendParameter = ChargeTParameter,

                // Пороги задаём руками: автоматические разложили бы клипы равномерно
                // по числу детей, и края дерева перестали бы совпадать с краями заряда
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };

            tree.AddChild(poses.CarryIdle, 0f);
            tree.AddChild(poses.CarryWindup, 1f);

            AssetDatabase.AddObjectToAsset(tree, controller);

            return tree;
        }

        private static void ByHandsMode(AnimatorStateTransition transition, int mode)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = HandsBlend;

            // Ловушка, ради которой этот хелпер и существует: условие истинно каждый кадр
            transition.canTransitionToSelf = false;

            transition.AddCondition(AnimatorConditionMode.Equals, mode, HandsModeParameter);
        }

        private static void Phase(AnimatorStateTransition transition, int phase)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = BlockBlend;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, phase, BlockPhaseParameter);
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

        private static void AddFloat(AnimatorController controller, string name, float defaultValue) =>
            AnimatorControllerRebuild.AddFloat(controller, name, defaultValue);

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
