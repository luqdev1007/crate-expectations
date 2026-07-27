using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Inspection.AI
{
    internal static class Pause
    {
        internal static async UniTask<bool> ForAsync(float seconds, CancellationToken token)
        {
            if (seconds <= 0f) 
                return token.IsCancellationRequested;

            return await UniTask
                .Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token)
                .SuppressCancellationThrow();
        }
    }
}
