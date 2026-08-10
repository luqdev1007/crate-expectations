using System;
using CrateExpectations.Combat;
using CrateExpectations.Core.Hands;
using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Player.Combat
{
    /// <summary>
    /// Тонкий слой между вводом и <see cref="WeaponStateMachine"/>: нажатия переводит
    /// в команды, а решение о видимости оружия - в сокет. Сама логика состояний, выбора
    /// приёма и памяти на нажатие живёт в обычных C#-классах и про
    /// <see cref="MonoBehaviour"/> ничего не знает.
    /// <para>
    /// Направление удара снимается с НАЖАТЫХ КЛАВИШ в момент нажатия и держится до конца
    /// приёма. Не со скорости <c>Rigidbody</c>: в ней живёт инерция после отпускания,
    /// она равна нулю при упоре в стену, когда игрок вовсю жмёт вперёд, и в неё
    /// подмешивается гравитация. Клавиши - это намерение, а бой должен отвечать
    /// намерению.
    /// </para>
    /// <para>
    /// Нажатие уходит в удар не всегда сразу. Если у направления есть заряженный приём
    /// (<see cref="AttackSelector.ChargeTime"/> больше нуля), нажатие начинает УДЕРЖАНИЕ:
    /// отпустил рано - ушёл незаряженный вариант, додержал до порога - заряженный уходит
    /// сам. У направлений без заряда ничего не меняется: удар начинается по нажатию,
    /// как и был.
    /// </para>
    /// <para>
    /// Он же - источник <see cref="IWeaponStateSource"/> для общей модели занятости рук.
    /// Владельцем сделан именно контроллер, а не <see cref="WeaponStateMachine"/>:
    /// машина не знает про заряд (тот живёт здесь, в <see cref="HoldCharge"/>),
    /// а без заряда ответ на «занят ли» был бы неполным - зажатая кнопка удара
    /// это уже занятые руки, хотя состояние машины всё ещё Ready.
    /// </para>
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour, IWeaponStateSource
    {
        [Tooltip("Чем машем. Отсюда берутся посадка оружия в руке и раскладка приёмов")]
        [SerializeField] private WeaponDefinition _weapon;

        [Tooltip("Пустышки под костью кисти, в которые вешается оружие: одна на физическом " +
                 "теле, вторая на вьюмодели. Обе всегда держат одно и то же оружие в одном " +
                 "и том же состоянии - решение о видимости принимается здесь, один раз на всех")]
        [SerializeField] private WeaponSocket[] _sockets;

        [Tooltip("У кого спрашивать, стоит ли игрок на земле. Нужно только для того, " +
                 "чтобы удар в прыжке отличался от удара с земли")]
        [SerializeField] private PlayerController _body;

        private IInputReader _input;
        private WeaponStateMachine _machine;
        private AttackSelector _selector;
        private AttackInputBuffer _buffer;

        // Заряд удара. Экземпляр СВОЙ, и это принципиально: тем же классом копит замах
        // бросок груза, но у него свой экземпляр у переноски. Смешать их значило бы,
        // что замах ящиком поднимает боевой IsBusy и руки уезжают в Combat посреди броска
        private readonly HoldCharge _charge = new();

        // Направление, снятое в момент нажатия. Ждёт вместе с буфером: удар, вышедший
        // из буфера через десятую долю секунды, обязан быть тем, который игрок заказывал
        // нажатием, а не тем, куда он успел отпустить клавиши
        private AttackDirection _pendingDirection;

        // Удержание, с которым нажатие легло в буфер. Ждёт там же и по той же причине:
        // ступень заряда выбирается тем, что игрок держал, а не тем, что осталось
        // от кнопки к моменту, когда до нажатия дошла очередь
        private float _pendingHold;

        /// <summary>Ассет текущего оружия - единый источник таймингов для всех, кто их спросит</summary>
        public WeaponDefinition Weapon => _weapon;

        /// <summary>
        /// Каким приёмом бьют прямо сейчас. Ставится ДО входа в удар, поэтому слушатель
        /// <see cref="StateChanged"/> уже видит здесь тот приём, который начался,
        /// а не предыдущий
        /// </summary>
        public AttackDefinition CurrentAttack { get; private set; }

        /// <summary>Что сейчас с оружием</summary>
        public WeaponState State => _machine == null ? WeaponState.Sheathed : _machine.State;

        /// <inheritdoc />
        public bool IsWeaponDrawn => State != WeaponState.Sheathed;

        /// <summary>
        /// Занят ли игрок действием оружия. Список состояний не перечисляется, а выводится
        /// от обратного: свободны ровно два - убрано и готово. Всё остальное (доставание,
        /// удар со всеми его фазами, три фазы блока, убирание) - занято, и перечисление
        /// пришлось бы дописывать при каждом новом состоянии машины.
        /// <para>
        /// Заряд добавлен отдельным слагаемым: кнопку удара можно держать и в Ready,
        /// и даже с убранным оружием, а руки при этом уже заняты - игрок целится приёмом.
        /// Заряд здесь ровно один - БОЕВОЙ. Замах для броска груза копится тем же классом,
        /// но другим экземпляром и у другого владельца, и в этот флаг не попадает:
        /// иначе занятость рук во время броска уехала бы из Carrying в Combat
        /// </para>
        /// </summary>
        public bool IsBusy =>
            _charge.IsCharging ||
            (State != WeaponState.Sheathed && State != WeaponState.Ready);

        /// <summary>
        /// Состояние сменилось. Событие своё, а не проброшенное из машины напрямую: подписчик
        /// живёт на другом объекте, и порядок <c>Awake</c> между объектами Unity не обещает -
        /// проброс через свойство падал бы, окажись подписка раньше сборки машины
        /// </summary>
        public event Action<WeaponState> StateChanged;

        /// <summary>
        /// Оружие появилось в руке или исчезло из неё. Момент не совпадает со сменой состояния:
        /// сабля возникает на середине доставания, а не в его начале, - и всё, что должно
        /// появиться вместе с ней, обязано слушать именно это событие
        /// </summary>
        public event Action<bool> WeaponVisibilityChanged;

        [Inject]
        public void Construct(IInputReader input) => _input = input;

        /// <summary>
        /// Отдаёт бою общую модель занятости рук. Не через <c>[Inject]</c> намеренно:
        /// этот же компонент - ИСТОЧНИК для <see cref="HandsState"/>, и инъекция замкнула бы
        /// граф зависимостей сам на себя. Связывание идёт из <c>GameLifetimeScope</c>,
        /// когда оба конца уже созданы
        /// </summary>
        public void BindHands(HandsState hands) => _hands = hands;

        private HandsState _hands;

        private void Awake()
        {
            if (_weapon == null || _sockets == null || _sockets.Length == 0)
            {
                Debug.LogError($"Оружию игрока '{name}' не назначен ассет или сокет - доставать нечего.", this);
                enabled = false;
                return;
            }

            if (_weapon.Attacks == null)
            {
                Debug.LogError($"Оружию '{_weapon.name}' не назначена раскладка приёмов - бить нечем.", this);
                enabled = false;
                return;
            }

            _machine = new WeaponStateMachine(_weapon.Timings);
            _machine.WeaponVisibilityChanged += OnWeaponVisibilityChanged;
            _machine.StateChanged += OnStateChanged;

            _selector = new AttackSelector(_weapon.Attacks);
            _buffer = new AttackInputBuffer(_weapon.Attacks.InputBuffer);

            // Оружие собирается один раз и дальше только прячется: пересоздавать префаб
            // на каждое доставание значило бы аллоцировать в момент, когда игрок нажал клавишу
            foreach (WeaponSocket socket in _sockets)
                if (socket != null)
                    socket.Mount(_weapon);

            SetWeaponVisible(false);
        }

        private void Start()
        {
            _input.ToggleWeapon += OnToggleWeaponPressed;
            _input.Attack += OnAttackPressed;
            _input.AttackReleased += OnAttackReleased;
            _input.BlockPressed += OnBlockPressed;
            _input.BlockReleased += OnBlockReleased;
        }

        private void OnDestroy()
        {
            if (_input == null)
                return;

            _input.ToggleWeapon -= OnToggleWeaponPressed;
            _input.Attack -= OnAttackPressed;
            _input.AttackReleased -= OnAttackReleased;
            _input.BlockPressed -= OnBlockPressed;
            _input.BlockReleased -= OnBlockReleased;
        }

        private void OnToggleWeaponPressed()
        {
            // Руки заняты чем-то, кроме оружия, - нажатие пропадает. Ронять груз или
            // убирать листок за игрока не будем: он занял руки осознанно, и потерять
            // это от случайной единички обидно
            if (_hands != null && !_hands.CanToggleWeapon)
                return;

            _machine.ToggleWeapon();

            // Убрали оружие - забыли и нажатие, и место в чередовании. Иначе сабля,
            // вернувшись в руку, махнула бы сама собой, а серия продолжилась бы с той
            // вариации, на которой её бросили
            if (_machine.State == WeaponState.Sheathing)
            {
                _buffer.Clear();
                _charge.Cancel();
                _selector.ResetVariations();
            }
        }

        /// <summary>
        /// Нажатие только запоминается. Начнётся удар в этом же кадре или через десятую
        /// долю секунды - решает <see cref="TryStartBufferedAttack"/>, и от этого решения
        /// не должно зависеть, каким ударом он окажется: направление снимается здесь
        /// </summary>
        private void OnBlockPressed() => _machine.RaiseBlock();

        private void OnBlockReleased() => _machine.LowerBlock();

        private void OnAttackPressed()
        {
            // Нажатие во время блока не буферизуется, а пропадает: иначе оно выстрелило бы
            // ударом в тот момент, когда игрок опустил клинок, - через полсекунды после
            // того, как отпустил кнопку
            if (_machine.IsBlocking)
                return;

            // Руки заняты грузом или листком - ударить нечем. Одна проверка на оба случая:
            // и то, и другое выводит занятость из Free/Combat, а разбирать их по отдельности
            // значило бы держать это правило в двух местах
            if (_hands != null && !_hands.CanAttack)
                return;

            _pendingDirection = AttackSelector.ResolveDirection(_input.MoveInput, IsAirborne());

            // Направление сняли раньше, чем решили, когда бить: заряд длится доли секунды,
            // и за них игрок успевает отпустить клавиши движения. Приём, который он
            // заказывал нажатием, от этого меняться не должен
            float charge = _selector.ChargeTime(_pendingDirection);

            if (charge > 0f)
            {
                // У направления есть заряженный приём - нажатие пока не удар, а начало
                // удержания. Ударом оно станет либо на отпускании, либо на пороге.
                // Мёртвой зоны у боя нет: самое короткое нажатие - это уже удар,
                // вопрос только какой
                _charge.Begin(charge, 0f);
                return;
            }

            Fire(0f);
        }

        /// <summary>
        /// Кнопку отпустили раньше порога - уходит незаряженный вариант. Отпускание
        /// после того, как полный заряд уже ушёл в удар сам, не делает ничего:
        /// заряд к этому моменту погашен, и второго удара с него не будет
        /// </summary>
        private void OnAttackReleased()
        {
            if (!_charge.IsHolding)
                return;

            Fire(_charge.Release());
        }

        /// <summary>
        /// Кладёт нажатие в буфер вместе с тем удержанием, с которым его сделали.
        /// Начнётся удар в этом же кадре или через десятую долю секунды - решает
        /// <see cref="TryStartBufferedAttack"/>
        /// </summary>
        private void Fire(float heldTime)
        {
            _pendingHold = heldTime;
            _buffer.Press();
        }

        private void TryStartBufferedAttack()
        {
            if (!_buffer.HasPending || !_machine.CanAttack)
                return;

            AttackDefinition attack = _selector.Select(_pendingDirection, _pendingHold);

            if (attack == null)
            {
                // Раскладка пуста настолько, что бить нечем вообще. Держать нажатие
                // в буфере смысла нет - оно не станет валидным само по себе
                _buffer.Clear();
                return;
            }

            // Ставим до входа в удар: слушатели StateChanged читают приём сразу же,
            // и увидеть предыдущий они не должны
            CurrentAttack = attack;

            if (!_machine.Attack(attack.Duration, attack.CancelAfter(_weapon.Attacks.CancelWindow)))
                return;

            _buffer.Consume();

            // Направление рывка не передаём: его снимает сам контроллер в момент выпада,
            // со взгляда на тот кадр. Игрок за время замаха успевает довернуться, и
            // направление, снятое здесь - на нажатии, - к моменту выпада уже устарело бы
            if (_body != null)
                _body.Lunge(attack.LungeSpeed, attack.LungeDuration, attack.LungeDelay);
        }

        private bool IsAirborne() => _body != null && _body.IsAirborne;

        private void OnWeaponVisibilityChanged(bool visible)
        {
            SetWeaponVisible(visible);
            WeaponVisibilityChanged?.Invoke(visible);
        }

        /// <summary>
        /// Сабля появляется и исчезает сразу у обеих копий: физическая даёт тень, вьюмодельная -
        /// картинку, и разъехаться они не должны ни на кадр
        /// </summary>
        private void SetWeaponVisible(bool visible)
        {
            foreach (WeaponSocket socket in _sockets)
                if (socket != null)
                    socket.SetVisible(visible);
        }

        private void OnStateChanged(WeaponState state)
        {
            // Раньше здесь поднимался флаг запрета захвата у переноски. Флага больше нет:
            // «можно ли взять ящик» спрашивают у HandsState, и правило живёт в одном месте,
            // а не выталкивается отсюда в чужой компонент на каждой смене состояния
            StateChanged?.Invoke(state);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            _machine.Tick(deltaTime);

            // Заряд дошёл до порога - удар уходит сам, кнопку отпускать не надо.
            // Снимаем заряд тут же: без этого отпускание кнопки положило бы в буфер
            // второй удар - тот самый, который уже ушёл
            if (_charge.Tick(deltaTime))
                Fire(_charge.Release());

            // Порядок: сначала машина досчитала время и, возможно, освободилась, и только
            // потом буфер пробует отработать. Наоборот - и нажатие ждало бы лишний кадр
            TryStartBufferedAttack();

            _buffer.Tick(deltaTime);
        }
    }
}
