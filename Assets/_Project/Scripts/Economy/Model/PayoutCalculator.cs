using System;
using System.Collections.Generic;

namespace CrateExpectations.Economy
{
    /// <summary>
    /// Считает деньги за одну сдачу. Чистая функция по образцу <c>DisguiseProcessor</c>
    /// и <c>ClueEvaluator</c>: неизменяемые входы, никакого Unity, никакой собственной
    /// случайности - один и тот же расчёт всегда даёт один и тот же результат,
    /// поэтому его можно целиком закрыть edit-mode тестами
    /// </summary>
    public sealed class PayoutCalculator
    {
        // Строк в расчёте по определению не больше трёх: буфер заводится один раз
        // и переиспользуется - сдача груза не должна мусорить в куче
        private readonly List<PayoutLine> _lines = new(3);

        /// <summary>Посчитать, сколько причитается за сдачу</summary>
        /// <param name="terms">Денежные условия контракта</param>
        /// <param name="delivery">Чем сдача кончилась</param>
        public PayoutResult Calculate(in PayoutTerms terms, in DeliveryReport delivery)
        {
            _lines.Clear();
            int total = 0;

            if (delivery.IsSeized)
            {
                // Штраф хранится положительным числом - в расчёт он входит со знаком минус,
                // чтобы дизайнеру не приходилось помнить про знак в ассете
                total += Add(PayoutReason.Seizure, -Math.Abs(terms.PenaltyPerSeizure));
            }
            else
            {
                total += Add(PayoutReason.Delivery, terms.RewardPerCrate);

                if (delivery.Spotless) total += Add(PayoutReason.CleanBonus, terms.CleanBonus);
            }

            // Наружу отдаётся снимок: результат живёт дольше вызова - его показывает HUD
            // и разносит событие, а буфер к следующей сдаче уже перезаписан
            PayoutLine[] lines = _lines.Count > 0 ? _lines.ToArray() : Array.Empty<PayoutLine>();
            return new PayoutResult(total, lines);
        }

        /// <summary>Записать строку расчёта. Нулевые строки не пишутся: показывать нечего</summary>
        private int Add(PayoutReason reason, int amount)
        {
            if (amount == 0) return 0;

            _lines.Add(new PayoutLine(reason, amount));
            return amount;
        }
    }
}
