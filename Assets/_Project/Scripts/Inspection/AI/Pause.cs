using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Inspection.AI
{
    /// <summary>
    /// Пауза, которую можно прервать. Общая для состояний, потому что отмена должна
    /// выглядеть одинаково везде: не исключением, а честным "нас прервали" в возврате -
    /// иначе забранный со стола груз рождал бы <c>OperationCanceledException</c>
    /// в самых неожиданных местах
    /// </summary>
    internal static class Pause
    {
        /// <summary>Подождать. <c>true</c> - ожидание прервали, продолжать нечего</summary>
        internal static async UniTask<bool> ForAsync(float seconds, CancellationToken token)
        {
            if (seconds <= 0f) return token.IsCancellationRequested;

            return await UniTask
                .Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token)
                .SuppressCancellationThrow();
        }
    }
}
