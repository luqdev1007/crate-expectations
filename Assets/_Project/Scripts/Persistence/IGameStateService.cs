using System.Threading;
using Cysharp.Threading.Tasks;

namespace CrateExpectations.Persistence
{
    /// <summary>
    /// Сохранение и загрузка прогресса. Всё, что нужно знать вызывающему: два действия
    /// и признак занятости. Ни файлов, ни JSON, ни имён систем
    /// </summary>
    public interface IGameStateService
    {
        /// <summary>Операция уже идёт. Второй запрос будет отклонён</summary>
        bool IsBusy { get; }

        /// <summary>Записать текущий прогресс. <c>false</c> - не вышло, состояние не тронуто</summary>
        UniTask<bool> SaveAsync(CancellationToken ct = default);

        /// <summary>Восстановить прогресс. <c>false</c> - не вышло, мир остался прежним</summary>
        UniTask<bool> LoadAsync(CancellationToken ct = default);

        /// <summary>Есть ли что загружать</summary>
        UniTask<bool> HasSaveAsync();
    }
}
