using CrateExpectations.Combat;
using CrateExpectations.Player.Combat;
using UnityEngine;

namespace CrateExpectations.Player.View
{
    /// <summary>
    /// Изготовка к заряженному удару: пока игрок держит кнопку, вьюмодель уводится в отвод,
    /// а на выстреле или отмене возвращается обратно. Ещё один <see cref="IViewModelLayer"/> -
    /// слоистость <see cref="ViewModelRig"/> для того и сделана, чтобы новый вид движения
    /// добавлялся компонентом, без правок в риге и в соседних слоях.
    /// <para>
    /// Зачем понадобился: у боевого заряда не было никакой картинки вообще. Между нажатием
    /// и ударом граф стоял в <c>CombatIdle</c>, и игрок, держащий кнопку, не видел разницы
    /// с тем, кто её не держит. Пока заряжено было одно направление, это ещё сходило;
    /// теперь заряжены четыре из шести.
    /// </para>
    /// <para>
    /// Слой не имеет отношения к <see cref="ViewModelSwing"/> и его тумблеру: тот
    /// ЗАМЕНЯЕТ клип удара кривыми, а этот добавляет движение ПЕРЕД ударом, которого
    /// в клипе нет вовсе. Клип удара при этом играет как играл.
    /// </para>
    /// </summary>
    public sealed class ViewModelChargeWindup : MonoBehaviour, IViewModelLayer
    {
        [Tooltip("Чей заряд отыгрываем. Отсюда же берётся ассет отвода - через оружие, " +
                 "как и дуга взмаха: отвод это свойство приёма, а не объекта сцены")]
        [SerializeField] private PlayerWeaponController _weapon;

        // Направление снимается ОДИН РАЗ, на входе в заряд, и до конца отвода не
        // перечитывается - по той же причине, по которой его фиксирует _pendingDirection
        // в контроллере: за время удержания игрок успевает отпустить клавиши движения,
        // а изготовка обязана остаться той, под приём которой она началась
        private AttackDirection _direction;

        // Слою есть что сказать: идёт отвод или ещё едет возврат. Пока он опущен,
        // Update не считает ничего, а Evaluate отдаёт None
        private bool _active;

        // Направление заряжаемое и отвод для него нашёлся. Разведено с _active
        // намеренно: заряд по направлению без заряженной ступени слой молчит, но
        // недоехавший возврат от предыдущего заряда обязан доехать
        private bool _armed;

        private bool _wasCharging;

        // Текущая добавка. Держим позой, а не пересчитываем в Evaluate из прогресса:
        // возврат идёт от того места, где отвод застали, и прогресса заряда для него
        // уже не существует
        private Vector3 _offset;
        private Quaternion _rotation = Quaternion.identity;

        // Точка, ОТ которой едет отвод. Ноль в обычном случае и что-то ненулевое, если
        // заряд начали посреди возврата: быстрый повторный тап-и-держи не должен
        // дёргать позу скачком в ноль и обратно
        private Vector3 _startOffset;
        private Quaternion _startRotation = Quaternion.identity;

        private Vector3 _snapFromOffset;
        private Quaternion _snapFromRotation = Quaternion.identity;
        private float _snapElapsed;
        private bool _snapping;

        // Длительность возврата запоминается на ходу, пока заряд копится: к моменту
        // релиза спрашивать её у ассета уже поздно - направление к тому времени может
        // оказаться незаряжаемым, а возврат всё равно обязан отработать
        private float _snapDuration;

