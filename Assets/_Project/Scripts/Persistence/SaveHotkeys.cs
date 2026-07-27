using System;
using System.Threading;
using CrateExpectations.Core.Input;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Persistence
{
    public sealed class SaveHotkeys : IDisposable
    {
        private readonly IInputReader _input;
        private readonly IGameStateService _state;

        private readonly CancellationTokenSource _cts = new();

        public SaveHotkeys(IInputReader input, IGameStateService state)
        {
            _input = input;
            _state = state;

            _input.SaveGame += OnSavePressed;
            _input.LoadGame += OnLoadPressed;
        }

        public void Dispose()
        {
            _input.SaveGame -= OnSavePressed;
            _input.LoadGame -= OnLoadPressed;

            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnSavePressed() => SaveAsync().Forget();

        private void OnLoadPressed() => LoadAsync().Forget();

        private async UniTaskVoid SaveAsync() =>
            await _state.SaveAsync(_cts.Token).SuppressCancellationThrow();

        private async UniTaskVoid LoadAsync() =>
            await _state.LoadAsync(_cts.Token).SuppressCancellationThrow();
    }
}
