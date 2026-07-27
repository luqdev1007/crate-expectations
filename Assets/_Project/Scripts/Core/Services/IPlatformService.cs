using Cysharp.Threading.Tasks;

namespace CrateExpectations.Core.Services
{
    public interface IPlatformService
    {
        bool IsAvailable { get; }
        UniTask UnlockAchievementAsync(string achievementId);
    }
}
