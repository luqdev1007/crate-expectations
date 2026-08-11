using CrateExpectations.Core.Hands;
using CrateExpectations.Interaction;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Переводит занятость рук и заряд броска в параметры аниматора вьюмодели.
    /// <para>
    /// Отдельный компонент, а не пристройка к <see cref="PlayerAnimatorDriver"/>, и вот
    /// почему. Тот живёт СОБЫТИЯМИ машины состояний оружия: у него нет и не должно быть
    /// <c>Update</c>, он просыпается на смену состояния. Занятость же нигде не «меняется» -
    /// она ВЫЧИСЛЯЕТСЯ из трёх источников (см. <see cref="HandsState"/>), события у неё нет
    /// и заводить его значило бы городить четвёртую копию состояния. Её остаётся опрашивать,
    /// а опрос каждый кадр внутри событийного компонента - это два разных способа жить
    /// в одном классе.
    /// </para>
    /// <para>
    /// Второе отличие: адресат. <see cref="PlayerAnimatorDriver"/> раздаёт вызовы ОБОИМ
    /// графам - телу и вьюмодели. Здешние параметры есть только у вьюмодели: в кадре руки
    /// видит только она, а тело в это время показывает игрока со спины, и поза переноски
    /// там ничего не решает. Один аниматор - одна ссылка, без списка и без флагов «а этот
    /// умеет».
    /// </para>
    /// <para>
    /// Логики здесь нет никакой: прочитал - сравнил - записал. Любое «а если заряд, то...»
    /// принадлежит тому, кто заряд считает
    /// </para>
    /// </summary>
    public sealed class HandsAnimatorDriver : MonoBehaviour
    {
        // Имена объявлены в ViewModelAnimatorBuilder, здесь - хешируются. Ровно так же
        // устроены остальные параметры графа; третьего места, где они встречаются, нет
        private const string HandsModeName = "HandsMode";
        private const string ChargeTName = "ChargeT";
        private const string GrabName = "Grab";
        private const string ThrowName = "Throw";
        private const string CarryGrabSpeedName = "CarryGrabSpeed";

        private static readonly int HandsModeId = Animator.StringToHash(HandsModeName);
        private static readonly int ChargeTId = Animator.StringToHash(ChargeTName);
        private static readonly int GrabId = Animator.StringToHash(GrabName);
        private static readonly int ThrowId = Animator.StringToHash(ThrowName);
        private static readonly int CarryGrabSpeedId = Animator.StringToHash(CarryGrabSpeedName);

        /// <summary>
        /// Мельче этого изменение заряда в аниматор не едет. Заряд - это float, который
        /// меняется каждый кадр; писать его без порога значило бы дёргать параметр
        /// на величину, которой не видно, все 60 раз в секунду
        /// </summary>
        private const float ChargeEpsilon = 0.002f;

        [Tooltip("Аниматор вьюмодели - тот, чей граф собирает ViewModelAnimatorBuilder. " +
                 "Телу эти параметры не нужны: в кадре руки показывает только вьюмодель")]
        [SerializeField] private Animator _animator;

        [Tooltip("Небоевые позы рук. Нужны здесь ровно за одним: длиной клипа взятия, " +
                 "чтобы ужать его до GrabDuration. Тот же приём, что у AttackSpeed")]
        [SerializeField] private HandsPoseDefinition _poses;

        private HandsState _hands;
        private Carrier _carrier;

        // Заведомо невозможные значения: первый же кадр обязан записать оба параметра,
        // иначе граф стартовал бы с чужой позой, пока игрок чего-нибудь не сделает
        private int _mode = -1;
        private float _charge = -1f;
        private float _grabSpeed = -1f;

        private bool _wired;

        /// <summary>
        /// Подписка на бросок идёт ЗДЕСЬ, а не в <c>Start</c>: инъекция приходит из
        /// <c>GameLifetimeScope</c>, и до неё ссылки на переноску просто нет. Порядок
        /// между инъекцией и <c>Start</c> Unity не обещает, а вот «после Construct
        /// переноска есть» - обещает сам контейнер
        /// </summary>
        [Inject]
        public void Construct(HandsState hands, Carrier carrier)
        {
            _hands = hands;
            _carrier = carrier;
            _carrier.Thrown += OnThrown;
        }

        private void Awake()
        {
            if (_animator == null)
            {
                Debug.LogError($"Водителю рук '{name}' не назначен аниматор - писать некуда.", this);
                enabled = false;
                return;
            }

            if (_poses == null)
            {
                Debug.LogError($"Водителю рук '{name}' не назначен ассет поз - " +
                               "проводку взятия нечем уложить в GrabDuration.", this);
                enabled = false;
                return;
            }

            // Параметров может не быть по одной-единственной причине: граф не пересобран
            // после правки генератора. Ругаемся именно об этом - иначе диагноз выглядел бы
            // как «руки не шевелятся», а лечился бы пунктом меню
            _wired = AnimatorParameters.Has(_animator, HandsModeName)
                     && AnimatorParameters.Has(_animator, ChargeTName)
                     && AnimatorParameters.Has(_animator, GrabName)
                     && AnimatorParameters.Has(_animator, ThrowName)
                     && AnimatorParameters.Has(_animator, CarryGrabSpeedName);

            if (_wired)
                return;

            Debug.LogError(
                $"В графе '{_animator.runtimeAnimatorController?.name}' нет всех параметров " +
                $"{HandsModeName} / {ChargeTName} / {GrabName} / {ThrowName} / {CarryGrabSpeedName}. " +
                "Пересоберите его: Tools → Crate Expectations → Rebuild View Model Animator.", this);

            enabled = false;
        }

        private void OnDestroy()
        {
            if (_carrier != null)
                _carrier.Thrown -= OnThrown;
        }

        /// <summary>
        /// Выброс - разовое СОБЫТИЕ, а не состояние: занятость рук во время броска
        /// не меняется (её держит хвост переноски), и вычислить момент из неё нельзя.
        /// <para>
        /// Проверка <c>_wired</c> здесь не формальность: подписка живёт с момента
        /// инъекции, а граф мог оказаться непересобранным - тогда компонент выключен,
        /// и писать в отсутствующий параметр нельзя
        /// </para>
        /// </summary>
        private void OnThrown()
        {
            if (_wired)
                _animator.SetTrigger(ThrowId);
        }

        private void Update()
        {
            // Инъекция приходит из GameLifetimeScope; до первого Update она уже случилась,
            // но собственный порядок Awake между объектами Unity не обещает - проверяем
            if (!_wired || _hands == null)
                return;

            WriteMode();
            WriteCharge();
            WriteGrabSpeed();
        }

        /// <summary>
        /// Занятость - целое, и писать его повторно нельзя вообще: значение, истинное
        /// каждый кадр, вместе с переходом из Any State перезапускало бы стейт бесконечно.
        /// <para>
        /// Здесь же взводится триггер взятия. Отдельного события у него нет и не нужно:
        /// «взял груз» - это ровно ФРОНТ занятости Carrying, а фронт тут уже вычисляется,
        /// чтобы не писать целое повторно. Второй источник того же момента разъехался бы
        /// с этим при первой же правке
        /// </para>
        /// </summary>
        private void WriteMode()
        {
            int mode = HandsAnimatorMode.Of(_hands.Occupancy);

            if (mode == _mode)
                return;

            bool grabbed = mode == HandsAnimatorMode.Carrying && _mode != HandsAnimatorMode.Carrying;

            _mode = mode;
            _animator.SetInteger(HandsModeId, mode);

            // Скорость обязана лечь раньше триггера: граф читает её при входе в стейт,
            // и выставленная следом досталась бы уже следующему взятию
            if (!grabbed)
                return;

            WriteGrabSpeed();
            _animator.SetTrigger(GrabId);
        }

        /// <summary>
        /// Множитель скорости проводки взятия: клип обязан уложиться в длительность
        /// из ассета, а не наоборот. Тот же приём, которым длина приёма подчиняет себе
        /// клип удара (<c>AttackSpeed</c>)
        /// </summary>
        private void WriteGrabSpeed()
        {
            AnimationClip clip = _poses != null ? _poses.CarryGrab : null;

            if (clip == null || _carrier.GrabDuration <= 0f)
                return;

            float speed = clip.length / _carrier.GrabDuration;

            if (Mathf.Approximately(speed, _grabSpeed))
                return;

            _grabSpeed = speed;
            _animator.SetFloat(CarryGrabSpeedId, speed);
        }

        private void WriteCharge()
        {
            float charge = _carrier.ChargeT;

            // Края блендтри - особый случай: в 0 и 1 надо попадать ТОЧНО, иначе поза покоя
            // останется чуть заряженной, а полный замах не доедет до своего края.
            // Сравнение на точное равенство здесь и означает «это значение уже записано»
            bool edge = (charge <= 0f || charge >= 1f) && charge != _charge;

            if (!edge && Mathf.Abs(charge - _charge) < ChargeEpsilon)
                return;

            _charge = charge;
            _animator.SetFloat(ChargeTId, charge);
        }
    }
}
