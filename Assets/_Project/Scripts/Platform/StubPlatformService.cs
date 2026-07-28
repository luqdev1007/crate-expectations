using Cysharp.Threading.Tasks;
using UnityEngine;
using CrateExpectations.Core.Services;

namespace CrateExpectations.Platform
{
    public sealed class StubPlatformService : IPlatformService
    {
        public bool IsAvailable => false; // Steam недоступен в MVP

        public UniTask UnlockAchievementAsync(string achievementId)
        {
            Debug.Log($"[Platform:Stub] Achievement -> {achievementId}");
            return UniTask.CompletedTask;
        }
    }
}
