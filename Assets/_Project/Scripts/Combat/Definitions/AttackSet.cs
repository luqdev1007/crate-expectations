using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrateExpectations.Combat
{
    /// <summary>
    /// Раскладка приёмов: какое направление каким ударом отвечает. Таблица в данных,
    /// а не <c>switch</c> в коде - поменять местами два удара или добавить третью
    /// вариацию должно быть правкой ассета, а не правкой класса.
    /// <para>
    /// Отсюда же собирается граф аниматора: билдер перечисляет
    /// <see cref="All"/> и ставит по стейту на приём, а <see cref="IndexOf"/> даёт то
    /// самое число, которым стейт выбирается в рантайме. Один источник и для выбора
    /// удара, и для графа - разъехаться им негде.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "AttackSet",
        menuName = "CrateExpectations/Combat/Attack Set")]
    public sealed class AttackSet : ScriptableObject, IAttackTable
    {
        /// <summary>
        /// Строка раскладки. Вариаций может быть сколько угодно: они чередуются
        /// при повторных ударах в ту же сторону, и одна в списке - это просто
        /// «вариаций нет»
        /// </summary>
        [Serializable]
        private struct DirectionEntry
        {
            public AttackDirection Direction;

            [Tooltip("Приёмы этого направления по порядку. Повторный удар в ту же " +
                     "сторону берёт следующий по кругу")]
            public AttackDefinition[] Attacks;
        }

        [Tooltip("Раскладка. Направление без строки падает на Neutral")]
        [SerializeField] private DirectionEntry[] _directions;

        [Header("Отзывчивость")]
        [Tooltip("Буфер ввода, с. Нажатие, пришедшее в конце текущего удара, не " +
                 "проглатывается, а ждёт своей очереди столько времени")]
        [SerializeField][Range(0f, 0.5f)] private float _inputBuffer = 0.15f;

        [Tooltip("Окно отмены: какая доля ДОВОДКИ (времени после активного окна) " +
                 "прерывается следующим ударом. 0.3 - последняя треть доводки. " +
                 "Считается от доводки, а не от всего приёма: отменять замах и момент " +
                 "касания нельзя, иначе удар исчезает раньше, чем ударил")]
        [SerializeField][Range(0f, 1f)] private float _cancelWindow = 0.3f;

        // Плоский список строится один раз и кэшируется: его спрашивают на каждом ударе
        // (IndexOf), а строится он аллокациями. OnValidate сбрасывает кэш - иначе правка
        // раскладки в инспекторе не доезжала бы до play mode
        private List<AttackDefinition> _flat;

        /// <summary>Буфер ввода, с</summary>
        public float InputBuffer => _inputBuffer;

        /// <summary>Доля доводки, отданная под отмену следующим ударом</summary>
        public float CancelWindow => _cancelWindow;

        /// <summary>
        /// Все приёмы набора без повторов, в порядке объявления. Порядок здесь -
        /// это и есть номер стейта в графе, поэтому он обязан быть устойчивым
        /// </summary>
        public IReadOnlyList<AttackDefinition> All
        {
            get
            {
                EnsureFlat();
                return _flat;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<AttackDefinition> Get(AttackDirection direction)
        {
            if (_directions == null)
                return Array.Empty<AttackDefinition>();

            for (int i = 0; i < _directions.Length; i++)
                if (_directions[i].Direction == direction && HasAny(_directions[i].Attacks))
                    return _directions[i].Attacks;

            return Array.Empty<AttackDefinition>();
        }

        /// <summary>
        /// Номер приёма в графе аниматора. Минус один - приёма в наборе нет;
        /// это значит, что ассет и граф разъехались, и звать стейт бесполезно
        /// </summary>
        public int IndexOf(AttackDefinition attack)
        {
            EnsureFlat();

            return attack == null ? -1 : _flat.IndexOf(attack);
        }

        private void OnValidate() => _flat = null;

        private void EnsureFlat()
        {
            if (_flat != null)
                return;

            _flat = new List<AttackDefinition>();

            if (_directions == null)
                return;

            foreach (DirectionEntry entry in _directions)
            {
                if (entry.Attacks == null)
                    continue;

                // Один и тот же приём может стоять на двух направлениях - стейт в графе
                // ему всё равно нужен один
                foreach (AttackDefinition attack in entry.Attacks)
                    if (attack != null && !_flat.Contains(attack))
                        _flat.Add(attack);
            }
        }

        private static bool HasAny(AttackDefinition[] attacks)
        {
            if (attacks == null)
                return false;

            foreach (AttackDefinition attack in attacks)
                if (attack != null)
                    return true;

            return false;
        }
    }
}
