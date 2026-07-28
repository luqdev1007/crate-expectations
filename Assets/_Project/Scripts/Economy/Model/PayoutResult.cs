using System;
using System.Collections.Generic;

namespace CrateExpectations.Economy
{
    /// <summary>За что именно начислили или сняли деньги</summary>
    public enum PayoutReason
    {
        /// <summary>Плата за принятый ящик</summary>
        Delivery,
        /// <summary>Надбавка за то, что инспектор не нашёл ни одной улики</summary>
        CleanBonus,
        /// <summary>Штраф за задержанный ящик</summary>
        Seizure,
    }

    /// <summary>Одна строка расчёта: за что и на сколько</summary>
    public readonly struct PayoutLine
    {
        public PayoutLine(PayoutReason reason, int amount)
        {
            Reason = reason;
            Amount = amount;
        }

        /// <summary>За что</summary>
        public PayoutReason Reason { get; }

        /// <summary>Сумма со знаком: плюс - начисление, минус - списание</summary>
        public int Amount { get; }

        public override string ToString() => $"{Reason} {Amount:+#;-#;0}";
    }

    /// <summary>
    /// Результат расчёта: итоговая сумма со знаком и разбивка, из чего она сложилась.
    /// Не голое число - иначе игроку нечего было бы показать, кроме "баланс изменился",
    /// а разбираться, почему именно, пришлось бы по логам
    /// </summary>
    public readonly struct PayoutResult
    {
        private readonly IReadOnlyList<PayoutLine> _lines;

        public PayoutResult(int amount, IReadOnlyList<PayoutLine> lines)
        {
            Amount = amount;
            _lines = lines;
        }

        /// <summary>Итог со знаком: плюс - начисление, минус - списание</summary>
        public int Amount { get; }

        /// <summary>Из чего сложился итог. Никогда не <c>null</c></summary>
        public IReadOnlyList<PayoutLine> Lines => _lines ?? Array.Empty<PayoutLine>();

        /// <summary>Расчёт закончился списанием</summary>
        public bool IsPenalty => Amount < 0;

        /// <summary>Нашлась ли в расчёте строка такого рода</summary>
        public bool Has(PayoutReason reason)
        {
            IReadOnlyList<PayoutLine> lines = Lines;

            for (int i = 0; i < lines.Count; i++)
                if (lines[i].Reason == reason) 
                    return true;

            return false;
        }

        public override string ToString() => $"{Amount:+#;-#;0} ({Lines.Count} строк)";
    }
}