        private void Awake()
        {
            if (_weapon == null)
            {
                Debug.LogError($"Изготовке вьюмодели '{name}' не назначено оружие - " +
                               "не от чего отсчитывать заряд.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Состояние копим здесь, а не в <see cref="Evaluate"/>: слой опрашивают из
        /// <c>LateUpdate</c> рига, и класть накопление в опрос значит завязать результат
        /// на то, сколько раз за кадр слой спросили
        /// </summary>
        private void Update()
        {
            if (_weapon == null)
                return;

            bool charging = _weapon.IsCharging;

            if (charging != _wasCharging)
            {
                _wasCharging = charging;

                if (charging)
                    BeginWindup();
                else
                    BeginSnapBack();
            }

            // Ни отвода, ни возврата - слой не считает вообще ничего и не крутится вхолостую
            if (!_active)
                return;

            if (charging && _armed)
            {
                TickWindup();
                return;
            }

            TickSnapBack();
        }

        /// <inheritdoc />
        public ViewModelOffset Evaluate()
        {
            // Слой остаётся в списке рига, даже когда молчит: порядок наложения задаётся
            // порядком компонентов, и выпадение одного из них сдвинуло бы остальные
            if (!_active)
                return ViewModelOffset.None;

            // Пивот нулевой, и это не упущение: изготовка не проворачивает вьюмодель
            // вокруг точки хвата, как дуга взмаха, а уводит её целиком. При нулевом пивоте
            // формула ViewModelOffset вырождается в Rotation * pose + Translation -
            // ровно поворот вокруг локального начала плюс смещение
            return new ViewModelOffset(Vector3.zero, _rotation, _offset);
        }

        private void BeginWindup()
        {
            _armed = false;

            ChargeWindupDefinition definition = Definition;

            // Направление без заряженной ступени отводить незачем: заряда на нём не будет,
            // а если он всё-таки идёт - изготовка врала бы про приём, которого нет
            if (definition == null || _weapon.ChargingAttack == null)
                return;

            _direction = _weapon.ChargingDirection;

            // Стартуем ОТ текущей позы, а не от нуля. В покое это одно и то же, а вот
            // заряд, начатый посреди возврата, иначе рванул бы в ноль на первом же кадре
            _startOffset = _offset;
            _startRotation = _rotation;

            _snapping = false;
            _armed = true;
            _active = true;
        }

        private void TickWindup()
        {
            ChargeWindupDefinition definition = Definition;

            if (definition == null)
                return;

            // Отвод перечитываем каждый кадр, а направление - нет. Числа лежат в ассете
            // ровно ради подбора вживую, и правка ползунка должна доезжать сразу;
            // направление же менять посреди заряда нельзя ни при каких обстоятельствах
            ChargeWindupDefinition.DirectionWindup windup = definition.For(_direction);

            float t = Mathf.Clamp01(_weapon.ChargeProgress);

            _offset = Vector3.Lerp(_startOffset, windup.Offset, t);
            _rotation = Quaternion.Slerp(_startRotation, Quaternion.Euler(windup.EulerAngles), t);
            _snapDuration = windup.SnapBackTime;
        }

        /// <summary>
        /// Заряд кончился - неважно, ушёл он ударом или его отменили убиранием сабли.
        /// Возврат в обоих случаях один: клип удара стартует со своей позы и подхватывать
        /// отвод не обязан, а отмена вообще ничего не проигрывает
        /// </summary>
        private void BeginSnapBack()
        {
            _armed = false;

            if (!_active)
                return;

            _snapFromOffset = _offset;
            _snapFromRotation = _rotation;
            _snapElapsed = 0f;
            _snapping = _snapDuration > 0f;

            if (_snapping)
                return;

            // Возврата нет как настройки - гасим позу тем же кадром
            Rest();
        }

        private void TickSnapBack()
        {
            if (!_snapping)
            {
                Rest();
                return;
            }

            _snapElapsed += Time.deltaTime;

            float t = Mathf.Clamp01(_snapElapsed / _snapDuration);

            _offset = Vector3.Lerp(_snapFromOffset, Vector3.zero, t);
            _rotation = Quaternion.Slerp(_snapFromRotation, Quaternion.identity, t);

            if (t >= 1f)
                Rest();
        }

        /// <summary>
        /// Поза в нуле, слой молчит. Ноль ставится явно, а не оставляется тем, что
        /// насчитала интерполяция: остаток в тысячных не виден в кадре, но остался бы
        /// стартовой точкой следующего заряда
        /// </summary>
        private void Rest()
        {
            _offset = Vector3.zero;
            _rotation = Quaternion.identity;
            _snapping = false;
            _active = false;
        }

        private ChargeWindupDefinition Definition =>
            _weapon.Weapon == null ? null : _weapon.Weapon.ChargeWindup;
    }
}
