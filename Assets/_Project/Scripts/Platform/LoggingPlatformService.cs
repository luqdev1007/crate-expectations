using System.Collections.Generic;
using CrateExpectations.Core.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Platform
{
    public sealed class LoggingPlatformService : IPlatformService
    {
        private readonly HashSet<string> _unlocked = new();

        public bool IsAvailable => true;

        public IReadOnlyCollection<string> Unlocked => _unlocked;

        public UniTask UnlockAchievementAsync(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) 
                return UniTask.CompletedTask;

            if (!_unlocked.Add(achievementId)) 
                return UniTask.CompletedTask;

            Debug.Log($"[Платформа] Достижение получено: {achievementId}");

            return UniTask.CompletedTask;
        }
    }
}
