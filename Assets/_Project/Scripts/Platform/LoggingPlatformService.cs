using System.Collections.Generic;
using CrateExpectations.Core.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrateExpectations.Platform
{
    /// <summary>
    /// Вторая реализация платформы: считает себя доступной, помнит выданные достижения
    /// и не выдаёт одно и то же дважды - ровно так ведёт себя Steamworks.
    ///
    /// <para>Она здесь не ради логов, а как доказательство: чтобы игра поехала на другой
    /// платформе, меняется <b>одна строка в composition root</b>. Ни один файл игрового кода
    /// про <c>StubPlatformService</c> не знает и знать не может - он видит только
    /// <see cref="IPlatformService"/>. Реальный <c>SteamPlatformService</c> встанет
    /// на это же место.</para>
    /// </summary>
    public sealed class LoggingPlatformService : IPlatformService
    {
        private readonly HashSet<string> _unlocked = new();

        /// <inheritdoc />
        public bool IsAvailable => true;

        /// <summary>Достижения, выданные за сессию. Для тестов и отладки</summary>
        public IReadOnlyCollection<string> Unlocked => _unlocked;

        /// <inheritdoc />
        public UniTask UnlockAchievementAsync(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return UniTask.CompletedTask;

            // Повторная выдача - не ошибка и не событие: Steam на неё тоже отвечает молча
            if (!_unlocked.Add(achievementId)) return UniTask.CompletedTask;

            Debug.Log($"[Платформа] Достижение получено: {achievementId}");
            return UniTask.CompletedTask;
        }
    }
}
