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

        private static readonly int HandsModeId = Animator.StringToHash(HandsModeName);
        private static readonly int ChargeTId = Animator.StringToHash(ChargeTName);

        /// <summary>
        /// Мельче этого изменение заряда в аниматор не едет. Заряд - это float, который
        /// меняется каждый кадр; писать его без порога значило бы дёргать параметр
        /// на величину, которой не видно, все 60 раз в секунду
        /// </summary>
        private const float ChargeEpsilon = 0.002f;

        [Tooltip("Аниматор вьюмодели - тот, чей граф собирает ViewModelAnimatorBuilder. " +
                 "Телу эти параметры не нужны: в кадре руки показывает только вьюмодель")]
        [SerializeField] private Animator _animator;

        private HandsState _hands;
        private Carrier _carrier;

        // Заведомо невозможные значения: первый же кадр обязан записать оба параметра,
        // иначе граф стартовал бы с чужой позой, пока игрок чего-нибудь не сделает
        private int _mode = -1;
        private float _charge = -1f;

        private bool _wired;

        [Inject]
        public void Construct(HandsState hands, Carrier carrier)
        {
            _hands = hands;
            _carrier = carrier;
        }

        private void Awake()
        {
            if (_animator == null)
            {
                Debug.LogError($"Водителю рук '{name}' не назначен аниматор - писать некуда.", this);
                enabled = false;
                return;
            }

            // Параметров может не быть по одной-единственной причине: граф не пересобран
            // после правки генератора. Ругаемся именно об этом - иначе диагноз выглядел бы
            // как «руки не шевелятся», а лечился бы пунктом меню
            _wired = AnimatorParameters.Has(_animator, HandsModeName)
                     && AnimatorParameters.Has(_animator, ChargeTName);

            if (_wired)
                return;

            Debug.LogError(
                $"В графе '{_animator.runtimeAnimatorController?.name}' нет параметров " +
                $"{HandsModeName} / {ChargeTName}. Пересоберите его: " +
                "Tools → Crate Expectations → Rebuild View Model Animator.", this);

            enabled = false;
        }

        private void Update()
        {
            // Инъекция приходит из GameLifetimeScope; до первого Update она уже случилась,
            // но собственный порядок Awake между объектами Unity не обещает - проверяем
            if (!_wired || _hands == null)
                return;

            WriteMode();
            WriteCharge();
        }

        /// <summary>
        /// Занятость - целое, и писать его повторно нельзя вообще: значение, истинное
        /// каждый кадр, вместе с переходом из Any State перезапускало бы стейт бесконечно
        /// </summary>
        private void WriteMode()
        {
            int mode = HandsAnimatorMode.Of(_hands.Occupancy);

            if (mode == _mode)
                return;

            _mode = mode;
            _animator.SetInteger(HandsModeId, mode);
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
