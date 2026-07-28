using Cysharp.Threading.Tasks;

namespace CrateExpectations.Core.Services
{
    /// <summary>Абстракция платформы (Steam в будущем). Игровой код зависит только от интерфейса</summary>
    public interface IPlatformService
    {
        bool IsAvailable { get; }
        UniTask UnlockAchievementAsync(string achievementId);
    }
}
