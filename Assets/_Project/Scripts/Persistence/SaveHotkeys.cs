using System;
using System.Threading;
using CrateExpectations.Core.Input;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Persistence
{
    /// <summary>
    /// F5 сохранить, F9 загрузить. Тонкая связка ввода с координатором: клавиши приходят
    /// через <see cref="IInputReader"/>, поэтому ни Input System, ни конкретные коды клавиш
    /// сюда не попадают - раскладка живёт в ассете действий.
    ///
    /// <para>Обычный C#-класс с подпиской в конструкторе и отпиской в <see cref="Dispose"/>:
    /// время жизни держит контейнер, и снять подписку невозможно забыть.</para>
    /// </summary>
    public sealed class SaveHotkeys : IDisposable
    {
        private readonly IInputReader _input;
        private readonly IGameStateService _state;

        // Общий токен: со scope контейнера незавершённые запись и чтение сворачиваются разом
        private readonly CancellationTokenSource _cts = new();

        public SaveHotkeys(IInputReader input, IGameStateService state)
        {
            _input = input;
            _state = state;

            _input.SaveGame += OnSavePressed;
            _input.LoadGame += OnLoadPressed;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _input.SaveGame -= OnSavePressed;
            _input.LoadGame -= OnLoadPressed;

            _cts.Cancel();
            _cts.Dispose();
        }

        // Обработчик события - единственное место, где допустим запуск задачи "в никуда":
        // об исходе игроку скажет HUD по событию из шины, а ждать здесь некому
        private void OnSavePressed() => SaveAsync().Forget();

        private void OnLoadPressed() => LoadAsync().Forget();

        // Отмена - не ошибка: сцену закрыли посреди записи. Исход игроку сообщит HUD
        // по событию из шины, поэтому результат здесь никому не нужен
        private async UniTaskVoid SaveAsync() =>
            await _state.SaveAsync(_cts.Token).SuppressCancellationThrow();

        private async UniTaskVoid LoadAsync() =>
            await _state.LoadAsync(_cts.Token).SuppressCancellationThrow();
    }
}
