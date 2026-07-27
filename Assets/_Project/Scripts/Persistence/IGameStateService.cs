using System.Threading;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Persistence
{
    public interface IGameStateService
    {
        bool IsBusy { get; }

        UniTask<bool> SaveAsync(CancellationToken ct = default);

        UniTask<bool> LoadAsync(CancellationToken ct = default);

        UniTask<bool> HasSaveAsync();
    }
}
