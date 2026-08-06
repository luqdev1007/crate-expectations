using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace CrateExpectations.Core.Services
{
    /// <summary>Абстракция платформы (Steam в будущем). Игровой код зависит только от интерфейса</summary>
    public interface IPlatformService
    {
        bool IsAvailable { get; }
        UniTask UnlockAchievementAsync(string achievementId);
    }
}


 